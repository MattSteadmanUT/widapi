# @ulmita/ng-national-wid — Integration Guide

A ready-to-drop-in Angular component that renders a full National WID 3.0 data explorer in any Angular 17+ application.

---

## What's Included

| Export | Description |
|---|---|
| `NationalWidModule` | Angular module — import this in your `AppModule` |
| `NationalWidComponent` | The data explorer component (`<nwid-national-wid>`) |
| `NationalWidConfig` | TypeScript interface for configuration |
| `NATIONAL_WID_CONFIG` | Angular injection token |
| `provideNationalWid()` | Provider factory (preferred setup) |
| `NationalWidApiService` | Injectable HTTP service (advanced use) |

---

## Prerequisites

- Angular 17 or later
- CSS loaded from W3.CSS and Font Awesome (see step 4 below)

---

## Setup

### 1. Install the library

After publishing to your npm registry:

```bash
npm install @ulmita/ng-national-wid
```

### 2. Register the module and provide configuration

In your `AppModule`:

```typescript
import { NgModule } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';
import { NationalWidModule, provideNationalWid } from '@ulmita/ng-national-wid';

@NgModule({
  imports: [
    HttpClientModule,   // required
    NationalWidModule,
    // ... your other imports
  ],
  providers: [
    provideNationalWid({
      apiBaseUrl: 'https://your-wid-api.example.gov/',
      getToken: () => yourAuthService.getAccessToken()  // () => Promise<string | null>
    }),
    // ... your other providers
  ]
})
export class AppModule {}
```

`getToken` must return a `Promise<string | null>`. Return `null` if no token is available (the API call will be sent without an Authorization header).

### 3. Add the component to a route

In your routing module:

```typescript
const routes: Routes = [
  {
    path: 'workforce-data',
    component: YourWrapperComponent  // or use the component directly
  }
];
```

In the template of a wrapper component (or directly in a routed component):

```html
<nwid-national-wid (navigateBack)="onWidClose()">
</nwid-national-wid>
```

The `(navigateBack)` output fires when the user clicks the "Back" button inside the component. Handle it however makes sense for your application:

```typescript
onWidClose(): void {
  this.router.navigate(['/dashboard']);
  // or: this.activeModal.close();
  // or: this.location.back();
}
```

### 4. Add CSS dependencies

In `index.html`:

```html
<link rel="stylesheet" href="https://www.w3schools.com/w3css/4/w3.css">
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.0/css/all.min.css">
```

Or install them via npm if you prefer a fully offline build:

```bash
npm install w3css @fortawesome/fontawesome-free
```

Then import in `styles.scss`:

```scss
@import 'w3css/w3';
@import '@fortawesome/fontawesome-free/css/all.min.css';
```

---

## Using an API Key Instead of a Bearer Token

For server-to-server or automation scenarios where you have a static API key:

```typescript
provideNationalWid({
  apiBaseUrl: 'https://your-wid-api.example.gov/',
  getToken: async () => `ApiKey ${environment.nationalWidApiKey}`
})
```

The `Authorization` header value is whatever string `getToken` returns. The component prefixes it with `Authorization: ` automatically.

---

## Standalone / Lazy-Loaded Usage (Angular 17+)

If your app uses standalone components:

```typescript
// In a standalone wrapper component
import { Component } from '@angular/core';
import { NationalWidModule, provideNationalWid } from '@ulmita/ng-national-wid';

@Component({
  standalone: true,
  imports: [NationalWidModule],
  providers: [
    provideNationalWid({
      apiBaseUrl: 'https://your-wid-api.example.gov/',
      getToken: () => yourAuthService.getAccessToken()
    })
  ],
  template: `
    <nwid-national-wid (navigateBack)="router.navigate(['/'])">
    </nwid-national-wid>
  `
})
export class WorkforceDataPage {}
```

---

## NationalWidConfig Interface

```typescript
interface NationalWidConfig {
  /** Full base URL of the National WID API, including trailing slash. */
  apiBaseUrl: string;

  /** 
   * Returns an Authorization header value (e.g., "Bearer eyJ..." or "ApiKey abc...")
   * or null to send the request without an Authorization header.
   */
  getToken: () => Promise<string | null>;
}
```

---

## NationalWidApiService (Advanced)

If you need to call WID API endpoints from your own components, inject `NationalWidApiService` directly:

```typescript
import { NationalWidApiService } from '@ulmita/ng-national-wid';

@Component({ ... })
export class MyComponent {
  constructor(private widApi: NationalWidApiService) {}

  loadLaborForce(stFips: string) {
    this.widApi.get<any>('labor-force', { stFips }).subscribe(response => {
      this.rows = response.data;
    });
  }
}
```

`get<T>(path, params?)` builds the full URL from `apiBaseUrl`, appends query parameters, attaches the Authorization header from `getToken`, and returns an `Observable<T>`.

---

## Publishing to a Private Registry

The built package is at `dist/ng-national-wid/` in the `fed-ulmita-ng` repository.

```bash
cd dist/ng-national-wid

# Publish to Nexus / Artifactory / GitHub Packages
npm publish --registry https://your-registry.example.gov/

# Or pack as a tarball for manual distribution
npm pack
# → ulmita-ng-national-wid-1.0.0.tgz

# Install directly from tarball
npm install ./ulmita-ng-national-wid-1.0.0.tgz
```

---

## Rebuilding the Library

In the `fed-ulmita-ng` repository:

```bash
ng build ng-national-wid
```

Output goes to `dist/ng-national-wid/`. Run this whenever the source changes in `projects/ng-national-wid/src/`.
