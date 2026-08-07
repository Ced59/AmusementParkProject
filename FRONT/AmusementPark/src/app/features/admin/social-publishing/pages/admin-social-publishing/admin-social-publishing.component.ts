import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { debounceTime, distinctUntilChanged, map } from 'rxjs';

import {
  PublishSocialLinkRequest,
  SocialPublication,
  SocialPublicationDraft,
  SocialPublicationStatus
} from '@app/models/social-publishing/social-publishing.models';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { InputText } from '@shared/ui/primitives/inputtext';
import { Tag } from '@shared/ui/primitives/tag';
import { AdminSocialPublishingFacade } from '../../state/admin-social-publishing.facade';

interface SocialPublicationForm {
  readonly message: FormControl<string>;
  readonly url: FormControl<string>;
}

@Component({
  selector: 'app-admin-social-publishing',
  templateUrl: './admin-social-publishing.component.html',
  styleUrl: './admin-social-publishing.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminSocialPublishingFacade],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    PageStateComponent,
    ImageDisplayComponent,
    ButtonDirective,
    InputText,
    Tag
  ]
})
export class AdminSocialPublishingComponent implements OnInit {
  protected readonly state = this.facade.state;
  protected readonly facebookPublisher = this.facade.facebookPublisher;
  protected readonly recentPublications = this.facade.recentPublications;
  protected readonly publishing = this.facade.publishing;
  protected readonly draft = this.facade.draft;
  protected readonly draftLoading = this.facade.draftLoading;
  protected readonly draftError = this.facade.draftError;
  protected readonly selectedImageId = this.facade.selectedImageId;
  protected readonly retryingPublicationId = this.facade.retryingPublicationId;
  protected readonly updatingPublicationId = this.facade.updatingPublicationId;
  protected readonly deletingPublicationId = this.facade.deletingPublicationId;
  protected readonly synchronizing = this.facade.synchronizing;
  protected readonly editingPublicationId = signal<string | null>(null);
  protected readonly editMessageControl = new FormControl<string>('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(5000)]
  });

  protected readonly publicationForm = new FormGroup<SocialPublicationForm>({
    message: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(5000)]
    }),
    url: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.required]
    })
  });

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private lastAppliedDraftUrl: string | null = null;

  constructor(
    private readonly facade: AdminSocialPublishingFacade,
    private readonly translateService: TranslateService
  ) {
    effect((): void => {
      const currentDraft: SocialPublicationDraft | null = this.draft();
      if (currentDraft === null) {
        this.lastAppliedDraftUrl = null;
        return;
      }

      if (this.lastAppliedDraftUrl === currentDraft.url) {
        return;
      }

      this.lastAppliedDraftUrl = currentDraft.url;
      this.publicationForm.patchValue({
        url: currentDraft.url,
        message: currentDraft.defaultMessage
      }, { emitEvent: false });
    });
  }

  ngOnInit(): void {
    this.facade.load();
    this.publicationForm.controls.url.valueChanges.pipe(
      map((value: string): string => value.trim()),
      debounceTime(350),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((url: string): void => this.facade.resolveDraft(url));
  }

  protected publish(): void {
    const value = this.publicationForm.getRawValue();
    const normalizedUrl: string = value.url.trim();
    const currentDraft: SocialPublicationDraft | null = this.draft();
    if (this.publicationForm.invalid
      || currentDraft === null
      || currentDraft.url !== normalizedUrl
      || this.draftLoading()
      || this.publishing()
      || !this.facebookPublisher()?.isConfigured) {
      this.publicationForm.markAllAsTouched();
      return;
    }

    const request: PublishSocialLinkRequest = {
      network: 'Facebook',
      message: value.message.trim(),
      url: normalizedUrl,
      previewImageId: this.selectedImageId()
    };
    this.facade.publish(request);
  }

  protected selectPreviewImage(imageId: string | null): void {
    this.facade.selectPreviewImage(imageId);
  }

  protected previousImagePage(): void {
    const currentDraft: SocialPublicationDraft | null = this.draft();
    const currentPage: number = currentDraft?.images.pagination.currentPage ?? 1;
    if (currentDraft !== null && currentPage > 1) {
      this.facade.resolveDraft(currentDraft.url, currentPage - 1);
    }
  }

  protected nextImagePage(): void {
    const currentDraft: SocialPublicationDraft | null = this.draft();
    const currentPage: number = currentDraft?.images.pagination.currentPage ?? 1;
    const totalPages: number = currentDraft?.images.pagination.totalPages ?? 0;
    if (currentDraft !== null && currentPage < totalPages) {
      this.facade.resolveDraft(currentDraft.url, currentPage + 1);
    }
  }

  protected retry(publication: SocialPublication): void {
    if (publication.status !== 'Failed') {
      return;
    }

    this.facade.retry(publication.id);
  }

  protected startEdit(publication: SocialPublication): void {
    if (publication.status !== 'Published') {
      return;
    }

    this.editMessageControl.setValue(publication.message);
    this.editingPublicationId.set(publication.id);
  }

  protected cancelEdit(): void {
    this.editingPublicationId.set(null);
    this.editMessageControl.reset('');
  }

  protected saveEdit(publication: SocialPublication): void {
    if (this.editMessageControl.invalid || this.updatingPublicationId()) {
      this.editMessageControl.markAsTouched();
      return;
    }

    this.facade.update(publication.id, this.editMessageControl.value.trim(), () => this.cancelEdit());
  }

  protected delete(publication: SocialPublication): void {
    if (publication.status !== 'Published'
      || !confirm(this.deleteConfirmationMessage())) {
      return;
    }

    this.facade.delete(publication.id);
    if (this.editingPublicationId() === publication.id) {
      this.cancelEdit();
    }
  }

  protected synchronize(): void {
    this.facade.synchronize();
  }

  protected statusSeverity(status: SocialPublicationStatus): string {
    switch (status) {
      case 'Published':
        return 'success';
      case 'Failed':
        return 'danger';
      case 'Deleted':
        return 'secondary';
      default:
        return 'info';
    }
  }

  protected statusLabel(status: SocialPublicationStatus): string {
    return `admin.socialPublishing.statuses.${status}`;
  }

  protected triggerLabel(trigger: SocialPublication['trigger']): string {
    return `admin.socialPublishing.triggers.${trigger}`;
  }

  protected trackByPublicationId(_: number, publication: SocialPublication): string {
    return publication.id;
  }

  private deleteConfirmationMessage(): string {
    return this.translateService.instant('admin.socialPublishing.history.deleteConfirm');
  }
}
