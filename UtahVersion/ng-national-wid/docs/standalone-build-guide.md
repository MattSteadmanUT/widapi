# Building `ng-national-wid` Standalone

## Why this doc exists

In Utah's environment, `ng-national-wid` was never a standalone project — it lived at
`projects/ng-national-wid/` inside the larger `fed-ulmita-ng` Angular CLI workspace (the ULMITA
portal application). Its `tsconfig.lib.json` extends `../../tsconfig.json` from that workspace,
its `ng-package.json` writes output to `../../dist/ng-national-wid`, and it has no `angular.json`,
root `tsconfig.json`, or `node_modules` of its own. It is a **library sub-project**, not a
buildable application, and it depended on the host workspace for its Angular CLI toolchain,
TypeScript compiler options, and dependency versions.

What's copied into this repo (`src/`, `ng-package.json`, `tsconfig.lib.json`,
`tsconfig.lib.prod.json`, `package.json`) is the library's actual source and packaging
configuration — everything that's specific to *this* library. What's intentionally **not**
included is a fabricated standalone Angular workspace scaffold (`angular.json`, workspace-level
`tsconfig.json`, a `node_modules` tree) — generating one from scratch risks silently encoding the
wrong Angular CLI/build config, which would look legitimate but not actually build. Instead, below
is the exact, low-risk recipe: regenerate a real library project with the Angular CLI, then drop
this source in. This is also literally how the library is built and consumed today — see
[INTEGRATION.md](../INTEGRATION.md) — so it's a faithful description of the real process, not a
new one invented for this handoff.

## Recipe

1. **Create or use an existing Angular 17+ CLI workspace.** If you don't already have one to host
   this library:
   ```bash
   npm install -g @angular/cli
   ng new host-workspace --no-create-application
   cd host-workspace
   ```

2. **Generate a library project with the same name:**
   ```bash
   ng generate library ng-national-wid
   ```
   This creates `projects/ng-national-wid/` with a fresh `ng-package.json`, `tsconfig.lib.json`,
   `package.json`, and `src/` — scaffolded correctly for whatever Angular CLI version you just
   installed, and registered in the new workspace's `angular.json`.

3. **Replace the generated `src/` with this repo's `src/`:**
   ```bash
   rm -rf projects/ng-national-wid/src
   cp -r <this-repo>/UtahVersion/ng-national-wid/src projects/ng-national-wid/src
   ```

4. **Reconcile `ng-package.json` / `tsconfig.lib*.json` / `package.json`.** In most cases the
   freshly-generated files from step 2 are compatible as-is — diff them against the copies in this
   repo and pull over anything library-specific (this repo's `ng-package.json` sets
   `lib.entryFile: src/public-api.ts` and `dest: ../../dist/ng-national-wid`, which the generated
   one should already match by convention). The `package.json` in this repo carries the real
   package metadata (`name: "@ulmita/ng-national-wid"`, `peerDependencies`, `repository`) —
   merge that into the generated one rather than replacing it outright, since the generated one is
   also where `ng build` reads publish-relevant fields from.

5. **Build:**
   ```bash
   npm install
   ng build ng-national-wid
   ```
   Output lands in `dist/ng-national-wid/`, ready to `npm publish` or `npm pack` per
   [INTEGRATION.md](../INTEGRATION.md#publishing-to-a-private-registry).

## Version notes

- `package.json`'s `peerDependencies` require `@angular/common` / `@angular/core` / `@angular/forms`
  `>=17.0.0` — this is the actual minimum the code was written against (standalone: false NgModule
  pattern, no Angular 17+-only APIs in use).
- The library was last built and verified against **Angular 21** as part of the `fed-ulmita-ng`
  workspace (`@angular/cli ^21.0.4`, `typescript 5.9.3`, `ng-packagr ^21.2.7` — see that
  workspace's root `package.json` if you need exact versions for a reproducible build). Building
  against an older Angular 17-20 CLI should work given the peer dependency floor, but hasn't been
  verified by Utah — budget time to shake out CLI/ng-packagr version drift if you target an older
  Angular version.
- If NC's own Angular workspace already uses a `projects/` multi-project layout (common for
  in-house component libraries), skip step 1 and run `ng generate library ng-national-wid`
  directly inside it — that's the intended integration path, not a fresh workspace per library.
