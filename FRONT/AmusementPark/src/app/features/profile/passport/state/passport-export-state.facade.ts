import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { DestroyRef, Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Observable, catchError, finalize, switchMap, takeWhile, tap, timer } from 'rxjs';

import { PassportExport, PassportExportFormat } from '@app/models/passport/passport-export.models';
import { PASSPORT_PRODUCT_ANALYTICS_PORT } from '@core/analytics/passport-product-analytics.port';
import { PASSPORT_EXPORT_API_PORT } from './passport-export-state-data.ports';

@Injectable()
export class PassportExportStateFacade {
  private readonly api = inject(PASSPORT_EXPORT_API_PORT);
  private readonly destroyRef = inject(DestroyRef);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly document = inject(DOCUMENT);
  private readonly productAnalytics = inject(PASSPORT_PRODUCT_ANALYTICS_PORT);
  private readonly exportState = signal<PassportExport | null>(null);
  private readonly requestingState = signal<boolean>(false);
  private readonly downloadingState = signal<boolean>(false);
  private readonly errorState = signal<string | null>(null);

  public readonly passportExport = this.exportState.asReadonly();
  public readonly requesting = this.requestingState.asReadonly();
  public readonly downloading = this.downloadingState.asReadonly();
  public readonly errorKey = this.errorState.asReadonly();
  public readonly generating = computed<boolean>(() => {
    const status: PassportExport['status'] | undefined = this.exportState()?.status;
    return this.requestingState() || status === 'Pending' || status === 'Processing';
  });
  public readonly ready = computed<boolean>(() => this.exportState()?.status === 'Ready');

  public request(format: PassportExportFormat): void {
    if (this.generating()) {
      return;
    }

    this.requestingState.set(true);
    this.errorState.set(null);
    this.exportState.set(null);
    this.api.requestExport({ format }).pipe(
      tap((passportExport: PassportExport) => {
        this.exportState.set(passportExport);
        this.requestingState.set(false);
        this.productAnalytics.track({
          type: 'passport_export_requested',
          source: 'authenticated',
          format
        });
      }),
      switchMap((passportExport: PassportExport) => this.pollUntilTerminal(passportExport)),
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.requestingState.set(false))
    ).subscribe({
      error: () => this.errorState.set('passport.exports.errors.request')
    });
  }

  public download(): void {
    const passportExport: PassportExport | null = this.exportState();
    if (!passportExport || passportExport.status !== 'Ready' || this.downloadingState()) {
      return;
    }

    this.downloadingState.set(true);
    this.errorState.set(null);
    this.api.downloadExport(passportExport.id).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.downloadingState.set(false))
    ).subscribe({
      next: (content: Blob) => this.save(content, passportExport.fileName),
      error: () => this.errorState.set('passport.exports.errors.download')
    });
  }

  private pollUntilTerminal(initial: PassportExport): Observable<PassportExport> {
    if (this.isTerminal(initial) || !isPlatformBrowser(this.platformId)) {
      this.applyTerminalState(initial);
      return EMPTY;
    }

    return timer(1500, 2000).pipe(
      switchMap(() => this.api.getExport(initial.id)),
      tap((passportExport: PassportExport) => {
        this.exportState.set(passportExport);
        this.applyTerminalState(passportExport);
      }),
      takeWhile((passportExport: PassportExport) => !this.isTerminal(passportExport), true),
      catchError(() => {
        this.exportState.set(null);
        this.errorState.set('passport.exports.errors.status');
        return EMPTY;
      })
    );
  }

  private applyTerminalState(passportExport: PassportExport): void {
    if (passportExport.status === 'Failed') {
      this.errorState.set(
        passportExport.errorCode === 'passport-export.too-large'
          ? 'passport.exports.errors.tooLarge'
          : 'passport.exports.errors.failed'
      );
    } else if (passportExport.status === 'Expired') {
      this.errorState.set('passport.exports.errors.expired');
    }
  }

  private isTerminal(passportExport: PassportExport): boolean {
    return passportExport.status === 'Ready'
      || passportExport.status === 'Failed'
      || passportExport.status === 'Expired';
  }

  private save(content: Blob, fileName: string | null): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    const objectUrl: string = URL.createObjectURL(content);
    const link: HTMLAnchorElement = this.document.createElement('a');
    link.href = objectUrl;
    link.download = fileName || 'amusement-park-passport-export';
    link.rel = 'noopener';
    link.click();
    URL.revokeObjectURL(objectUrl);
  }
}
