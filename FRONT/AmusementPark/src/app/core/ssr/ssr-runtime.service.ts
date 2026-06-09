import { isPlatformBrowser, isPlatformServer } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID } from '@angular/core';

/**
 * Centralise la détection du contexte SSR.
 *
 * Les pages publiques peuvent charger un sous-ensemble minimal de données côté serveur
 * pour garder le HTML SEO utile sans déclencher les cascades d'appels lourdes
 * (photos de tous les items, tags admin, cartes, proximités, etc.).
 */
@Injectable({ providedIn: 'root' })
export class SsrRuntimeService {
  private readonly serverSideRender: boolean;
  private readonly browserRuntime: boolean;

  constructor(@Inject(PLATFORM_ID) platformId: object) {
    this.serverSideRender = isPlatformServer(platformId);
    this.browserRuntime = isPlatformBrowser(platformId);
  }

  isServerSideRender(): boolean {
    return this.serverSideRender;
  }

  isBrowserRuntime(): boolean {
    return this.browserRuntime;
  }

  shouldUseMinimalPublicData(): boolean {
    return this.serverSideRender;
  }
}
