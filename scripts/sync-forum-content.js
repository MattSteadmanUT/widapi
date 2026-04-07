#!/usr/bin/env node
/**
 * Syncs forum posts from S3 into repo artifacts used for API design discussions.
 *
 * Defaults:
 * - AWS profile: GovProd
 * - S3 source: s3://ulmita-forum-content/nationalwid/
 *
 * Outputs:
 * - docs/reference-documents/forum/nationalwid-forum-sync.md
 * - specs/wid-3.0/supporting/forum/nationalwid-forum-normalized.json
 */

const fs = require("fs");
const path = require("path");
const crypto = require("crypto");
const { execSync } = require("child_process");

const repoRoot = path.join(__dirname, "..");
const tempRoot = path.join(repoRoot, ".forum-sync", "raw", "nationalwid");
const docsOut = path.join(
  repoRoot,
  "docs",
  "reference-documents",
  "forum",
  "nationalwid-forum-sync.md"
);
const specOut = path.join(
  repoRoot,
  "specs",
  "wid-3.0",
  "supporting",
  "forum",
  "nationalwid-forum-normalized.json"
);

const args = parseArgs(process.argv.slice(2));
const awsProfile = args.profile || process.env.AWS_PROFILE || "GovProd";
const s3Source = args.source || "s3://ulmita-forum-content/nationalwid/";

function parseArgs(argv) {
  const parsed = {};
  for (let i = 0; i < argv.length; i += 1) {
    const value = argv[i];
    if (!value.startsWith("--")) continue;
    const key = value.slice(2);
    const next = argv[i + 1];
    parsed[key] = next && !next.startsWith("--") ? next : true;
    if (next && !next.startsWith("--")) i += 1;
  }
  return parsed;
}

function ensureDir(dirPath) {
  fs.mkdirSync(dirPath, { recursive: true });
}

function run(command) {
  execSync(command, { stdio: "inherit" });
}

function listFilesRecursive(dirPath) {
  const out = [];
  if (!fs.existsSync(dirPath)) return out;
  const entries = fs.readdirSync(dirPath, { withFileTypes: true });
  for (const entry of entries) {
    const fullPath = path.join(dirPath, entry.name);
    if (entry.isDirectory()) {
      out.push(...listFilesRecursive(fullPath));
    } else {
      out.push(fullPath);
    }
  }
  return out;
}

function parseJsonFiles(files) {
  const payloads = [];
  for (const filePath of files) {
    if (!filePath.toLowerCase().endsWith(".json")) continue;
    try {
      const data = JSON.parse(fs.readFileSync(filePath, "utf8"));
      payloads.push({ filePath, data });
    } catch (error) {
      console.warn(`Skipping invalid JSON: ${relativeRepoPath(filePath)} (${error.message})`);
    }
  }
  return payloads;
}

function relativeRepoPath(filePath) {
  return path.relative(repoRoot, filePath).replace(/\\/g, "/");
}

function flattenPostCandidates(node, bag, sourceFile) {
  if (Array.isArray(node)) {
    for (const item of node) flattenPostCandidates(item, bag, sourceFile);
    return;
  }

  if (!node || typeof node !== "object") {
    return;
  }

  if (looksLikePost(node)) {
    bag.push({ raw: node, sourceFile });
  }

  for (const value of Object.values(node)) {
    if (value && typeof value === "object") {
      flattenPostCandidates(value, bag, sourceFile);
    }
  }
}

function looksLikePost(obj) {
  const keys = Object.keys(obj);
  const keySet = new Set(keys.map((k) => k.toLowerCase()));
  const hasAny = (candidates) => candidates.some((k) => keySet.has(k.toLowerCase()));

  const hasIdentity = hasAny(["id", "postId", "messageId", "uuid", "_id"]);
  const hasBody = hasAny(["body", "content", "message", "text", "post", "postBody"]);
  const hasAuthor = hasAny(["author", "user", "createdBy", "authorName"]);
  const hasDate = hasAny(["createdAt", "created", "postedAt", "timestamp", "date"]);

  return hasIdentity || (hasBody && (hasAuthor || hasDate));
}

function firstValue(obj, paths) {
  for (const keyPath of paths) {
    const segments = keyPath.split(".");
    let current = obj;
    let found = true;
    for (const segment of segments) {
      if (!current || typeof current !== "object") {
        found = false;
        break;
      }

      const actualKey = Object.keys(current).find(
        (k) => k.toLowerCase() === segment.toLowerCase()
      );

      if (!actualKey) {
        found = false;
        break;
      }

      current = current[actualKey];
    }
    if (found && current !== undefined && current !== null && current !== "") {
      return current;
    }
  }
  return null;
}

function toText(value) {
  if (value === null || value === undefined) return "";
  if (typeof value === "string") return value.trim();
  if (typeof value === "number" || typeof value === "boolean") return String(value);
  if (typeof value === "object") {
    if ("text" in value && typeof value.text === "string") return value.text.trim();
    if ("content" in value && typeof value.content === "string") return value.content.trim();
    return JSON.stringify(value);
  }
  return String(value);
}

function normalizeDate(value) {
  const text = toText(value);
  if (!text) return "";
  const dt = new Date(text);
  if (!Number.isNaN(dt.getTime())) return dt.toISOString();

  if (/^\d{10}$/.test(text)) {
    return new Date(Number(text) * 1000).toISOString();
  }
  if (/^\d{13}$/.test(text)) {
    return new Date(Number(text)).toISOString();
  }

  return text;
}

function createStableId(raw, sourceFile, index) {
  const explicit = toText(
    firstValue(raw, ["postId", "id", "messageId", "uuid", "_id", "post.id", "Id"])
  );
  if (explicit) return explicit;

  const hashInput = `${sourceFile}::${index}::${toText(firstValue(raw, ["title", "body", "content", "message"]))}`;
  return `generated-${crypto.createHash("sha1").update(hashInput).digest("hex").slice(0, 12)}`;
}

function inferThreadKey(raw, sourceFile) {
  const fromPayload = toText(
    firstValue(raw, ["threadTitle", "topic", "threadId", "topicId", "conversationId"])
  );
  if (fromPayload) return fromPayload;

  const relative = relativeRepoPath(sourceFile);
  const parts = relative.split("/");
  if (parts.length >= 2) {
    return parts[parts.length - 2];
  }
  return "";
}

function makeScopedId(threadKey, baseId) {
  const thread = toText(threadKey);
  const id = toText(baseId);
  if (!id) return "";
  if (!thread) return id;
  if (id.includes("#")) return id;
  return `${thread}#${id}`;
}

function normalizePosts(candidates) {
  const normalized = [];
  for (let i = 0; i < candidates.length; i += 1) {
    const candidate = candidates[i];
    const raw = candidate.raw;
    const threadKey = inferThreadKey(raw, candidate.sourceFile);

    const rawId = createStableId(raw, candidate.sourceFile, i);
    const id = makeScopedId(threadKey, rawId);
    const rawReplyToId = toText(
      firstValue(raw, [
        "replyTo",
        "ReplyToId",
        "replyToPostId",
        "replyToId",
        "inReplyTo",
        "inReplyToPostId",
        "parentId",
        "parentPostId",
        "reply.parentId",
      ])
    );
    const replyToId = makeScopedId(threadKey, rawReplyToId);

    const title = toText(firstValue(raw, ["title", "subject", "topicTitle", "threadTitle", "Title"]));
    const body = toText(
      firstValue(raw, ["pureContent", "body", "content", "message", "text", "post", "postBody", "Content"])
    );
    const author = toText(
      firstValue(raw, ["authorName", "author.name", "author", "user.name", "user", "createdBy", "Author"])
    );

    const createdAt = normalizeDate(
      firstValue(raw, ["createdAt", "created", "postedAt", "timestamp", "date", "createdDate"])
    );

    const threadHint = toText(firstValue(raw, ["threadId", "topicId", "conversationId", "thread.id"]));

    normalized.push({
      id,
      rawId,
      replyToId: replyToId || null,
      threadHint: threadHint || null,
      title: title || null,
      body: body || null,
      author: author || null,
      createdAt: createdAt || null,
      sourceFile: relativeRepoPath(candidate.sourceFile),
    });
  }

  return dedupePosts(normalized);
}

function dedupePosts(posts) {
  const byId = new Map();
  for (const post of posts) {
    const existing = byId.get(post.id);
    if (!existing) {
      byId.set(post.id, post);
      continue;
    }

    const merged = { ...existing };
    for (const key of ["replyToId", "threadHint", "title", "body", "author", "createdAt"]) {
      if (!merged[key] && post[key]) merged[key] = post[key];
    }
    if (existing.sourceFile !== post.sourceFile) {
      merged.sourceFile = `${existing.sourceFile};${post.sourceFile}`;
    }
    byId.set(post.id, merged);
  }

  return Array.from(byId.values());
}

function comparePosts(a, b) {
  const aDate = a.createdAt || "";
  const bDate = b.createdAt || "";
  if (aDate !== bDate) return aDate.localeCompare(bDate);
  return a.id.localeCompare(b.id);
}

function buildThreads(posts) {
  const byId = new Map(posts.map((p) => [p.id, p]));
  const children = new Map();

  for (const post of posts) {
    const parentId = post.replyToId;
    if (parentId && byId.has(parentId)) {
      if (!children.has(parentId)) children.set(parentId, []);
      children.get(parentId).push(post.id);
    }
  }

  for (const childIds of children.values()) {
    childIds.sort((a, b) => comparePosts(byId.get(a), byId.get(b)));
  }

  const roots = posts
    .filter((p) => !(p.replyToId && byId.has(p.replyToId)))
    .sort(comparePosts)
    .map((p) => p.id);

  const threads = roots.map((rootId) => ({
    rootId,
    postIds: traverseThread(rootId, children),
  }));

  return { threads, children, byId };
}

function traverseThread(rootId, children) {
  const result = [];
  const stack = [{ id: rootId, visited: false }];

  while (stack.length > 0) {
    const top = stack.pop();
    if (!top.visited) {
      result.push(top.id);
      const childIds = children.get(top.id) || [];
      for (let i = childIds.length - 1; i >= 0; i -= 1) {
        stack.push({ id: childIds[i], visited: false });
      }
    }
  }

  return result;
}

function postSummaryLine(post) {
  const who = post.author || "Unknown author";
  const when = post.createdAt || "Unknown date";
  return `${post.id} | ${who} | ${when}`;
}

function truncate(text, maxLength) {
  if (!text) return "";
  const compact = text.replace(/\s+/g, " ").trim();
  if (compact.length <= maxLength) return compact;
  return `${compact.slice(0, maxLength - 3)}...`;
}

function renderThreadMarkdown(rootId, byId, children, indentLevel) {
  const lines = [];
  const post = byId.get(rootId);
  const indent = "  ".repeat(indentLevel);

  lines.push(`${indent}- ${postSummaryLine(post)}`);
  if (post.replyToId) {
    lines.push(`${indent}  Reply to: ${post.replyToId}`);
  }
  if (post.title) {
    lines.push(`${indent}  Title: ${post.title}`);
  }
  if (post.body) {
    lines.push(`${indent}  Excerpt: ${truncate(post.body, 280)}`);
    lines.push(`${indent}  <details>`);
    lines.push(`${indent}    <summary>Full post content</summary>`);
    lines.push("");
    lines.push(`${indent}    ${post.body.replace(/\s+/g, " ").trim()}`);
    lines.push("");
    lines.push(`${indent}  </details>`);
  }

  const childIds = children.get(rootId) || [];
  for (const childId of childIds) {
    lines.push(...renderThreadMarkdown(childId, byId, children, indentLevel + 1));
  }

  return lines;
}

function toMarkdown(posts, threadData) {
  const { threads, children, byId } = threadData;

  const lines = [];
  lines.push("# National WID Forum Sync");
  lines.push("");
  lines.push("Synced: Deterministic output (updated when source forum content changes)");
  lines.push(`Source: ${s3Source}`);
  lines.push(`AWS Profile: ${awsProfile}`);
  lines.push(`Post Count: ${posts.length}`);
  lines.push(`Thread Count: ${threads.length}`);
  lines.push("");
  lines.push("This file is generated by scripts/sync-forum-content.js.");
  lines.push("Reply relationships are represented by indentation and the Reply to line for each non-root post.");
  lines.push("");

  for (let i = 0; i < threads.length; i += 1) {
    const thread = threads[i];
    const rootPost = byId.get(thread.rootId);
    const heading = rootPost.title || `Thread rooted at ${thread.rootId}`;
    lines.push(`## Thread ${i + 1}: ${heading}`);
    lines.push("");
    lines.push(...renderThreadMarkdown(thread.rootId, byId, children, 0));
    lines.push("");
  }

  return `${lines.join("\n")}\n`;
}

function writeOutputs(posts, threadData) {
  ensureDir(path.dirname(docsOut));
  ensureDir(path.dirname(specOut));

  const markdown = toMarkdown(posts, threadData);
  fs.writeFileSync(docsOut, markdown);

  const json = {
    source: s3Source,
    awsProfile,
    postCount: posts.length,
    threadCount: threadData.threads.length,
    threads: threadData.threads,
    posts,
  };
  fs.writeFileSync(specOut, `${JSON.stringify(json, null, 2)}\n`);
}

function main() {
  ensureDir(tempRoot);

  console.log(`Syncing forum data from ${s3Source} with profile ${awsProfile}...`);
  run(`aws s3 sync "${s3Source}" "${tempRoot}" --profile "${awsProfile}" --only-show-errors`);

  const rawFiles = listFilesRecursive(tempRoot);
  const payloads = parseJsonFiles(rawFiles);

  const candidates = [];
  for (const payload of payloads) {
    flattenPostCandidates(payload.data, candidates, payload.filePath);
  }

  const posts = normalizePosts(candidates).sort(comparePosts);
  const threadData = buildThreads(posts);

  writeOutputs(posts, threadData);

  console.log(`Generated ${relativeRepoPath(docsOut)}`);
  console.log(`Generated ${relativeRepoPath(specOut)}`);
  console.log(`Posts: ${posts.length}, Threads: ${threadData.threads.length}`);
}

main();
