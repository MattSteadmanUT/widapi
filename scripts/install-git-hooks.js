#!/usr/bin/env node
/**
 * Installs a local pre-push hook for this repo.
 *
 * The hook runs forum sync before each push when the local machine has access to
 * the private forum source. Contributors without access are allowed to push
 * without interruption.
 */

const fs = require("fs");
const path = require("path");

const repoRoot = path.join(__dirname, "..");
const hooksDir = path.join(repoRoot, ".git", "hooks");
const hookPath = path.join(hooksDir, "pre-push");

const hookContent = `#!/bin/sh
set -e

FORUM_SOURCE="\${WID_FORUM_SOURCE:-s3://ulmita-forum-content/nationalwid/}"
FORUM_AWS_PROFILE="\${WID_FORUM_AWS_PROFILE:-GovProd}"

if [ "\${WID_FORUM_SYNC_ON_PUSH:-1}" = "0" ]; then
  echo "[pre-push] Forum sync disabled by WID_FORUM_SYNC_ON_PUSH=0; continuing push."
  exit 0
fi

if ! command -v aws >/dev/null 2>&1; then
  echo "[pre-push] AWS CLI not found; skipping private forum sync and continuing push."
  exit 0
fi

if ! aws s3 ls "$FORUM_SOURCE" --profile "$FORUM_AWS_PROFILE" >/dev/null 2>&1; then
  echo "[pre-push] Private forum source is not available for AWS profile '$FORUM_AWS_PROFILE'."
  echo "[pre-push] Skipping forum sync and continuing push."
  exit 0
fi

echo "[pre-push] Running forum sync from $FORUM_SOURCE with AWS profile $FORUM_AWS_PROFILE..."
npm run sync:forum -- --profile "$FORUM_AWS_PROFILE" --source "$FORUM_SOURCE"

if ! git diff --quiet -- docs/reference-documents/forum specs/wid-3.0/supporting/forum; then
  echo "[pre-push] Forum sync generated updates. Commit these files before pushing:"
  git status --short -- docs/reference-documents/forum specs/wid-3.0/supporting/forum
  exit 1
fi
`;

fs.mkdirSync(hooksDir, { recursive: true });
fs.writeFileSync(hookPath, hookContent, { mode: 0o755 });

try {
  fs.chmodSync(hookPath, 0o755);
} catch (error) {
  // Windows may ignore chmod for git hooks; write still succeeds.
}

console.log("Installed git hook:", path.relative(repoRoot, hookPath));
console.log("The pre-push hook will run forum sync only when this machine can access the private forum source.");
console.log("Set WID_FORUM_SYNC_ON_PUSH=0 to skip it explicitly.");
