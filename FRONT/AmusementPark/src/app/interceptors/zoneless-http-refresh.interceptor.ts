import { ApplicationRef, Inject, Injectable, PLATFORM_ID } from '@angular/core';
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { EMPTY, Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';

@Injectable()
export class ZonelessHttpRefreshInterceptor implements HttpInterceptor {
  constructor(
    private readonly applicationRef: ApplicationRef,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
  }

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    // Côté serveur (SSR), les appels vers l'API externe échouent systématiquement
    // (certificat auto-signé, réseau non disponible). Retourner EMPTY évite :
    // - les erreurs console inutiles
    // - les changements d'état (Loading → Error) pendant le rendu SSR
    // - les NG0100 liés à ces changements d'état durant le cycle de CD SSR
    // Les composants restent en état "Loading" côté serveur ; le navigateur
    // prend le relais après hydratation et charge les vraies données.
    if (!isPlatformBrowser(this.platformId) && this.isApiRequest(req)) {
      return EMPTY;
    }

    return next.handle(req).pipe(
      finalize((): void => {
        this.scheduleRefresh();
      })
    );
  }

  private isApiRequest(req: HttpRequest<unknown>): boolean {
    // Les appels API utilisent des URLs absolues (https://localhost:44390/...).
    // Les fichiers assets/i18n utilisent des chemins relatifs (./assets/...).
    // On ne bloque que les requêtes absolues pour laisser passer les traductions.
    return req.url.startsWith('http://') || req.url.startsWith('https://');
  }

  private scheduleRefresh(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    // Sans zone.js et sans observeOn(asyncScheduler), les réponses HTTP
    // arrivent via des microtasks (Promises de fetch). La mise à jour de l'état
    // se fait donc dans la même chaîne de microtasks. En appelant queueMicrotask
    // APRÈS la mise à jour (dans finalize), le tick() se déclenche proprement
    // après que l'état est stable, sans conflit de cycle de CD.
    queueMicrotask((): void => {
      this.applicationRef.tick();
    });
  }
}
