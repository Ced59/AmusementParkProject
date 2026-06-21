import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { SKIP_AUTHORIZATION_HEADER } from '@core/http/auth/auth-request-policy';
import { AdminPublicViewMode } from '../models/admin-public-view-mode.model';
import { AdminPublicViewModeFacade } from '../state/admin-public-view-mode.facade';

const ADMIN_PUBLIC_VIEW_MODE_HEADER = 'X-AmusementPark-Public-View-Mode';

@Injectable()
export class AdminPublicViewSimulationInterceptor implements HttpInterceptor {
  constructor(private readonly publicViewModeFacade: AdminPublicViewModeFacade) {
  }

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (!this.shouldApplySimulation(req)) {
      return next.handle(req);
    }

    const viewMode: AdminPublicViewMode = this.publicViewModeFacade.viewMode();
    if (viewMode === 'anonymousVisitor') {
      return next.handle(req);
    }

    const simulatedRequest: HttpRequest<unknown> = req.clone({
      context: req.context.set(SKIP_AUTHORIZATION_HEADER, false),
      headers: req.headers
        .set(ADMIN_PUBLIC_VIEW_MODE_HEADER, viewMode)
        .set('Cache-Control', 'no-store')
        .set('Pragma', 'no-cache')
    });

    return next.handle(simulatedRequest);
  }

  private shouldApplySimulation(req: HttpRequest<unknown>): boolean {
    return req.context.get(SKIP_AUTHORIZATION_HEADER)
      && (req.method === 'GET' || req.method === 'HEAD');
  }
}
