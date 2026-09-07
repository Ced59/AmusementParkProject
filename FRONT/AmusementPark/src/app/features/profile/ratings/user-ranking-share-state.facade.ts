import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';

import { UserRankingShareSettings } from '@app/models/ratings/rating.models';
import {
  ShareContentField,
  SharePublicationPreview,
  SharePublicationPreviewRequest,
  SharePublicationPublishRequest,
  SharePublicationSettings
} from '@app/models/sharing/share-publication.models';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { USER_RANKING_SHARE_PORT, UserRankingSharePort } from './user-ranking-share-state-data.ports';

@Injectable()
export class UserRankingShareStateFacade {
  private readonly settingsSignal = signal<UserRankingShareSettings | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly savingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private readonly editorOpenSignal = signal<boolean>(false);
  private readonly includeDisplayNameSignal = signal<boolean>(true);
  private readonly previewSignal = signal<SharePublicationPreview | null>(null);
  private readonly previewingSignal = signal<boolean>(false);
  private readonly previewErrorSignal = signal<boolean>(false);
  private settingsRequestGeneration: number = 0;
  private previewRequestGeneration: number = 0;
  private publishRequestGeneration: number = 0;

  public readonly settings: Signal<UserRankingShareSettings | null> = this.settingsSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  public readonly error: Signal<boolean> = this.errorSignal.asReadonly();
  public readonly editorOpen: Signal<boolean> = this.editorOpenSignal.asReadonly();
  public readonly includeDisplayName: Signal<boolean> = this.includeDisplayNameSignal.asReadonly();
  public readonly preview: Signal<SharePublicationPreview | null> = this.previewSignal.asReadonly();
  public readonly previewing: Signal<boolean> = this.previewingSignal.asReadonly();
  public readonly previewError: Signal<boolean> = this.previewErrorSignal.asReadonly();
  public readonly canPublishPreview: Signal<boolean> = computed(() => {
    return this.previewSignal() !== null && !this.savingSignal() && !this.previewingSignal();
  });

  constructor(
    @Inject(USER_RANKING_SHARE_PORT) private readonly sharePort: UserRankingSharePort,
    private readonly toastMessageService: ToastMessageService,
    private readonly translateService: TranslateService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  openEditor(): void {
    const savedFields: readonly string[] = this.settingsSignal()?.includedFields ?? [];
    this.includeDisplayNameSignal.set(
      savedFields.length === 0 || savedFields.includes('PublicDisplayName')
    );
    this.previewSignal.set(null);
    this.previewErrorSignal.set(false);
    this.editorOpenSignal.set(true);
  }

  closeEditor(): void {
    if (this.savingSignal() || this.previewingSignal()) {
      return;
    }

    this.editorOpenSignal.set(false);
    this.previewSignal.set(null);
    this.previewErrorSignal.set(false);
  }

  setDisplayNameIncluded(included: boolean): void {
    if (this.savingSignal()) {
      return;
    }

    this.includeDisplayNameSignal.set(included);
    this.previewSignal.set(null);
    this.previewErrorSignal.set(false);
  }

  preparePreview(): void {
    if (this.previewingSignal() || this.savingSignal()) {
      return;
    }

    const requestedFields: ShareContentField[] = this.selectedFields();
    const request: SharePublicationPreviewRequest = {
      publicationType: 'PersonalRanking',
      sourceId: null,
      datePrecision: 'Hidden',
      includedFields: requestedFields
    };
    const requestGeneration: number = ++this.previewRequestGeneration;
    this.previewingSignal.set(true);
    this.previewErrorSignal.set(false);
    this.previewSignal.set(null);
    this.sharePort.preview(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (preview: SharePublicationPreview): void => {
        if (requestGeneration !== this.previewRequestGeneration) {
          return;
        }

        this.previewingSignal.set(false);
        if (!this.hasSameFields(requestedFields, this.selectedFields())
          || !this.hasSameFields(requestedFields, preview.contentPolicy.includedFields)) {
          return;
        }

        this.previewSignal.set(preview);
      },
      error: (error: unknown): void => {
        if (requestGeneration !== this.previewRequestGeneration) {
          return;
        }

        console.error('Error preparing user ranking share preview', error);
        this.previewingSignal.set(false);
        if (!this.hasSameFields(requestedFields, this.selectedFields())) {
          return;
        }

        this.previewErrorSignal.set(true);
      }
    });
  }

  publishApprovedPreview(): void {
    const preview: SharePublicationPreview | null = this.previewSignal();
    if (!preview || this.savingSignal() || this.previewingSignal()) {
      return;
    }

    const request: SharePublicationPublishRequest = {
      publicationType: preview.publicationType,
      sourceId: null,
      approvedSourceVersion: preview.sourceVersion,
      approvedPolicySchemaVersion: preview.contentPolicy.schemaVersion,
      approvedDatePrecision: preview.contentPolicy.datePrecision,
      approvedIncludedFields: preview.contentPolicy.includedFields,
      approvalToken: preview.approvalToken
    };
    const requestGeneration: number = ++this.publishRequestGeneration;
    this.savingSignal.set(true);
    this.previewErrorSignal.set(false);
    this.sharePort.publish(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (settings: SharePublicationSettings): void => {
        if (requestGeneration !== this.publishRequestGeneration) {
          return;
        }

        this.settingsSignal.set(settings);
        this.savingSignal.set(false);
        this.editorOpenSignal.set(false);
        this.previewSignal.set(null);
        this.toastMessageService.add(
          'success',
          this.translateService.instant('common.success'),
          this.translateService.instant('ratings.share.manage.publishedToast')
        );
      },
      error: (error: unknown): void => {
        if (requestGeneration !== this.publishRequestGeneration) {
          return;
        }

        console.error('Error publishing approved user ranking share', error);
        this.savingSignal.set(false);
        this.previewSignal.set(null);
        this.previewErrorSignal.set(true);
        this.toastMessageService.add(
          'error',
          this.translateService.instant('common.error'),
          this.translateService.instant('ratings.share.editor.publishError')
        );
      }
    });
  }

  load(): void {
    const requestGeneration: number = ++this.settingsRequestGeneration;
    this.loadingSignal.set(true);
    this.errorSignal.set(false);

    this.sharePort.getMyShareSettings().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (settings: UserRankingShareSettings): void => {
        if (requestGeneration !== this.settingsRequestGeneration) {
          return;
        }

        this.settingsSignal.set(settings);
        this.loadingSignal.set(false);
      },
      error: (error: unknown): void => {
        if (requestGeneration !== this.settingsRequestGeneration) {
          return;
        }

        console.error('Error loading user ranking share settings', error);
        this.loadingSignal.set(false);
        this.errorSignal.set(true);
      }
    });
  }

  setPublic(isPublic: boolean): void {
    if (this.savingSignal()) {
      return;
    }

    this.savingSignal.set(true);
    this.errorSignal.set(false);
    this.sharePort.setMyShareVisibility(isPublic).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (settings: UserRankingShareSettings): void => {
        this.settingsSignal.set(settings);
        this.savingSignal.set(false);
        this.toastMessageService.add(
          'success',
          this.translateService.instant('common.success'),
          this.translateService.instant(isPublic
            ? 'ratings.share.manage.publishedToast'
            : 'ratings.share.manage.privateToast')
        );
      },
      error: (error: unknown): void => {
        console.error('Error updating user ranking share visibility', error);
        this.savingSignal.set(false);
        this.errorSignal.set(true);
        this.toastMessageService.add(
          'error',
          this.translateService.instant('common.error'),
          this.translateService.instant('ratings.share.manage.error')
        );
      }
    });
  }

  refreshAfterSourceChange(): void {
    this.previewRequestGeneration++;
    this.publishRequestGeneration++;
    this.editorOpenSignal.set(false);
    this.previewSignal.set(null);
    this.previewingSignal.set(false);
    this.savingSignal.set(false);
    this.previewErrorSignal.set(false);
    this.load();
  }

  private selectedFields(): ShareContentField[] {
    const fields: ShareContentField[] = ['GlobalRatings'];
    if (this.includeDisplayNameSignal()) {
      fields.unshift('PublicDisplayName');
    }

    return fields;
  }

  private hasSameFields(left: readonly ShareContentField[], right: readonly ShareContentField[]): boolean {
    return left.length === right.length && left.every((field: ShareContentField): boolean => {
      return right.includes(field);
    });
  }
}
