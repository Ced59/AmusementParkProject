import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { ToastMessageService } from '@app/services/messages/toast-message.service';
import {
  PublishSocialLinkRequest,
  SocialPublication,
  SocialPublisher,
  SocialPublishingOverview
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
  private readonly retryingPublicationIdSignal = signal<string | null>(null);

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
  public readonly retryingPublicationId = this.retryingPublicationIdSignal.asReadonly();

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
}
