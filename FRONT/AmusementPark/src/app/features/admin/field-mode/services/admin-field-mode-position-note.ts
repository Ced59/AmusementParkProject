import { Injectable } from '@angular/core';

import { AdminFieldModeGeolocationPermissionState, AdminFieldModeGeolocationPort } from '../state/admin-field-mode-data.ports';

interface BrowserPolicy {
  allowsFeature?(feature: string): boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AdminFieldModePositionService implements AdminFieldModeGeolocationPort {
  async getPermissionState(): Promise<AdminFieldModeGeolocationPermissionState> {
    if (typeof globalThis === 'undefined' || !globalThis.isSecureContext) {
      return 'insecure-context';
    }

    const nav: Navigator | undefined = globalThis.navigator;
    if (!nav?.geolocation) {
      return 'unavailable';
    }

    if (this.isBlockedByPermissionsPolicy()) {
      return 'blocked-by-policy';
    }

    if (!nav.permissions?.query) {
      return 'unsupported';
    }

    try {
      const permissionStatus: PermissionStatus = await nav.permissions.query({ name: 'geolocation' as PermissionName });
      return permissionStatus.state;
    } catch {
      return 'unsupported';
    }
  }

  getCurrentPosition(options?: PositionOptions): Promise<GeolocationPosition> {
    const nav: Navigator | undefined = typeof globalThis !== 'undefined'
      ? globalThis.navigator
      : undefined;
    const provider: Geolocation | undefined = nav?.geolocation;

    if (!provider) {
      return Promise.reject(new Error('Position is not available in this browser.'));
    }

    return new Promise((resolve: PositionCallback, reject: PositionErrorCallback): void => {
      provider.getCurrentPosition(resolve, reject, options);
    });
  }

  private isBlockedByPermissionsPolicy(): boolean {
    const currentDocument = typeof globalThis !== 'undefined'
      ? globalThis.document as (Document & { permissionsPolicy?: BrowserPolicy; featurePolicy?: BrowserPolicy }) | undefined
      : undefined;
    const policy: BrowserPolicy | undefined = currentDocument?.permissionsPolicy ?? currentDocument?.featurePolicy;

    if (!policy?.allowsFeature) {
      return false;
    }

    try {
      return !policy.allowsFeature('geolocation');
    } catch {
      return false;
    }
  }
}
