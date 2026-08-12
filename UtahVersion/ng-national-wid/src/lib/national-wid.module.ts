import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { NationalWidComponent } from './national-wid.component';

/**
 * Angular module that exports the National WID data explorer.
 *
 * ## Installation
 *
 * 1. **Add to your AppModule** (or any feature module):
 *    ```typescript
 *    import { NationalWidModule, provideNationalWid } from '@ulmita/ng-national-wid';
 *
 *    @NgModule({
 *      imports: [NationalWidModule],
 *      providers: [
 *        provideNationalWid({
 *          apiBaseUrl: 'https://your-wid-api.example.gov/',
 *          getToken: () => yourAuthService.getAccessToken()
 *        })
 *      ]
 *    })
 *    export class AppModule {}
 *    ```
 *
 * 2. **Use the component in your templates**:
 *    ```html
 *    <nwid-national-wid (navigateBack)="onBack()"></nwid-national-wid>
 *    ```
 *
 * ## Authentication
 *
 * The `getToken` callback in your config should return a valid bearer token from whatever
 * OIDC identity provider fronts your deployment of the National WID API (Cognito or otherwise
 * -- see that API's own docs for its current identity provider). For deployments using the
 * National WID API key feature instead, return `ApiKey <yourkey>`:
 *
 * ```typescript
 * getToken: async () => `ApiKey ${environment.nationalWidApiKey}`
 * ```
 *
 * ## Navigation
 *
 * The component does not import or depend on `@angular/router`.
 * Listen to the `(navigateBack)` output and call your own routing logic.
 *
 * ## Styling
 *
 * The component ships with scoped CSS that includes its own default color theme (no separate
 * theme stylesheet required), relying only on base W3.CSS for layout classes (`w3-*`, not the
 * theme colors) and Font Awesome for icons (`fa-*`). Add these to your host application:
 *
 * ```html
 * <!-- index.html -->
 * <link rel="stylesheet" href="https://www.w3schools.com/w3css/4/w3.css">
 * <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.0/css/all.min.css">
 * ```
 */
@NgModule({
  declarations: [NationalWidComponent],
  imports: [
    CommonModule,
    FormsModule,
    HttpClientModule,
  ],
  exports: [NationalWidComponent],
})
export class NationalWidModule {}
