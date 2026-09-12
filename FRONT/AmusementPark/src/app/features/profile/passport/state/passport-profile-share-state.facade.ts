import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';

import {
  PassportProfileShareInput,
  PassportProfileSharePreview,
  PassportProfileShareSelection,
  ShareContentField,
  SharePublicationPreview,
  SharePublicationPreviewRequest,
  SharePublicationPublishRequest,
  SharePublicationSettings,
  ShareVisibility
} from '@app/models/sharing/share-publication.models';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { PASSPORT_PROFILE_SHARE_PORT, PassportProfileSharePort } from './passport-profile-share-state-data.ports';

@Injectable()
export class PassportProfileShareStateFacade {
  private readonly selectionSignal = signal<PassportProfileShareSelection | null>(null);
  private readonly settingsSignal = signal<SharePublicationSettings | null>(null);
  private readonly previewSignal = signal<SharePublicationPreview | null>(null);
  private readonly selectedYearsSignal = signal<number[]>([]);
  private readonly selectedParkIdsSignal = signal<string[]>([]);
  private readonly selectedRatingKeysSignal = signal<string[]>([]);
  private readonly includedFieldsSignal = signal<ShareContentField[]>([]);
  private readonly publicCaptionSignal = signal<string>('');
  private readonly visibilitySignal = signal<ShareVisibility>('Unlisted');
  private readonly allowsComparisonsSignal = signal<boolean>(false);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly previewingSignal = signal<boolean>(false);
  private readonly savingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private requestGeneration: number = 0;
  private mutationGeneration: number = 0;

  public readonly selection: Signal<PassportProfileShareSelection | null> = this.selectionSignal.asReadonly();
  public readonly settings: Signal<SharePublicationSettings | null> = this.settingsSignal.asReadonly();
  public readonly selectedYears: Signal<number[]> = this.selectedYearsSignal.asReadonly();
  public readonly selectedParkIds: Signal<string[]> = this.selectedParkIdsSignal.asReadonly();
  public readonly selectedRatingKeys: Signal<string[]> = this.selectedRatingKeysSignal.asReadonly();
  public readonly includedFields: Signal<ShareContentField[]> = this.includedFieldsSignal.asReadonly();
  public readonly publicCaption: Signal<string> = this.publicCaptionSignal.asReadonly();
  public readonly visibility: Signal<ShareVisibility> = this.visibilitySignal.asReadonly();
  public readonly allowsComparisons: Signal<boolean> = this.allowsComparisonsSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly previewing: Signal<boolean> = this.previewingSignal.asReadonly();
  public readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  public readonly error: Signal<boolean> = this.errorSignal.asReadonly();
  public readonly preview: Signal<PassportProfileSharePreview | null> = computed(
    () => this.previewSignal()?.passportProfile ?? null
  );
  public readonly canPreview: Signal<boolean> = computed(
    () => this.selectedYearsSignal().length > 0
      && this.selectedParkIdsSignal().length > 0
      && !this.previewingSignal()
      && !this.savingSignal()
  );
  public readonly canPublish: Signal<boolean> = computed(
    () => this.preview() !== null && !this.preview()?.isEmpty && !this.savingSignal()
  );

  constructor(
    @Inject(PASSPORT_PROFILE_SHARE_PORT) private readonly port: PassportProfileSharePort,
    private readonly toastMessageService: ToastMessageService,
    private readonly translateService: TranslateService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  public load(): void {
    const generation: number = ++this.requestGeneration;
    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    forkJoin({ settings: this.port.getSettings(), selection: this.port.getSelection() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result: { settings: SharePublicationSettings; selection: PassportProfileShareSelection }): void => {
          if (generation !== this.requestGeneration) {
            return;
          }
          this.settingsSignal.set(result.settings);
          this.selectionSignal.set(result.selection);
          const availableYears: Set<number> = new Set(result.selection.years.map((candidate) => candidate.year));
          const availableParkIds: Set<string> = new Set(result.selection.parks.map((candidate) => candidate.parkId));
          const availableRatingKeys: Set<string> = new Set(
            result.selection.ratings.map((candidate) => candidate.selectionKey)
          );
          this.selectedYearsSignal.set(
            (result.selection.savedSelectedYears ?? [...availableYears])
              .filter((year) => availableYears.has(year))
          );
          this.selectedParkIdsSignal.set(
            (result.selection.savedSelectedParkIds
              ?? [...availableParkIds].slice(0, result.selection.maximumSelectedParks))
              .filter((parkId) => availableParkIds.has(parkId))
          );
          this.selectedRatingKeysSignal.set(
            (result.selection.savedSelectedRatingKeys ?? [])
              .filter((selectionKey) => availableRatingKeys.has(selectionKey))
          );
          this.publicCaptionSignal.set(result.selection.savedPublicCaption ?? '');
          this.visibilitySignal.set(result.selection.savedVisibility ?? 'Unlisted');
          this.allowsComparisonsSignal.set(result.selection.savedAllowsComparisons);
          this.includedFieldsSignal.set(this.resolveFields(result.settings));
          this.loadingSignal.set(false);
        },
        error: (): void => {
          if (generation !== this.requestGeneration) {
            return;
          }
          this.loadingSignal.set(false);
          this.errorSignal.set(true);
        }
      });
  }

  public toggleYear(year: number): void { this.toggleNumber(this.selectedYearsSignal, year); }
  public togglePark(parkId: string): void { this.toggleString(this.selectedParkIdsSignal, parkId); }
  public toggleRating(selectionKey: string): void { this.toggleString(this.selectedRatingKeysSignal, selectionKey); }
  public toggleField(field: ShareContentField): void {
    const wasIncluded: boolean = this.includedFieldsSignal().includes(field);
    this.toggleString(this.includedFieldsSignal, field);
    if (field === 'GlobalRatings' && wasIncluded) {
      this.selectedRatingKeysSignal.set([]);
    }
  }

  public setPublicCaption(value: string): void {
    this.publicCaptionSignal.set(value.slice(0, 500));
    this.invalidatePreview();
  }

  public setVisibility(value: ShareVisibility): void {
    this.visibilitySignal.set(value);
    this.invalidatePreview();
  }

  public setAllowsComparisons(value: boolean): void {
    this.allowsComparisonsSignal.set(value);
    this.invalidatePreview();
  }

  public previewPublication(): void {
    if (!this.canPreview()) {
      return;
    }
    const generation: number = ++this.requestGeneration;
    this.previewingSignal.set(true);
    this.errorSignal.set(false);
    this.previewSignal.set(null);
    this.port.preview(this.buildPreviewRequest()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (preview: SharePublicationPreview): void => {
        if (generation !== this.requestGeneration) {
          return;
        }
        this.previewSignal.set(preview);
        this.previewingSignal.set(false);
      },
      error: (): void => {
        if (generation !== this.requestGeneration) {
          return;
        }
        this.previewingSignal.set(false);
        this.errorSignal.set(true);
      }
    });
  }

  public publish(): void {
    const preview: SharePublicationPreview | null = this.previewSignal();
    if (!preview || !this.canPublish()) {
      return;
    }
    const generation: number = ++this.mutationGeneration;
    this.savingSignal.set(true);
    const request: SharePublicationPublishRequest = {
      publicationType: preview.publicationType,
      sourceId: null,
      approvedSourceVersion: preview.sourceVersion,
      approvedPolicySchemaVersion: preview.contentPolicy.schemaVersion,
      approvedDatePrecision: preview.contentPolicy.datePrecision,
      approvedIncludedFields: preview.contentPolicy.includedFields,
      approvalToken: preview.approvalToken,
      passportProfile: this.buildInput()
    };
    this.port.publish(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (settings: SharePublicationSettings): void => {
        if (generation !== this.mutationGeneration) {
          return;
        }
        this.settingsSignal.set(settings);
        this.previewSignal.set(null);
        this.savingSignal.set(false);
        this.toast('success', 'passportProfileShare.toast.published');
      },
      error: (): void => {
        if (generation !== this.mutationGeneration) {
          return;
        }
        this.savingSignal.set(false);
        this.errorSignal.set(true);
        this.toast('error', 'passportProfileShare.toast.publishError');
      }
    });
  }

  public revoke(): void {
    if (this.savingSignal()) {
      return;
    }
    const generation: number = ++this.mutationGeneration;
    this.savingSignal.set(true);
    this.port.revoke().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (settings: SharePublicationSettings): void => {
        if (generation !== this.mutationGeneration) {
          return;
        }
        this.settingsSignal.set(settings);
        this.savingSignal.set(false);
        this.toast('success', 'passportProfileShare.toast.revoked');
      },
      error: (): void => {
        if (generation !== this.mutationGeneration) {
          return;
        }
        this.savingSignal.set(false);
        this.toast('error', 'passportProfileShare.toast.revokeError');
      }
    });
  }

  private buildPreviewRequest(): SharePublicationPreviewRequest {
    return {
      publicationType: 'PassportProfile',
      sourceId: null,
      datePrecision: 'Year',
      includedFields: this.includedFieldsSignal(),
      passportProfile: this.buildInput()
    };
  }

  private buildInput(): PassportProfileShareInput {
    return {
      selectedYears: this.selectedYearsSignal(),
      selectedParkIds: this.selectedParkIdsSignal(),
      selectedRatingKeys: this.includedFieldsSignal().includes('GlobalRatings')
        ? this.selectedRatingKeysSignal()
        : [],
      publicCaption: this.includedFieldsSignal().includes('PublicCaption')
        ? this.publicCaptionSignal().trim() || null
        : null,
      visibility: this.visibilitySignal(),
      allowsComparisons: this.allowsComparisonsSignal()
    };
  }

  private resolveFields(settings: SharePublicationSettings): ShareContentField[] {
    return settings.policySchemaVersion == null
      ? ['PublicDisplayName', 'Avatar', 'RideCount', 'TemporalRatings', 'GeographicStatistics']
      : [...settings.includedFields];
  }

  private toggleNumber(target: { (): number[]; set(values: number[]): void }, value: number): void {
    const values: number[] = target();
    target.set(values.includes(value) ? values.filter((item) => item !== value) : [...values, value]);
    this.invalidatePreview();
  }

  private toggleString<T extends string>(target: { (): T[]; set(values: T[]): void }, value: T): void {
    const values: T[] = target();
    target.set(values.includes(value) ? values.filter((item) => item !== value) : [...values, value]);
    this.invalidatePreview();
  }

  private invalidatePreview(): void {
    this.requestGeneration++;
    this.previewSignal.set(null);
    this.previewingSignal.set(false);
    this.errorSignal.set(false);
  }

  private toast(severity: 'success' | 'error', messageKey: string): void {
    this.toastMessageService.add(
      severity,
      this.translateService.instant(severity === 'success' ? 'common.success' : 'common.error'),
      this.translateService.instant(messageKey)
    );
  }
}
