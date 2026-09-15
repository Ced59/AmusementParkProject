import { InjectionToken } from '@angular/core';

// Supplied by CommonEngine on the platform injector used by BootstrapContext.
// No root factory: it would mask the per-process deployment-pair origin.
export const SSR_API_ORIGIN = new InjectionToken<string>('SSR_API_ORIGIN');
