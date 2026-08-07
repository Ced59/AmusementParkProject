import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { ToastMessageService } from '@app/services/messages/toast-message.service';
import {
  PublishSocialLinkRequest,
  SocialPublication,
  SocialPublicationDraft,
  SocialPublicationSynchronizationResult,
  SocialPublisher,
  SocialPublishingOverview,
  UpdateSocialPublicationRequest
} from '@app/models/social-publishing/social-publishing.models';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import {
  ADMIN_SOCIAL_PUBLISHING_DATA_PORT,
  AdminSocialPublishingDataPort
} from './admin-social-publishing-data.ports';

@Injectable()
export class AdminSocialPublishingFacade {
  private readonly screenStateStore = new SignalScreenStateStore<SocialPublishingOverview>();
  private readonly publishingSignal = signal<boolean>(false);
  private readonly draftSignal = signal<SocialPublicationDraft | null>(null);
  private readonly draftLoadingSignal = signal<boolean>(false);
  private readonly draftErrorSignal = signal<boolean>(false);
  private readonly selectedImageIdSignal = signal<string | null>(null);
  private readonly retryingPublicationIdSignal = signal<string | null>(null);
  private readonly updatingPublicationIdSignal = signal<string | null>(null);
  private readonly deletingPublicationIdSignal = signal<string | null>(null);
  private readonly synchronizingSignal = signal<boolean>(false);
  private draftRequestId: number = 0;

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly overview: Signal<SocialPublishingOverview | null> = computed(() => this.screenStateStore.data() ?? null);
  public readonly publishers: Signal<SocialPublisher[]> = computed(() => this.overview()?.publishers ?? []);
  public readonly facebookPublisher: Signal<SocialPublisher | null> = computed(
    () => this.publishers().find((publisher: SocialPublisher) => publisher.network === 'Facebook') ?? null
  );
  public readonly recentPublications: Signal<SocialPublication[]> = computed(
    () => this.overview()?.recentPublications ?? []
  );
  public readonly publishing = this.publishingSignal.asReadonly();
  public readonly draft = this.draftSignal.asReadonly();
  public readonly draftLoading = this.draftLoadingSignal.asReadonly();
  public readonly draftError = this.draftErrorSignal.asReadonly();
  public readonly selectedImageId = this.selectedImageIdSignal.asReadonly();
  public readonly retryingPublicationId = this.retryingPublicationIdSignal.asReadonly();
  public readonly updatingPublicationId = this.updatingPublicationIdSignal.asReadonly();
  public readonly deletingPublicationId = this.deletingPublicationIdSignal.asReadonly();
  public readonly synchronizing = this.synchronizingSignal.asReadonly();

  constructor(
    @Inject(ADMIN_SOCIAL_PUBLISHING_DATA_PORT) private readonly dataPort: AdminSocialPublishingDataPort,
    private readonly destroyRef: DestroyRef,
    private readonly translateService: TranslateService,
    private readonly toastMessageService: ToastMessageService
  ) {
  }

  load(): void {
    const previousData: SocialPublishingOverview | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);
    this.dataPort.getOverview().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (overview: SocialPublishingOverview) => this.screenStateStore.setReady(overview),
      error: (error: unknown) => {
        console.error('Error loading social publishing overview', error);
        this.screenStateStore.setError('admin.socialPublishing.loadError', previousData);
      }
    });
  }

  resolveDraft(url: string, imagePage: number = 1): void {
    const normalizedUrl: string = url.trim();
    const requestId: number = ++this.draftRequestId;
    if (!normalizedUrl) {
      this.draftSignal.set(null);
      this.draftLoadingSignal.set(false);
      this.draftErrorSignal.set(false);
      this.selectedImageIdSignal.set(null);
      return;
    }

    const previousUrl: string | null = this.draftSignal()?.url ?? null;
    this.draftLoadingSignal.set(true);
    this.draftErrorSignal.set(false);
    if (imagePage === 1 && previousUrl !== normalizedUrl) {
      this.draftSignal.set(null);
      this.selectedImageIdSignal.set(null);
    }

    this.dataPort.getDraft(normalizedUrl, imagePage, 6).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        if (requestId === this.draftRequestId) {
          this.draftLoadingSignal.set(false);
        }
      })
    ).subscribe({
      next: (draft: SocialPublicationDraft) => {
        if (requestId !== this.draftRequestId) {
          return;
        }

        const targetChanged: boolean = previousUrl !== null && previousUrl !== draft.url;
        this.draftSignal.set(draft);
        if (targetChanged) {
          this.selectedImageIdSignal.set(null);
        }
      },
      error: (error: unknown) => {
        if (requestId !== this.draftRequestId) {
          return;
        }

        console.error('Error resolving social publication draft', error);
        this.draftSignal.set(null);
        this.draftErrorSignal.set(true);
        this.selectedImageIdSignal.set(null);
      }
    });
  }

  selectPreviewImage(imageId: string | null): void {
    if (imageId === null || this.draftSignal()?.images.data.some((image) => image.id === imageId)) {
      this.selectedImageIdSignal.set(imageId);
    }
  }

  publish(request: PublishSocialLinkRequest): void {
    if (this.publishingSignal()) {
      return;
    }

    this.publishingSignal.set(true);
    this.dataPort.publish(request).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.publishingSignal.set(false))
    ).subscribe({
      next: (publication: SocialPublication) => {
        this.applyPublication(publication);
        this.selectedImageIdSignal.set(null);
        this.showPublicationResult(publication);
      },
      error: (error: unknown) => {
        console.error('Error publishing social link', error);
        this.toastMessageService.add(
          'error',
          this.translateService.instant('admin.socialPublishing.toasts.errorSummary'),
          this.translateService.instant('admin.socialPublishing.toasts.publishError')
        );
      }
    });
  }

  retry(publicationId: string): void {
    if (this.retryingPublicationIdSignal()) {
      return;
    }

    this.retryingPublicationIdSignal.set(publicationId);
    this.dataPort.retry(publicationId).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.retryingPublicationIdSignal.set(null))
    ).subscribe({
      next: (publication: SocialPublication) => {
        this.applyPublication(publication);
        this.showPublicationResult(publication);
      },
      error: (error: unknown) => {
        console.error('Error retrying social publication', error);
        this.toastMessageService.add(
          'error',
          this.translateService.instant('admin.socialPublishing.toasts.errorSummary'),
          this.translateService.instant('admin.socialPublishing.toasts.retryError')
        );
      }
    });
  }

  update(publicationId: string, message: string, onSuccess?: () => void): void {
    if (this.updatingPublicationIdSignal()) {
      return;
    }

    const request: UpdateSocialPublicationRequest = { message };
    this.updatingPublicationIdSignal.set(publicationId);
    this.dataPort.update(publicationId, request).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.updatingPublicationIdSignal.set(null))
    ).subscribe({
      next: (publication: SocialPublication) => {
        this.applyPublication(publication);
        onSuccess?.();
        this.toastMessageService.add(
          publication.status === 'Deleted' ? 'info' : 'success',
          this.translateService.instant('admin.socialPublishing.toasts.successSummary'),
          this.translateService.instant(
            publication.status === 'Deleted'
              ? 'admin.socialPublishing.toasts.alreadyDeleted'
              : 'admin.socialPublishing.toasts.updateSuccess'
          )
        );
      },
      error: (error: unknown) => {
        console.error('Error updating social publication', error);
        this.showActionError('admin.socialPublishing.toasts.updateError');
      }
    });
  }

  delete(publicationId: string): void {
    if (this.deletingPublicationIdSignal()) {
      return;
    }

    this.deletingPublicationIdSignal.set(publicationId);
    this.dataPort.delete(publicationId).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.deletingPublicationIdSignal.set(null))
    ).subscribe({
      next: (publication: SocialPublication) => {
        this.applyPublication(publication);
        this.toastMessageService.add(
          'success',
          this.translateService.instant('admin.socialPublishing.toasts.successSummary'),
          this.translateService.instant('admin.socialPublishing.toasts.deleteSuccess')
        );
      },
      error: (error: unknown) => {
        console.error('Error deleting social publication', error);
        this.showActionError('admin.socialPublishing.toasts.deleteError');
      }
    });
  }

  synchronize(): void {
    if (this.synchronizingSignal()) {
      return;
    }

    this.synchronizingSignal.set(true);
    this.dataPort.synchronize().pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.synchronizingSignal.set(false))
    ).subscribe({
      next: (result: SocialPublicationSynchronizationResult) => {
        this.toastMessageService.add(
          result.failureCount > 0 ? 'warn' : 'success',
          this.translateService.instant('admin.socialPublishing.toasts.syncSummary'),
          this.translateService.instant('admin.socialPublishing.toasts.syncSuccess', {
            checked: result.checkedCount,
            updated: result.updatedCount,
            deleted: result.deletedCount,
            failed: result.failureCount
          })
        );
        this.load();
      },
      error: (error: unknown) => {
        console.error('Error synchronizing social publications', error);
        this.showActionError('admin.socialPublishing.toasts.syncError');
      }
    });
  }

  private applyPublication(publication: SocialPublication): void {
    const currentOverview: SocialPublishingOverview = this.overview() ?? {
      publishers: [],
      recentPublications: []
    };
    const recentPublications: SocialPublication[] = [
      publication,
      ...currentOverview.recentPublications.filter((item: SocialPublication) => item.id !== publication.id)
    ].slice(0, 25);
    this.screenStateStore.setReady({
      publishers: currentOverview.publishers,
      recentPublications
    });
  }

  private showPublicationResult(publication: SocialPublication): void {
    if (publication.status === 'Published') {
      this.toastMessageService.add(
        'success',
        this.translateService.instant('admin.socialPublishing.toasts.successSummary'),
        this.translateService.instant('admin.socialPublishing.toasts.publishSuccess')
      );
      return;
    }

    this.toastMessageService.add(
      'error',
      this.translateService.instant('admin.socialPublishing.toasts.errorSummary'),
      publication.failureMessage ?? this.translateService.instant('admin.socialPublishing.toasts.publishError')
    );
  }

  private showActionError(messageKey: string): void {
    this.toastMessageService.add(
      'error',
      this.translateService.instant('admin.socialPublishing.toasts.errorSummary'),
      this.translateService.instant(messageKey)
    );
  }
}
