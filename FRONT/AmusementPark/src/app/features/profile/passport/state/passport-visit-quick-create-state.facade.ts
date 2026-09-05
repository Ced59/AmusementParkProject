import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslateService } from '@ngx-translate/core';
import { Subject, catchError, debounceTime, distinctUntilChanged, map, of, switchMap, take, tap } from 'rxjs';

import { CreatePassportVisitRequest, PassportVisit } from '@app/models/passport/passport-visit.models';
import { AuthService } from '@app/services/auth/auth.service';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import {
  PASSPORT_PRODUCT_ANALYTICS_PORT,
  PassportProductAnalyticsPort
} from '@core/analytics/passport-product-analytics.port';
import { PassportProductSource } from '@core/analytics/passport-product-event.model';
import { extractApiProblemDetails } from '@shared/utils/security/error-display.helpers';
import { PassportParkOption, PassportVisitQuickCreateDraft } from '../models/passport-visit-quick-create.models';
import {
  PASSPORT_ANONYMOUS_DRAFT_SCHEMA_VERSION,
  PassportAnonymousDraft
} from '../anonymous-drafts/models/passport-anonymous-draft.models';
import {
  PASSPORT_ANONYMOUS_DRAFT_STORE_PORT,
  PassportAnonymousDraftStorePort
} from '../anonymous-drafts/state/passport-anonymous-draft-store.ports';
import {
  mapParkToPassportOption,
  mapPassportVisitQuickCreateDraft,
  PassportVisitQuickCreateMappingResult
} from '../mappers/passport-visit-quick-create.mapper';
import {
  PASSPORT_VISIT_OPERATION_ID_PORT,
  PASSPORT_VISIT_QUICK_CREATE_API_PORT,
  PASSPORT_VISIT_QUICK_CREATE_PARKS_PORT,
  PassportVisitOperationIdPort,
  PassportVisitQuickCreateApiPort,
  PassportVisitQuickCreateParksPort
} from './passport-visit-quick-create-state-data.ports';

@Injectable()
export class PassportVisitQuickCreateStateFacade {
  private readonly parkOptionsSignal = signal<PassportParkOption[]>([]);
  private readonly searchingSignal = signal<boolean>(false);
  private readonly searchErrorKeySignal = signal<string | null>(null);
  private readonly savingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly createdVisitSignal = signal<PassportVisit | null>(null);
  private readonly createdLocalDraftIdSignal = signal<string | null>(null);
  private readonly searchTerms = new Subject<string>();
  private pendingFingerprint: string | null = null;
  private pendingIdempotencyKey: string | null = null;
  private pendingDraftId: string | null = null;
  private pendingRideOperationId: string | null = null;

  readonly parkOptions: Signal<PassportParkOption[]> = this.parkOptionsSignal.asReadonly();
  readonly searching: Signal<boolean> = this.searchingSignal.asReadonly();
  readonly searchErrorKey: Signal<string | null> = this.searchErrorKeySignal.asReadonly();
  readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  readonly createdVisit: Signal<PassportVisit | null> = this.createdVisitSignal.asReadonly();
  readonly createdLocalDraftId: Signal<string | null> = this.createdLocalDraftIdSignal.asReadonly();

  constructor(
    @Inject(PASSPORT_VISIT_QUICK_CREATE_API_PORT) private readonly visitsApi: PassportVisitQuickCreateApiPort,
    @Inject(PASSPORT_VISIT_QUICK_CREATE_PARKS_PORT) private readonly parksApi: PassportVisitQuickCreateParksPort,
    @Inject(PASSPORT_VISIT_OPERATION_ID_PORT) private readonly operationIds: PassportVisitOperationIdPort,
    @Inject(PASSPORT_ANONYMOUS_DRAFT_STORE_PORT) private readonly anonymousDrafts: PassportAnonymousDraftStorePort,
    @Inject(PASSPORT_PRODUCT_ANALYTICS_PORT)
    private readonly productAnalytics: PassportProductAnalyticsPort,
    private readonly authService: AuthService,
    private readonly messages: ToastMessageService,
    private readonly translateService: TranslateService,
    private readonly destroyRef: DestroyRef
  ) {
    this.bindParkSearch();
  }

  searchParks(term: string): void {
    this.searchTerms.next(term);
  }

  createVisit(draft: PassportVisitQuickCreateDraft, parkName: string | null = null): void {
    if (this.savingSignal()) {
      return;
    }

    const mapping: PassportVisitQuickCreateMappingResult = mapPassportVisitQuickCreateDraft(draft);
    if (!mapping.request) {
      this.errorKeySignal.set(mapping.errorKey);
      return;
    }

    const request: CreatePassportVisitRequest = mapping.request;
    const fingerprint: string = JSON.stringify(request);
    if (this.pendingFingerprint !== fingerprint || !this.pendingIdempotencyKey) {
      this.pendingFingerprint = fingerprint;
      this.pendingIdempotencyKey = this.operationIds.create();
      this.pendingDraftId = this.operationIds.create();
      this.pendingRideOperationId = this.operationIds.create();
    }

    const idempotencyKey: string = this.pendingIdempotencyKey;
    this.errorKeySignal.set(null);
    this.savingSignal.set(true);

    this.authService.ensureValidAccessToken(false)
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (token: string | null): void => {
          if (!token) {
            this.trackCreation('visit_creation_started', 'anonymous-local', request);
            void this.saveAnonymousDraft(request, parkName);
            return;
          }

          this.trackCreation('visit_creation_started', 'authenticated', request);
          this.sendCreateRequest(request, idempotencyKey);
        },
        error: (): void => {
          this.savingSignal.set(false);
          this.errorKeySignal.set('passport.quickCreate.errors.generic');
        }
      });
  }

  clearCreationResult(): void {
    this.createdVisitSignal.set(null);
    this.createdLocalDraftIdSignal.set(null);
    this.errorKeySignal.set(null);
    this.pendingFingerprint = null;
    this.pendingIdempotencyKey = null;
    this.pendingDraftId = null;
    this.pendingRideOperationId = null;
  }

  clearParkSearch(): void {
    this.searchTerms.next('');
  }

  private bindParkSearch(): void {
    this.searchTerms.pipe(
      map((term: string): string => term.trim()),
      debounceTime(250),
      distinctUntilChanged(),
      tap((term: string): void => {
        this.searchErrorKeySignal.set(null);
        this.searchingSignal.set(term.length >= 2);
        if (term.length < 2) {
          this.parkOptionsSignal.set([]);
        }
      }),
      switchMap((term: string) => {
        if (term.length < 2) {
          return of<PassportParkOption[]>([]);
        }

        return this.parksApi.searchParks(term, 1, 8, true).pipe(
          map((response: ParksApiResponse): PassportParkOption[] => response.data
            .map(mapParkToPassportOption)
            .filter((option: PassportParkOption | null): option is PassportParkOption => option !== null)),
          catchError(() => {
            this.searchErrorKeySignal.set('passport.quickCreate.errors.parkSearch');
            return of<PassportParkOption[]>([]);
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((options: PassportParkOption[]): void => {
      this.parkOptionsSignal.set(options);
      this.searchingSignal.set(false);
    });
  }

  private sendCreateRequest(request: CreatePassportVisitRequest, idempotencyKey: string): void {
    this.visitsApi.createVisit(request, idempotencyKey)
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (visit: PassportVisit): void => {
          if (visit.parkId !== request.parkId) {
            this.savingSignal.set(false);
            this.errorKeySignal.set('passport.quickCreate.errors.generic');
            return;
          }

          this.createdVisitSignal.set(visit);
          this.createdLocalDraftIdSignal.set(null);
          this.savingSignal.set(false);
          this.pendingFingerprint = null;
          this.pendingIdempotencyKey = null;
          this.pendingDraftId = null;
          this.pendingRideOperationId = null;
          this.trackCreation('visit_created', 'authenticated', request);
          this.messages.add(
            'success',
            this.translateService.instant('common.success'),
            this.translateService.instant('passport.quickCreate.success.toast')
          );
        },
        error: (error: unknown): void => {
          this.savingSignal.set(false);
          this.errorKeySignal.set(this.resolveErrorKey(error));
        }
      });
  }

  private async saveAnonymousDraft(
    request: CreatePassportVisitRequest,
    parkName: string | null
  ): Promise<void> {
    if (!this.anonymousDrafts.isAvailable()
      || !this.pendingDraftId
      || !this.pendingIdempotencyKey
      || !this.pendingRideOperationId) {
      this.savingSignal.set(false);
      this.errorKeySignal.set('passport.quickCreate.errors.localStorageUnavailable');
      return;
    }

    const nowUtc: string = new Date().toISOString();
    const localDraft: PassportAnonymousDraft = {
      schemaVersion: PASSPORT_ANONYMOUS_DRAFT_SCHEMA_VERSION,
      id: this.pendingDraftId,
      visitOperationId: this.pendingIdempotencyKey,
      rideOperationId: this.pendingRideOperationId,
      parkName: parkName?.trim() || request.parkId,
      visit: request,
      rides: [],
      createdAtUtc: nowUtc,
      updatedAtUtc: nowUtc
    };

    try {
      await this.anonymousDrafts.save(localDraft);
      this.createdVisitSignal.set(null);
      this.createdLocalDraftIdSignal.set(localDraft.id);
      this.savingSignal.set(false);
      this.pendingFingerprint = null;
      this.pendingIdempotencyKey = null;
      this.pendingDraftId = null;
      this.pendingRideOperationId = null;
      this.trackCreation('visit_created', 'anonymous-local', request);
      void this.trackSecondAnonymousVisitIfReached();
      this.messages.add(
        'success',
        this.translateService.instant('common.success'),
        this.translateService.instant('passport.quickCreate.localSuccess.toast')
      );
    } catch {
      this.savingSignal.set(false);
      this.errorKeySignal.set('passport.quickCreate.errors.localSave');
    }
  }

  private resolveErrorKey(error: unknown): string {
    const errorCode: string | null | undefined = extractApiProblemDetails(error)?.errorCode;
    if (errorCode === 'visit.park-not-found') {
      return 'passport.quickCreate.errors.parkNotFound';
    }

    if (errorCode === 'visit.time-zone-id-invalid') {
      return 'passport.quickCreate.errors.timeZone';
    }

    if (errorCode?.startsWith('visit-date.')) {
      return 'passport.quickCreate.errors.date';
    }

    if (errorCode === 'visit.idempotency-key-conflict') {
      return 'passport.quickCreate.errors.retryConflict';
    }

    if (error instanceof HttpErrorResponse && error.status === 0) {
      return 'passport.quickCreate.errors.network';
    }

    return 'passport.quickCreate.errors.generic';
  }

  private trackCreation(
    type: 'visit_creation_started' | 'visit_created',
    source: PassportProductSource,
    request: CreatePassportVisitRequest
  ): void {
    this.productAnalytics.track({
      type,
      source,
      datePrecision: request.date.precision
    });
  }

  private async trackSecondAnonymousVisitIfReached(): Promise<void> {
    try {
      const drafts: PassportAnonymousDraft[] = await this.anonymousDrafts.list();
      if (drafts.length === 2) {
        this.productAnalytics.track({
          type: 'second_visit_recorded',
          source: 'anonymous-local'
        });
      }
    } catch {
      // Product analytics must never affect the locally persisted visit.
    }
  }
}
