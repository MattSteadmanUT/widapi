# WID 3.0 API Specification

A collaborative repository for developing the national **Workforce Information Database (WID) 3.0** API specification. Multiple state workforce agencies contribute to and review this spec together.

For background on the WID standard, see [widcenter.org](https://widcenter.org).

---

## Viewing the API docs

GitHub Pages is enabled on this repository. The Swagger UI is available at:

```
https://mattsteadmanut.github.io/widapi/
```

The "Try it out" feature in Swagger UI will send requests directly from your browser to whatever server URL is set in the spec. The target API must have CORS enabled for browser-based testing to work.

**Note:** The Swagger UI loads a JSON version of the spec for GitHub Pages compatibility. See the "[Spec format and generation](#spec-format-and-generation)" section below for details.

If the page loads but says "No API definition provided", open one of these direct URLs to verify the JSON is reachable:

```
https://mattsteadmanut.github.io/widapi/specs/wid-3.0/draft.json
https://mattsteadmanut.github.io/widapi/specs/reference/oregon-wid-2.8.json
```

You can also force which spec Swagger loads first via query param:

```
https://mattsteadmanut.github.io/widapi/?spec=specs/wid-3.0/draft.json
https://mattsteadmanut.github.io/widapi/?spec=specs/reference/oregon-wid-2.8.json
```

---

## Repository structure

```
widapi/
├── index.html                        # Swagger UI entry point (GitHub Pages)
├── specs/
│   ├── wid-3.0/
│   │   ├── draft.yaml                # National WID 3.0 working draft (source of truth)
│   │   └── draft.json                # Auto-generated from YAML for GitHub Pages
│   └── reference/
│       ├── README.md                 # How to add a reference spec
│       ├── <state>-wid-<ver>.yaml    # State-contributed reference specs (source of truth)
│       └── <state>-wid-<ver>.json    # Auto-generated from YAML for GitHub Pages
├── docs/
│   ├── decisions/                    # Design decisions and rationale
│   └── field-mapping/                # WID 2.8 → 3.0 field mapping notes
├── .github/workflows/
│   └── sync-spec-json.yml            # GitHub Action: converts YAML → JSON on main branch pushes
├── vendor/swagger-ui/                # Vendored Swagger UI assets
├── scripts/update-vendor.js          # Helper to refresh vendored assets
└── package.json
```

---

## Contributing

All changes go through pull requests — no one pushes directly to `main`.

### Proposing a change to the WID 3.0 draft

1. Fork this repository (or create a branch if you are a direct collaborator)
2. Edit `specs/wid-3.0/draft.yaml`
3. If the change maps from a WID 2.8 field, update the relevant file in `docs/field-mapping/`
4. Open a pull request — the PR template will guide you through what to include
5. At least one reviewer must approve before the PR can be merged

### Adding a reference spec

1. Add your state's YAML file to `specs/reference/` following the naming convention in that folder's README
2. Uncomment (or add) the matching entry in `index.html` so it appears in the Swagger UI dropdown
3. Open a pull request

### Adding a design decision record

Add a Markdown file to `docs/decisions/` following the format described in that folder's README.

---

## Spec format and generation

**YAML is the source of truth** for all OpenAPI specifications in this repository. JSON versions are automatically generated and used by GitHub Pages for Swagger UI compatibility.

### Why two formats?

- **YAML** is human-readable and used for collaborative editing and version control
- **JSON** is required for GitHub Pages + Swagger UI compatibility (the YAML parser on GitHub Pages has known limitations)

### The YAML → JSON workflow

When YAML specs are pushed to the `main` branch, a GitHub Action automatically:

1. Detects changes to `specs/**/*.yaml` files
2. Converts each YAML file to JSON using Python's `yaml` and `json` libraries  
3. Commits the updated JSON files back to `main` with message: `chore: sync JSON specs from YAML [skip ci]`

**Action file:** `.github/workflows/sync-spec-json.yml`

**Status:** The workflow will be activated once the change containing this workflow file is merged to the `main` branch. After that, JSON files will be automatically kept in sync with YAML changes.

### What you should do

- **Always edit YAML files directly** — never manually edit the JSON files
- **Commit YAML changes** to your PR
- **The JSON files will be auto-generated** when the PR is merged to `main` (once the workflow is active on main)

### Manual sync (if needed)

If you need to regenerate JSON manually:

```bash
python3 - <<'EOF'
import glob, json, yaml
for yaml_path in glob.glob('specs/**/*.yaml', recursive=True):
    json_path = yaml_path.rsplit('.yaml', 1)[0] + '.json'
    with open(yaml_path, 'r', encoding='utf-8') as f:
        data = yaml.safe_load(f)
    with open(json_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
        f.write('\n')
    print(f'Updated: {json_path}')
EOF
```

---

## Upgrading Swagger UI

```bash
npm install swagger-ui-dist@latest
npm run update-vendor
```

Commit the changed files under `vendor/swagger-ui/`.

---

## Syncing External Forum Discussions

This repo includes a local sync utility to pull discussion posts from the
national WID forum S3 location and materialize them as reviewable artifacts in
both docs and spec-supporting JSON.

### Source

- S3 prefix: `s3://ulmita-forum-content/nationalwid/`
- AWS profile used by default: `GovProd`

### Run manually

```bash
npm run sync:forum
```

Optional overrides:

```bash
node scripts/sync-forum-content.js --profile GovProd --source s3://ulmita-forum-content/nationalwid/
```

Generated outputs:

- `docs/reference-documents/forum/nationalwid-forum-sync.md`
- `specs/wid-3.0/supporting/forum/nationalwid-forum-normalized.json`

The Markdown output is thread-formatted so reply relationships are clear by
indentation and explicit `Reply to` lines.

### Run automatically before pushes (local machine)

Install the pre-push hook once:

```bash
npm run install:hooks
```

After this, each `git push` from this local clone will:

1. Run forum sync
2. Block push if forum-derived files changed and are not yet committed

To disable, remove `.git/hooks/pre-push`.
