/*
 * Public API surface for the @ulmita/ng-national-wid library.
 *
 * Consumers import from this module:
 *   import { NationalWidModule, NATIONAL_WID_CONFIG } from '@ulmita/ng-national-wid';
 */

// Primary module — add to your AppModule imports
export { NationalWidModule } from './lib/national-wid.module';

// Component — available after importing NationalWidModule
export { NationalWidComponent } from './lib/national-wid.component';

// Configuration token — provide in your AppModule or root injector
export { NATIONAL_WID_CONFIG, NationalWidConfig, provideNationalWid } from './lib/national-wid-config';

// Optional standalone API service (for custom integrations)
export { NationalWidApiService } from './lib/national-wid-api.service';
