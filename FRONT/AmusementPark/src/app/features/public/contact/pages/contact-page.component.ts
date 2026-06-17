import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { ContactPageFacade } from '@features/public/contact/state/contact-page.facade';

interface ContactForm {
  readonly message: FormControl<string>;
  readonly website: FormControl<string>;
}

@Component({
  selector: 'app-contact-page',
  templateUrl: './contact-page.component.html',
  styleUrl: './contact-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ContactPageFacade],
  imports: [
    ReactiveFormsModule,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class ContactPageComponent {
  protected readonly maxMessageLength: number = 2000;
  protected readonly contactEmail: string = 'c.caudron59@gmail.com';
  protected readonly submitting = this.contactPageFacade.submitting;
  protected readonly submitted = this.contactPageFacade.submitted;
  protected readonly errorKey = this.contactPageFacade.errorKey;
  protected readonly contactForm = new FormGroup<ContactForm>({
    message: new FormControl<string>('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(10),
        Validators.maxLength(this.maxMessageLength)
      ]
    }),
    website: new FormControl<string>('', { nonNullable: true })
  });

  constructor(private readonly contactPageFacade: ContactPageFacade) {
  }

  protected submit(): void {
    if (this.contactForm.invalid) {
      this.contactForm.markAllAsTouched();
      return;
    }

    const value = this.contactForm.getRawValue();
    this.contactPageFacade.submit(value.message, value.website);
    this.contactForm.controls.message.reset('');
  }

  protected get remainingCharacters(): number {
    return this.maxMessageLength - this.contactForm.controls.message.value.length;
  }

  protected clearStatus(): void {
    this.contactPageFacade.resetSubmissionState();
  }
}
