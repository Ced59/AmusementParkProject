import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';

import {
  ShareContentField,
  SharePublicationPreview,
  SharePublicationPreviewRequest,
  SharePublicationPublishRequest,
  SharePublicationSettings,
  YearRecapShareInput,
  YearRecapSharePreview,
  YearRecapShareSelection
} from '@app/models/sharing/share-publication.models';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { YEAR_RECAP_SHARE_PORT, YearRecapSharePort } from './year-recap-share-state-data.ports';

@Injectable()
export class YearRecapShareStateFacade {
  private readonly settingsSignal = signal<SharePublicationSettings | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly savingSignal = signal<boolean>(false);
  private readonly previewingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private readonly editorOpenSignal = signal<boolean>(false);
  private readonly includeRideCountSignal = signal<boolean>(true);
  private readonly includeRatingsSignal = signal<boolean>(false);
  private readonly includeGeographySignal = signal<boolean>(true);
  private readonly includeMissedItemsSignal = signal<boolean>(false);
  private readonly includeCaptionSignal = signal<boolean>(false);
  private readonly publicCaptionSignal = signal<string>('');
  private readonly previewSignal = signal<SharePublicationPreview | null>(null);
  private loadGeneration: number = 0;
  private previewGeneration: number = 0;
  private mutationGeneration: number = 0;
  private savedPublicCaption: string = '';

  public readonly settings: Signal<SharePublicationSettings | null> = this.settingsSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  public readonly previewing: Signal<boolean> = this.previewingSignal.asReadonly();
  public readonly error: Signal<boolean> = this.errorSignal.asReadonly();
  public readonly editorOpen: Signal<boolean> = this.editorOpenSignal.asReadonly();
  public readonly includeRideCount: Signal<boolean> = this.includeRideCountSignal.asReadonly();
  public readonly includeRatings: Signal<boolean> = this.includeRatingsSignal.asReadonly();
  public readonly includeGeography: Signal<boolean> = this.includeGeographySignal.asReadonly();
  public readonly includeMissedItems: Signal<boolean> = this.includeMissedItemsSignal.asReadonly();
  public readonly includeCaption: Signal<boolean> = this.includeCaptionSignal.asReadonly();
  public readonly publicCaption: Signal<string> = this.publicCaptionSignal.asReadonly();
  public readonly yearRecap: Signal<YearRecapSharePreview | null> = computed(() =>
    this.previewSignal()?.yearRecap ?? null
  );
  public readonly canPublish: Signal<boolean> = computed(() =>
    this.yearRecap() !== null
      && !this.yearRecap()?.isEmpty
      && !this.savingSignal()
      && !this.previewingSignal()
  );

  constructor(
    @Inject(YEAR_RECAP_SHARE_PORT) private readonly sharePort: YearRecapSharePort,
    private readonly toastMessageService: ToastMessageService,
    private readonly translateService: TranslateService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(year: number): void {
    const generation: number = ++this.loadGeneration;
    this.savedPublicCaption = '';
    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    forkJoin({
      settings: this.sharePort.getSettings(year),
      selection: this.sharePort.getSelection(year)
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: { settings: SharePublicationSettings; selection: YearRecapShareSelection }): void => {
        if (generation !== this.loadGeneration) {
          return;
        }
        this.settingsSignal.set(result.settings);
        this.savedPublicCaption = result.selection.savedPublicCaption?.slice(0, 500) ?? '';
        this.loadingSignal.set(false);
      },
      error: (error: unknown): void => {
        if (generation !== this.loadGeneration) {
          return;
        }
        console.error('Error loading annual recap share settings', error);
        this.loadingSignal.set(false);
        this.errorSignal.set(true);
      }
    });
  }

  openEditor(): void {
    const settings: SharePublicationSettings | null = this.settingsSignal();
    const fields: readonly ShareContentField[] = settings?.includedFields ?? [];
    const hasSavedPolicy: boolean = settings?.policySchemaVersion != null;
    this.includeRideCountSignal.set(!hasSavedPolicy || fields.includes('RideCount'));
    this.includeRatingsSignal.set(fields.includes('TemporalRatings'));
    this.includeGeographySignal.set(!hasSavedPolicy || fields.includes('GeographicStatistics'));
    this.includeMissedItemsSignal.set(fields.includes('MissedItems'));
    this.includeCaptionSignal.set(fields.includes('PublicCaption'));
    this.publicCaptionSignal.set(this.savedPublicCaption);
    this.previewSignal.set(null);
    this.errorSignal.set(false);
    this.editorOpenSignal.set(true);
  }

  closeEditor(): void {
    if (!this.savingSignal() && !this.previewingSignal()) {
      this.editorOpenSignal.set(false);
      this.previewSignal.set(null);
    }
  }

  setRideCountIncluded(value: boolean): void { this.setChoice(this.includeRideCountSignal, value); }
  setRatingsIncluded(value: boolean): void { this.setChoice(this.includeRatingsSignal, value); }
  setGeographyIncluded(value: boolean): void { this.setChoice(this.includeGeographySignal, value); }
  setMissedItemsIncluded(value: boolean): void { this.setChoice(this.includeMissedItemsSignal, value); }
  setCaptionIncluded(value: boolean): void { this.setChoice(this.includeCaptionSignal, value); }

  setPublicCaption(value: string): void {
    if (!this.isLocked()) {
      this.publicCaptionSignal.set(value.slice(0, 500));
      this.invalidatePreview();
    }
  }

  preparePreview(year: number): void {
    if (this.isLocked()) {
      return;
    }
    const request: SharePublicationPreviewRequest = {
      publicationType: 'YearRecap',
      sourceId: `${year}`,
      datePrecision: 'Year',
      includedFields: this.selectedFields(),
      yearRecap: this.buildInput()
    };
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
        this.previewSignal.set(preview);
      },
      error: (error: unknown): void => {
        if (generation !== this.previewGeneration) {
          return;
        }
        console.error('Error preparing annual recap preview', error);
        this.previewingSignal.set(false);
        this.errorSignal.set(true);
      }
    });
  }

  publish(year: number): void {
    const preview: SharePublicationPreview | null = this.previewSignal();
    if (!preview || !this.canPublish()) {
      return;
    }
    const request: SharePublicationPublishRequest = {
      publicationType: preview.publicationType,
      sourceId: `${year}`,
      approvedSourceVersion: preview.sourceVersion,
      approvedPolicySchemaVersion: preview.contentPolicy.schemaVersion,
      approvedDatePrecision: preview.contentPolicy.datePrecision,
      approvedIncludedFields: preview.contentPolicy.includedFields,
      approvalToken: preview.approvalToken,
      yearRecap: this.buildInput()
    };
    const generation: number = ++this.mutationGeneration;
    this.savingSignal.set(true);
    this.sharePort.publish(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (settings: SharePublicationSettings): void => {
        if (generation !== this.mutationGeneration) {
          return;
        }
        this.settingsSignal.set(settings);
        this.savedPublicCaption = this.includeCaptionSignal()
          ? this.publicCaptionSignal().trim()
          : '';
        this.savingSignal.set(false);
        this.editorOpenSignal.set(false);
        this.previewSignal.set(null);
        this.toast('success', 'yearRecapShare.toast.published');
      },
      error: (error: unknown): void => {
        if (generation !== this.mutationGeneration) {
          return;
        }
        console.error('Error publishing annual recap', error);
        this.savingSignal.set(false);
        this.previewSignal.set(null);
        this.errorSignal.set(true);
        this.toast('error', 'yearRecapShare.toast.publishError');
      }
    });
  }

  revoke(year: number): void {
    if (this.savingSignal()) {
      return;
    }
    const generation: number = ++this.mutationGeneration;
    this.savingSignal.set(true);
    this.sharePort.revoke(year).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (settings: SharePublicationSettings): void => {
        if (generation !== this.mutationGeneration) {
          return;
        }
        this.settingsSignal.set(settings);
        this.savingSignal.set(false);
        this.editorOpenSignal.set(false);
        this.toast('success', 'yearRecapShare.toast.revoked');
      },
      error: (error: unknown): void => {
        if (generation !== this.mutationGeneration) {
          return;
        }
        console.error('Error revoking annual recap', error);
        this.savingSignal.set(false);
        this.errorSignal.set(true);
        this.toast('error', 'yearRecapShare.toast.revokeError');
      }
    });
  }

  private setChoice(target: { set(value: boolean): void }, value: boolean): void {
    if (!this.isLocked()) {
      target.set(value);
      this.invalidatePreview();
    }
  }

  private buildInput(): YearRecapShareInput {
    return { publicCaption: this.includeCaptionSignal() ? this.publicCaptionSignal().trim() || null : null };
  }

  private selectedFields(): ShareContentField[] {
    const fields: ShareContentField[] = [];
    if (this.includeRideCountSignal()) fields.push('RideCount');
    if (this.includeRatingsSignal()) fields.push('TemporalRatings');
    if (this.includeGeographySignal()) fields.push('GeographicStatistics');
    if (this.includeMissedItemsSignal()) fields.push('MissedItems');
    if (this.includeCaptionSignal()) fields.push('PublicCaption');
    return fields;
  }

  private invalidatePreview(): void {
    this.previewGeneration++;
    this.previewingSignal.set(false);
    this.previewSignal.set(null);
    this.errorSignal.set(false);
  }

  private isLocked(): boolean {
    return this.savingSignal() || this.previewingSignal();
  }

  private toast(severity: 'success' | 'error', messageKey: string): void {
    this.toastMessageService.add(
      severity,
      this.translateService.instant(severity === 'success' ? 'common.success' : 'common.error'),
      this.translateService.instant(messageKey)
    );
  }
}
