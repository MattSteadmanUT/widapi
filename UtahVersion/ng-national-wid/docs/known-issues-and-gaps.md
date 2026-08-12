# Known Issues & Gaps

Found by walking through the library end-to-end as if integrating it for the first time. A few
concrete bugs were fixed directly as part of this handoff (see below); everything else here needs
an NC product/design decision, not a blind code fix, so it's documented rather than changed.

## Fixed as part of this handoff

- **Missing theme CSS.** The template used `w3-govopso`/`w3-govopsb` (custom classes, never part of
  W3.CSS) on the header and every primary "Query" button, plus `w3-theme`/`w3-theme-l3/l4/l5`
  (real W3.CSS classes, but they require an *additional* theme stylesheet beyond the base `w3.css`
  this library documents loading). Neither was ever shipped in this library — the component only
  rendered correctly inside Utah's ULMITA host app because that app happened to already load
  compatible CSS elsewhere. Fixed by defining all of these classes directly in
  `national-wid.component.css` (Angular's view encapsulation scopes them to this component only, so
  they can't leak into or conflict with a host app's own styles) with a self-contained default blue
  palette. The component now renders fully styled with only the two CDN links `INTEGRATION.md`
  already documents. Replace the color values whenever convenient — nothing else depends on them.
- **Back button wasn't keyboard-accessible.** It was an `<a>` with no `href`, only a `(click)`
  handler — not in the default tab order, doesn't respond to Enter/Space in most browsers. Changed
  to a `<button type="button">`, which is natively focusable and operable. Also relabeled from
  "My Resources" (a specific section name from Utah's ULMITA portal that assumes an IA NC doesn't
  have) to a neutral "Back."
- **`INTEGRATION.md`'s `NationalWidApiService` advanced-usage example was wrong** — it showed a
  2-argument call returning an `Observable` with `.subscribe()`; the actual method takes one
  argument (build the query string into the path yourself) and returns a `Promise<{success, data,
  error}>`. Any developer who copied the original example verbatim would hit a compile error.
  Corrected.
- **`rxjs` was an undocumented dependency.** `national-wid-api.service.ts` imports `firstValueFrom`
  (an RxJS 7+ API), but `package.json` never declared `rxjs` as a peer dependency at all. Added
  `rxjs: >=7.0.0` (the floor `firstValueFrom` actually requires — it doesn't exist in RxJS 6).
- **Redundant service provider.** `NationalWidModule` explicitly listed `NationalWidApiService` in
  its own `providers` array even though the service is already `@Injectable({ providedIn: 'root' })`
  — redundant, and a latent risk of creating duplicate instances if the module is ever imported by
  more than one lazy-loaded feature module. Removed; `providedIn: 'root'` is sufficient.
- **Hardcoded "100k" export-limit text** in the download bar assumed the API's `Api:ExportRowLimit`
  default and would silently go stale if a deployment changes that config value. Reworded to not
  hardcode a number.

## Not fixed — needs an NC decision, documented instead

1. **Branding/naming.** The package is still named `@ulmita/ng-national-wid` with
   `repository.url` pointing at `utahdws/fed-ulmita-ng`, and doc comments still reference ULMITA in
   places. None of this blocks functionality, but rename the package scope and repository URL
   before NC publishes it under their own registry — see
   [standalone-build-guide.md](./standalone-build-guide.md) for the rebuild process this touches.
2. **CDN dependency risk.** `INTEGRATION.md` and this module's own doc comment tell consumers to
   load `https://www.w3schools.com/w3css/4/w3.css` — a tutorial-site URL, not a CDN provider with
   an uptime SLA, and with no Subresource Integrity hash. The Font Awesome CDN link (cdnjs) is more
   reliable but also has no SRI hash and no documented process for bumping the version pin. Worth
   vendoring both (`npm install w3-css @fortawesome/fontawesome-free`, already mentioned as an
   alternative in `INTEGRATION.md`) rather than relying on either CDN for a production state-agency
   deployment.
3. **Accessibility.** No `aria-*` attributes, no live regions on loading/error state changes, no
   table captions/`scope` attributes, anywhere in the template (confirmed by an exhaustive grep, not
   an impression) — beyond the one keyboard-accessibility bug fixed above. Native `<label>` elements
   on every form input do give inputs accessible names, but that appears incidental rather than
   deliberate. No accessibility statement or Section 508/VPAT evaluation exists anywhere in the
   docs. If NC is bound by state ADA/508 requirements (likely, for a state-agency tool), budget for
   a real accessibility audit before broad rollout — this wasn't done by Utah and isn't something to
   retrofit blindly without dedicated testing.
4. **No handling for expired/invalid auth tokens.** Every error — a 401 from an expired token, a
   500, a network timeout, a validation failure — surfaces identically as a generic red text panel
   (`NationalWidApiService.extractErrorMessage` doesn't branch on HTTP status). There's no
   interceptor, no redirect-to-login prompt, no distinguishing UI state, and no client-side request
   timeout (a hung request leaves the loading spinner showing indefinitely). A user whose session
   expires mid-use gets no cue that re-authentication is what's needed. This is real feature work
   (an auth-aware HTTP interceptor, a timeout policy, distinct error UI states), not a quick fix —
   worth prioritizing if NC expects long analyst sessions.
5. **No mobile-responsive design.** Zero `@media` queries in the component's CSS (confirmed).
   Filter forms reflow reasonably via CSS grid/flexbox, but the 10-14-column data tables have no
   responsive fallback — on a narrow viewport every table just scrolls horizontally. Not broken,
   just not optimized; there's no evidence this was ever tested on a phone/tablet, and nothing in
   the docs claims mobile support either way.
6. **No end-user documentation exists** — everything in this library's docs is for developers
   embedding the component, nothing is written for the state analysts who'll actually use it.
   In-template hints show raw API parameter syntax (`periodType=03`, `sort=periodYear:desc`)
   assuming the reader already knows BLS/FIPS/SOC/NAICS coding conventions, and result abbreviations
   like `SA`/`NSA` and the `⚑` suppression-flag marker are never explained anywhere in the UI. If
   analysts using this tool don't already have BLS data-literacy, budget for either in-app help text
   or a separate user guide — this is a training/support-cost gap, not a code gap.
7. **`NgModule`-based, not standalone.** `NationalWidComponent` sets `standalone: false` and is
   wired through a classic `NgModule`. This still works fully — Angular hasn't deprecated or removed
   `NgModule` — but new Angular projects have defaulted to the standalone-component pattern since
   v17, and `INTEGRATION.md` still instructs importing the deprecated `HttpClientModule` rather than
   the standalone `provideHttpClient()`. Not urgent, but if NC's own codebase is standalone-first,
   converting this library to match (or at least offering a standalone entry point) is a
   forward-compatibility item worth scheduling rather than an immediate blocker.
