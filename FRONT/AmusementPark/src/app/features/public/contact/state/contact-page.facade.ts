import { DestroyRef, Inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TranslationService } from '@app/services/translation.service';
import { SubmitContactGrievanceRequest } from '@app/models/contact/contact-grievance.models';
import { CONTACT_PAGE_DATA_PORT, ContactPageDataPort } from './contact-page-data.ports';

@Injectable()
export class ContactPageFacade {
  private readonly submittingSignal = signal(false);
  private readonly submittedSignal = signal(false);
  private readonly errorKeySignal = signal<string | null>(null);

  public readonly submitting = this.submittingSignal.asReadonly();
  public readonly submitted = this.submittedSignal.asReadonly();
  public readonly errorKey = this.errorKeySignal.asReadonly();

  constructor(
    @Inject(CONTACT_PAGE_DATA_PORT) private readonly contactApiService: ContactPageDataPort,
    private readonly translationService: TranslationService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  submit(message: string, honeypot: string): void {
    if (this.submittingSignal()) {
      return;
    }

    const request: SubmitContactGrievanceRequest = {
      message: message.trim(),
      website: honeypot.trim() || null,
      languageCode: this.translationService.getCurrentLang() || 'en'
    };

    this.submittingSignal.set(true);
    this.submittedSignal.set(false);
    this.errorKeySignal.set(null);

    this.contactApiService.submitGrievance(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.submittingSignal.set(false);
        this.submittedSignal.set(true);
      },
      error: () => {
        this.submittingSignal.set(false);
        this.errorKeySignal.set('contactPage.form.error');
      }
    });
  }

  resetSubmissionState(): void {
    this.submittedSignal.set(false);
    this.errorKeySignal.set(null);
  }
}
