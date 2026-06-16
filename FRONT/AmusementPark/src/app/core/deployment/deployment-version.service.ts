import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, NgZone, PLATFORM_ID } from '@angular/core';

import { siteVersion } from '../../../environments/version.generated';

interface DeploymentVersionDocument {
  readonly version?: unknown;
}

export function normalizeDeploymentVersion(version: unknown): string | null {
  if (typeof version !== 'string') {
    return null;
  }

  const normalizedVersion: string = version.trim();
  return normalizedVersion.length > 0 ? normalizedVersion : null;
}

export function shouldReloadForDeploymentVersion(currentVersion: string, deployedVersion: unknown): boolean {
  const normalizedCurrentVersion: string | null = normalizeDeploymentVersion(currentVersion);
  const normalizedDeployedVersion: string | null = normalizeDeploymentVersion(deployedVersion);

  return normalizedCurrentVersion !== null
    && normalizedDeployedVersion !== null
    && normalizedCurrentVersion !== normalizedDeployedVersion;
}

@Injectable({
  providedIn: 'root'
})
export class DeploymentVersionService {
  private readonly versionEndpoint: string = '/version.json';
  private readonly checkIntervalMilliseconds: number = 5 * 60 * 1000;
  private readonly initialCheckDelayMilliseconds: number = 15 * 1000;
  private readonly reloadSessionStorageKey: string = 'amusement-park-reloaded-deployment-version';
  private initialized: boolean = false;

  constructor(
    @Inject(DOCUMENT) private readonly document: Document,
    @Inject(PLATFORM_ID) private readonly platformId: object,
    private readonly ngZone: NgZone
  ) {
  }

  initialize(): void {
    if (!isPlatformBrowser(this.platformId) || this.initialized) {
      return;
    }

    this.initialized = true;

    this.ngZone.runOutsideAngular((): void => {
      window.setTimeout((): void => {
        void this.checkForDeploymentVersionChange();
      }, this.initialCheckDelayMilliseconds);

      this.document.addEventListener('visibilitychange', this.handleVisibilityChange);
      window.addEventListener('pageshow', this.handlePageShow);
      window.setInterval((): void => {
        void this.checkForDeploymentVersionChange();
      }, this.checkIntervalMilliseconds);
    });
  }

  private readonly handleVisibilityChange = (): void => {
    if (!this.document.hidden) {
      void this.checkForDeploymentVersionChange();
    }
  };

  private readonly handlePageShow = (): void => {
    void this.checkForDeploymentVersionChange();
  };

  private async checkForDeploymentVersionChange(): Promise<void> {
    try {
      const response: Response = await fetch(this.versionEndpoint, {
        cache: 'no-store',
        headers: {
          Accept: 'application/json'
        }
      });

      if (!response.ok) {
        return;
      }

      const versionDocument = await response.json() as DeploymentVersionDocument;
      if (shouldReloadForDeploymentVersion(siteVersion, versionDocument.version)) {
        this.reloadOnceForVersion(String(versionDocument.version).trim());
      }
    } catch {
      // Best effort only. A failed version probe must never block the app.
    }
  }

  private reloadOnceForVersion(deployedVersion: string): void {
    const reloadMarker: string = `${siteVersion}->${deployedVersion}`;

    if (sessionStorage.getItem(this.reloadSessionStorageKey) === reloadMarker) {
      return;
    }

    sessionStorage.setItem(this.reloadSessionStorageKey, reloadMarker);
    window.location.reload();
  }
}
