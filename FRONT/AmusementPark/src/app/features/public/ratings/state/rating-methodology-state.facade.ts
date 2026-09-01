import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, forkJoin } from 'rxjs';

import { RatingMethodology } from '@app/models/ratings/rating-methodology.models';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { applySsrPublicDataErrorStatus } from '@core/ssr/ssr-public-error-status';
import { RATING_METHODOLOGY_PORT, RatingMethodologyPort } from './rating-methodology-state-data.ports';

interface RatingMethodologyViewModel {
  selected: RatingMethodology;
  history: RatingMethodology[];
}

@Injectable()
export class RatingMethodologyStateFacade {
  private readonly methodologySignal = signal<RatingMethodology | null>(null);
  private readonly historySignal = signal<RatingMethodology[]>([]);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private readonly notFoundSignal = signal<boolean>(false);

  public readonly methodology: Signal<RatingMethodology | null> = this.methodologySignal.asReadonly();
  public readonly history: Signal<RatingMethodology[]> = this.historySignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly error: Signal<boolean> = this.errorSignal.asReadonly();
  public readonly notFound: Signal<boolean> = this.notFoundSignal.asReadonly();

  constructor(
    @Inject(RATING_METHODOLOGY_PORT) private readonly methodologyPort: RatingMethodologyPort,
    private readonly ssrHttpStatusService: SsrHttpStatusService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(version: string | null): void {
    const normalizedVersion: string | null = version?.trim() || null;
    const selectedRequest: Observable<RatingMethodology> = normalizedVersion
      ? this.methodologyPort.getMethodology(normalizedVersion, anonymousHttpOptions())
      : this.methodologyPort.getCurrentMethodology(anonymousHttpOptions());

    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    this.notFoundSignal.set(false);
    forkJoin({
      selected: selectedRequest,
      history: this.methodologyPort.getMethodologyHistory(anonymousHttpOptions())
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (viewModel: RatingMethodologyViewModel): void => {
        this.methodologySignal.set(viewModel.selected);
        this.historySignal.set(viewModel.history);
        this.loadingSignal.set(false);
      },
      error: (error: unknown): void => {
        console.error('Error loading rating methodology', error);
        applySsrPublicDataErrorStatus(error, this.ssrHttpStatusService);
        this.methodologySignal.set(null);
        this.historySignal.set([]);
        this.notFoundSignal.set(error instanceof HttpErrorResponse && error.status === 404);
        this.errorSignal.set(true);
        this.loadingSignal.set(false);
      }
    });
  }
}
