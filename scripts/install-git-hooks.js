#!/usr/bin/env node
/**
 * Installs a local pre-push hook for this repo.
 *
 * The hook runs forum sync before each push and blocks push if sync output changed,
 * so discussion artifacts are reviewed/committed alongside spec changes.
 */

const fs = require("fs");
const path = require("path");

const repoRoot = path.join(__dirname, "..");
const hooksDir = path.join(repoRoot, ".git", "hooks");
const hookPath = path.join(hooksDir, "pre-push");

const hookContent = `#!/bin/sh
set -e

echo "[pre-push] Running forum sync..."
npm run sync:forum

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
console.log("The pre-push hook will now run forum sync before every push on this machine.");
