import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';

import {
  ShareContentField,
  SharePublicationPreview,
  SharePublicationPreviewRequest,
  SharePublicationPublishRequest,
  SharePublicationSettings,
  VisitRecapShareInput,
  VisitRecapShareCandidates,
  VisitRecapShareItem,
  VisitRecapSharePreview
} from '@app/models/sharing/share-publication.models';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { VISIT_RECAP_SHARE_PORT, VisitRecapSharePort } from './visit-recap-share-state-data.ports';

@Injectable()
export class VisitRecapShareStateFacade {
  private readonly settingsSignal = signal<SharePublicationSettings | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly savingSignal = signal<boolean>(false);
  private readonly previewingSignal = signal<boolean>(false);
  private readonly candidatesLoadingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private readonly editorOpenSignal = signal<boolean>(false);
  private readonly datePrecisionSignal = signal<string>('Hidden');
  private readonly includeRideCountSignal = signal<boolean>(true);
  private readonly includeRatingsSignal = signal<boolean>(false);
  private readonly includeMissedItemsSignal = signal<boolean>(false);
  private readonly includeCaptionSignal = signal<boolean>(false);
  private readonly publicCaptionSignal = signal<string>('');
  private readonly selectedParkItemIdsSignal = signal<string[] | null>(null);
  private readonly previewSignal = signal<SharePublicationPreview | null>(null);
  private readonly candidateItemsSignal = signal<VisitRecapShareItem[]>([]);
  private readonly candidateTotalSignal = signal<number>(0);
  private loadGeneration: number = 0;
  private previewGeneration: number = 0;
  private mutationGeneration: number = 0;
  private candidatesGeneration: number = 0;
  private currentVisitId: string = '';
  private editorStateHydrated: boolean = false;

  public readonly settings: Signal<SharePublicationSettings | null> = this.settingsSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  public readonly previewing: Signal<boolean> = this.previewingSignal.asReadonly();
  public readonly candidatesLoading: Signal<boolean> = this.candidatesLoadingSignal.asReadonly();
  public readonly error: Signal<boolean> = this.errorSignal.asReadonly();
  public readonly editorOpen: Signal<boolean> = this.editorOpenSignal.asReadonly();
  public readonly datePrecision: Signal<string> = this.datePrecisionSignal.asReadonly();
  public readonly includeRideCount: Signal<boolean> = this.includeRideCountSignal.asReadonly();
  public readonly includeRatings: Signal<boolean> = this.includeRatingsSignal.asReadonly();
  public readonly includeMissedItems: Signal<boolean> = this.includeMissedItemsSignal.asReadonly();
  public readonly includeCaption: Signal<boolean> = this.includeCaptionSignal.asReadonly();
  public readonly publicCaption: Signal<string> = this.publicCaptionSignal.asReadonly();
  public readonly selectedParkItemIds: Signal<string[] | null> = this.selectedParkItemIdsSignal.asReadonly();
  public readonly preview: Signal<SharePublicationPreview | null> = this.previewSignal.asReadonly();
  public readonly candidateItems: Signal<VisitRecapShareItem[]> = this.candidateItemsSignal.asReadonly();
  public readonly candidateTotal: Signal<number> = this.candidateTotalSignal.asReadonly();
  public readonly candidatesTruncated: Signal<boolean> = computed(() => {
    return this.candidateTotalSignal() > this.candidateItemsSignal().length;
  });
  public readonly visitRecap: Signal<VisitRecapSharePreview | null> = computed(() => {
    return this.previewSignal()?.visitRecap ?? null;
  });
  public readonly canPublish: Signal<boolean> = computed(() => {
    return this.previewSignal() !== null
      && !this.savingSignal()
      && !this.previewingSignal()
      && !this.candidatesLoadingSignal();
  });

  constructor(
    @Inject(VISIT_RECAP_SHARE_PORT) private readonly sharePort: VisitRecapSharePort,
    private readonly toastMessageService: ToastMessageService,
    private readonly translateService: TranslateService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(visitId: string): void {
    this.currentVisitId = visitId.trim();
    this.candidatesGeneration++;
    this.candidatesLoadingSignal.set(false);
    const generation: number = ++this.loadGeneration;
    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    this.sharePort.getSettings(visitId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (settings: SharePublicationSettings): void => {
        if (generation !== this.loadGeneration) {
          return;
        }

        this.settingsSignal.set(settings);
        this.loadingSignal.set(false);
      },
      error: (error: unknown): void => {
        if (generation !== this.loadGeneration) {
          return;
        }

        console.error('Error loading visit recap share settings', error);
        this.loadingSignal.set(false);
        this.errorSignal.set(true);
      }
    });
  }

  openEditor(sourceDatePrecision: string): void {
    const settings: SharePublicationSettings | null = this.settingsSignal();
    const fields: readonly ShareContentField[] = settings?.includedFields ?? [];
    const hasSavedPolicy: boolean = settings?.policySchemaVersion != null;
    this.datePrecisionSignal.set(this.clampDatePrecision(settings?.datePrecision ?? sourceDatePrecision, sourceDatePrecision));
    this.includeRideCountSignal.set(!hasSavedPolicy || fields.includes('RideCount'));
    this.includeRatingsSignal.set(fields.includes('TemporalRatings'));
    this.includeMissedItemsSignal.set(fields.includes('MissedItems'));
    this.includeCaptionSignal.set(fields.includes('PublicCaption'));
    this.publicCaptionSignal.set('');
    this.selectedParkItemIdsSignal.set(null);
    this.candidateItemsSignal.set([]);
    this.candidateTotalSignal.set(0);
    this.previewSignal.set(null);
    this.errorSignal.set(false);
    this.editorOpenSignal.set(true);
    this.editorStateHydrated = false;
    this.loadCandidates();
  }

  closeEditor(): void {
    if (this.savingSignal() || this.previewingSignal() || this.candidatesLoadingSignal()) {
      return;
    }

    this.editorOpenSignal.set(false);
    this.previewSignal.set(null);
  }

  setDatePrecision(value: string): void {
    if (this.isEditingLocked()) {
      return;
    }

    this.datePrecisionSignal.set(value);
    this.invalidatePreview();
  }

  setRideCountIncluded(value: boolean): void {
    if (this.isEditingLocked()) {
      return;
    }

    this.includeRideCountSignal.set(value);
    this.invalidatePreview();
  }

  setRatingsIncluded(value: boolean): void {
    if (this.isEditingLocked()) {
      return;
    }

    this.includeRatingsSignal.set(value);
    this.invalidatePreview();
  }

  setMissedItemsIncluded(value: boolean): void {
    if (this.isEditingLocked()) {
      return;
    }

    this.includeMissedItemsSignal.set(value);
    this.selectedParkItemIdsSignal.set(null);
    this.invalidatePreview();
    this.loadCandidates();
  }

  setCaptionIncluded(value: boolean): void {
    if (this.isEditingLocked()) {
      return;
    }

    this.includeCaptionSignal.set(value);
    this.invalidatePreview();
  }

  setPublicCaption(value: string): void {
    if (this.isEditingLocked()) {
      return;
    }

    this.publicCaptionSignal.set(value.slice(0, 500));
    this.invalidatePreview();
  }

  toggleItem(parkItemId: string, selected: boolean): void {
    if (this.isEditingLocked()) {
      return;
    }

    const currentIds: string[] = this.selectedParkItemIdsSignal()
      ?? this.candidateItemsSignal().map((item: VisitRecapShareItem): string => item.parkItemId)
      ?? [];
    const nextIds: string[] = selected
      ? [...new Set([...currentIds, parkItemId])]
      : currentIds.filter((id: string): boolean => id !== parkItemId);
    this.selectedParkItemIdsSignal.set(nextIds.sort((left: string, right: string): number => left.localeCompare(right)));
    this.invalidatePreview();
  }

  isItemSelected(parkItemId: string): boolean {
    const selectedIds: string[] | null = this.selectedParkItemIdsSignal();
    return selectedIds === null || selectedIds.includes(parkItemId);
  }

  preparePreview(visitId: string): void {
    if (this.previewingSignal() || this.savingSignal() || this.candidatesLoadingSignal()) {
      return;
    }

    const request: SharePublicationPreviewRequest = this.buildPreviewRequest(visitId);
    const generation: number = ++this.previewGeneration;
    this.previewingSignal.set(true);
    this.previewSignal.set(null);
    this.errorSignal.set(false);
    this.sharePort.preview(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (preview: SharePublicationPreview): void => {
        if (generation !== this.previewGeneration) {
          return;
        }

        this.previewingSignal.set(false);
        this.mergeCandidateItems(preview.visitRecap?.items ?? []);
        this.previewSignal.set(preview);
      },
      error: (error: unknown): void => {
        if (generation !== this.previewGeneration) {
          return;
        }

        console.error('Error preparing visit recap share preview', error);
        this.previewingSignal.set(false);
        this.errorSignal.set(true);
      }
    });
  }

  publish(visitId: string): void {
    const preview: SharePublicationPreview | null = this.previewSignal();
    if (!preview || this.savingSignal() || this.previewingSignal()) {
      return;
    }

    const request: SharePublicationPublishRequest = {
      publicationType: preview.publicationType,
      sourceId: visitId,
      approvedSourceVersion: preview.sourceVersion,
      approvedPolicySchemaVersion: preview.contentPolicy.schemaVersion,
      approvedDatePrecision: preview.contentPolicy.datePrecision,
      approvedIncludedFields: preview.contentPolicy.includedFields,
      approvalToken: preview.approvalToken,
      visitRecap: this.buildInput()
    };
    const generation: number = ++this.mutationGeneration;
    this.savingSignal.set(true);
    this.errorSignal.set(false);
    this.sharePort.publish(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (settings: SharePublicationSettings): void => {
        if (generation !== this.mutationGeneration) {
          return;
        }

        this.settingsSignal.set(settings);
        this.savingSignal.set(false);
        this.editorOpenSignal.set(false);
        this.previewSignal.set(null);
        this.toast('success', 'visitRecapShare.toast.published');
      },
      error: (error: unknown): void => {
        if (generation !== this.mutationGeneration) {
          return;
        }

        console.error('Error publishing visit recap', error);
        this.savingSignal.set(false);
        this.previewSignal.set(null);
        this.errorSignal.set(true);
        this.toast('error', 'visitRecapShare.toast.publishError');
      }
    });
  }

  revoke(visitId: string): void {
    if (this.savingSignal()) {
      return;
    }

    const generation: number = ++this.mutationGeneration;
    this.savingSignal.set(true);
    this.errorSignal.set(false);
    this.sharePort.revoke(visitId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (settings: SharePublicationSettings): void => {
        if (generation !== this.mutationGeneration) {
          return;
        }

        this.settingsSignal.set(settings);
        this.savingSignal.set(false);
        this.editorOpenSignal.set(false);
        this.previewSignal.set(null);
        this.toast('success', 'visitRecapShare.toast.revoked');
      },
      error: (error: unknown): void => {
        if (generation !== this.mutationGeneration) {
          return;
        }

        console.error('Error revoking visit recap', error);
        this.savingSignal.set(false);
        this.errorSignal.set(true);
        this.toast('error', 'visitRecapShare.toast.revokeError');
      }
    });
  }

  private buildPreviewRequest(visitId: string): SharePublicationPreviewRequest {
    return {
      publicationType: 'VisitRecap',
      sourceId: visitId,
      datePrecision: this.datePrecisionSignal(),
      includedFields: this.selectedFields(),
      visitRecap: this.buildInput()
    };
  }

  private buildInput(): VisitRecapShareInput {
    return {
      selectedParkItemIds: this.selectedParkItemIdsSignal(),
      publicCaption: this.includeCaptionSignal() ? this.publicCaptionSignal().trim() || null : null
    };
  }

  private selectedFields(): ShareContentField[] {
    const fields: ShareContentField[] = [];
    if (this.includeRideCountSignal()) {
      fields.push('RideCount');
    }
    if (this.includeRatingsSignal()) {
      fields.push('TemporalRatings');
    }
    if (this.includeMissedItemsSignal()) {
      fields.push('MissedItems');
    }
    if (this.includeCaptionSignal()) {
      fields.push('PublicCaption');
    }
    return fields;
  }

  private invalidatePreview(): void {
    this.previewGeneration++;
    this.previewingSignal.set(false);
    this.previewSignal.set(null);
    this.errorSignal.set(false);
  }

  private loadCandidates(): void {
    const visitId: string = this.currentVisitId;
    if (!visitId) {
      return;
    }

    const generation: number = ++this.candidatesGeneration;
    this.candidatesLoadingSignal.set(true);
    this.sharePort.getCandidates(visitId, this.includeMissedItemsSignal()).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (result: VisitRecapShareCandidates): void => {
        if (generation !== this.candidatesGeneration) {
          return;
        }

        this.candidatesLoadingSignal.set(false);
        this.candidateItemsSignal.set(result.items);
        this.candidateTotalSignal.set(result.totalEligibleItemCount);
        if (!this.editorStateHydrated) {
          this.publicCaptionSignal.set(result.savedPublicCaption?.slice(0, 500) ?? '');
          this.selectedParkItemIdsSignal.set(result.hasSavedSnapshot
            ? [...(result.savedSelectedParkItemIds ?? [])]
            : result.isTruncated
              ? result.items.map((item: VisitRecapShareItem): string => item.parkItemId)
              : null);
          this.editorStateHydrated = true;
        } else if (result.isTruncated && this.selectedParkItemIdsSignal() === null) {
          this.selectedParkItemIdsSignal.set(
            result.items.map((item: VisitRecapShareItem): string => item.parkItemId)
          );
        }
      },
      error: (error: unknown): void => {
        if (generation !== this.candidatesGeneration) {
          return;
        }

        console.error('Error loading visit recap share candidates', error);
        this.candidatesLoadingSignal.set(false);
        this.candidateItemsSignal.set([]);
        this.candidateTotalSignal.set(0);
        this.errorSignal.set(true);
      }
    });
  }

  private mergeCandidateItems(items: VisitRecapShareItem[]): void {
    const merged: Map<string, VisitRecapShareItem> = new Map(
      this.candidateItemsSignal().map((item: VisitRecapShareItem): [string, VisitRecapShareItem] => [item.parkItemId, item])
    );
    for (const item of items) {
      merged.set(item.parkItemId, item);
    }

    this.candidateItemsSignal.set([...merged.values()]);
  }

  private clampDatePrecision(savedPrecision: string, sourcePrecision: string): string {
    const values: readonly string[] = ['Hidden', 'Year', 'Month', 'Day'];
    const sourceIndex: number = values.indexOf(sourcePrecision);
    const savedIndex: number = values.indexOf(savedPrecision);
    return savedIndex >= 0 && savedIndex <= sourceIndex ? savedPrecision : sourcePrecision;
  }

  private isEditingLocked(): boolean {
    return this.savingSignal()
      || this.previewingSignal()
      || this.candidatesLoadingSignal();
  }

  private toast(severity: 'success' | 'error', messageKey: string): void {
    this.toastMessageService.add(
      severity,
      this.translateService.instant(severity === 'success' ? 'common.success' : 'common.error'),
      this.translateService.instant(messageKey)
    );
  }
}
