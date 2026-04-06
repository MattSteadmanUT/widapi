#!/usr/bin/env node
/**
 * Copies the required Swagger UI static assets from node_modules into vendor/.
 * Run after upgrading swagger-ui-dist:
 *   npm install swagger-ui-dist@latest
 *   npm run update-vendor
 */

const fs = require("fs");
const path = require("path");

const srcDir = path.join(__dirname, "..", "node_modules", "swagger-ui-dist");
const destDir = path.join(__dirname, "..", "vendor", "swagger-ui");

const files = [
  "swagger-ui.css",
  "swagger-ui-bundle.js",
  "favicon-16x16.png",
  "favicon-32x32.png",
  "oauth2-redirect.html",
];

fs.mkdirSync(destDir, { recursive: true });

for (const file of files) {
  fs.copyFileSync(path.join(srcDir, file), path.join(destDir, file));
  console.log(`Copied ${file}`);
}

console.log("vendor/swagger-ui updated.");
