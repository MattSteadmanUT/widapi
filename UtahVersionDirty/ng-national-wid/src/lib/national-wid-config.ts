import { InjectionToken } from '@angular/core';

/**
 * Configuration interface for the National WID Angular library.
 *
 * Provide this via the `NATIONAL_WID_CONFIG` injection token in your Angular module
 * or root injector to control how the library authenticates and routes API calls.
 *
 * @example
 * // app.module.ts
 * providers: [
 *   provideNationalWid({
 *     apiBaseUrl: 'https://api.wid.example.gov/',
 *     getToken: async () => myAuthService.getAccessToken()
 *   })
 * ]
 */
export interface NationalWidConfig {
  /**
   * Base URL of the National WID API, trailing slash required.
   * @example 'https://api.nationalwid.example.gov/'
   */
  apiBaseUrl: string;

  /**
   * Async callback that returns a bearer token (or API key) for authenticated requests.
   * Return `null` to make requests without an Authorization header (public endpoints only).
   *
   * @returns The token string or null if unauthenticated.
   * @example async () => (await cognitoAuth.currentSession()).getAccessToken().getJwtToken()
   */
  getToken: () => Promise<string | null>;
}

/**
 * Injection token used to provide {@link NationalWidConfig} to the National WID library.
 * Register it in your module's `providers` array using {@link provideNationalWid}.
 */
export const NATIONAL_WID_CONFIG = new InjectionToken<NationalWidConfig>('NATIONAL_WID_CONFIG');

/**
 * Convenience factory function that creates an Angular provider for `NATIONAL_WID_CONFIG`.
 *
 * @param config The configuration object.
 * @returns An Angular provider that can be added to `providers` in any module or component.
 *
 * @example
 * // app.module.ts
 * @NgModule({
 *   providers: [
 *     provideNationalWid({
 *       apiBaseUrl: environment.nationalWidApiUrl,
 *       getToken: () => myAuthService.getBearerToken()
 *     })
 *   ]
 * })
 * export class AppModule {}
 */
export function provideNationalWid(config: NationalWidConfig) {
  return { provide: NATIONAL_WID_CONFIG, useValue: config };
}
