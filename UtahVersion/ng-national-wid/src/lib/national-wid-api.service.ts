import { Inject, Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { NATIONAL_WID_CONFIG, NationalWidConfig } from './national-wid-config';

/**
 * HTTP API service for the National WID library.
 *
 * Handles base URL resolution, bearer token attachment, and response unwrapping for
 * all calls made by {@link NationalWidComponent}.
 *
 * Consumers do not need to interact with this service directly — it is used internally
 * by the component and is automatically provided when `NationalWidModule` is imported.
 * However, it is exported via `public-api.ts` for advanced integrations.
 */
@Injectable({ providedIn: 'root' })
export class NationalWidApiService {
  constructor(
    private http: HttpClient,
    @Inject(NATIONAL_WID_CONFIG) private config: NationalWidConfig
  ) {}

  /**
   * Performs an authenticated GET request to the WID API.
   * Attaches the bearer token from `config.getToken()` if one is available.
   *
   * @param path  API path relative to `config.apiBaseUrl` (e.g., `'labor-force?stFips=49'`).
   * @returns A result envelope indicating success/failure and the parsed JSON data.
   */
  async get<T>(path: string): Promise<{ success: boolean; data?: T; error?: string }> {
    try {
      const token = await this.config.getToken();
      const headers: Record<string, string> = { 'Content-Type': 'application/json' };
      if (token) {
        headers['Authorization'] = /^(Bearer|ApiKey)\s/i.test(token) ? token : `Bearer ${token}`;
      }

      const data = await firstValueFrom(
        this.http.get<T>(this.config.apiBaseUrl + path, { headers: new HttpHeaders(headers) })
      );
      return { success: true, data };
    } catch (err: any) {
      return { success: false, error: this.extractErrorMessage(err) };
    }
  }

  /**
   * Performs an authenticated GET request that returns a binary blob (file download).
   * Triggers a browser download with the given filename.
   *
   * @param path     API path relative to `config.apiBaseUrl`.
   * @param fileName Suggested filename for the download.
   */
  async download(path: string, fileName: string): Promise<{ success: boolean; error?: string }> {
    try {
      const token = await this.config.getToken();
      const headers: Record<string, string> = {};
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const response = await firstValueFrom(
        this.http.get(this.config.apiBaseUrl + path, {
          headers: new HttpHeaders(headers),
          responseType: 'blob',
          observe: 'response'
        })
      );

      const blob = response.body;
      if (!blob) return { success: false, error: 'Empty download response.' };

      // Infer filename from Content-Disposition if provided.
      const cd = response.headers.get('Content-Disposition');
      const cdFileName = cd?.match(/filename\*?=["']?(?:UTF-8'')?([^;\n"']+)/i)?.[1];
      const resolvedName = cdFileName ?? fileName;

      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = resolvedName;
      document.body.appendChild(anchor);
      anchor.click();
      document.body.removeChild(anchor);
      URL.revokeObjectURL(url);

      return { success: true };
    } catch (err: any) {
      return { success: false, error: this.extractErrorMessage(err) };
    }
  }

  private extractErrorMessage(err: any): string {
    if (err?.error?.title) return err.error.title;
    if (err?.error?.message) return err.error.message;
    if (typeof err?.error === 'string') return err.error;
    if (err?.message) return err.message;
    return 'An unexpected error occurred.';
  }
}
