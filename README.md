# widapi

A shared repository for API specifications (Swagger / OpenAPI) and related documentation.

---

## Viewing the API docs

Open **`index.html`** in a browser, or enable [GitHub Pages](https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-github-pages) on this repository (source: `/ (root)`, branch: `main`) to get a publicly-hosted Swagger UI at:

```
https://<org>.github.io/<repo>/
```

---

## Repository structure

```
widapi/
├── index.html              # Swagger UI entry point
├── swagger/
│   └── openapi.yaml        # Default OpenAPI 3 specification
├── vendor/
│   └── swagger-ui/         # Vendored Swagger UI static assets
├── docs/
│   └── README.md           # Home for non-spec documents
├── scripts/
│   └── update-vendor.js    # Helper to refresh vendored assets
└── package.json            # Node metadata (swagger-ui-dist dependency)
```

---

## Adding a new API spec

1. Create a new YAML or JSON file under `swagger/`, e.g. `swagger/payments.yaml`.
2. Open `index.html` and add an entry to the `urls` array:

```js
{ name: "Payments API", url: "swagger/payments.yaml" },
```

3. Commit and push — the new spec will appear in the Swagger UI dropdown.

---

## Upgrading Swagger UI

```bash
npm install swagger-ui-dist@latest
npm run update-vendor
```

Commit the changed files under `vendor/swagger-ui/`.

---

## Adding other documents

Place Markdown, PDF, or any other document under `docs/`. Use sub-directories to keep things organised (see `docs/README.md` for suggestions).

---

## Contributing

1. Fork or branch from `main`.
2. Make your changes.
3. Open a pull request with a short description of what was added or changed.
