import { signal, WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { ContactPageFacade } from '../state/contact-page.facade';
import { ContactPageComponent } from './contact-page.component';

class FakeContactPageFacade {
  readonly submitting = signal(false).asReadonly();
  readonly submittedSignal: WritableSignal<boolean> = signal(false);
  readonly submitted = this.submittedSignal.asReadonly();
  readonly errorKey = signal<string | null>(null).asReadonly();
  readonly submissions: Array<{ message: string; website: string }> = [];
  resetCallCount = 0;

  submit(message: string, website: string): void {
    this.submissions.push({ message, website });
  }

  resetSubmissionState(): void {
    this.resetCallCount += 1;
  }
}

describe('ContactPageComponent', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('keeps the message available until submission succeeds', () => {
    const facade = new FakeContactPageFacade();
    const component = TestBed.runInInjectionContext(() => new ContactPageComponent(facade as unknown as ContactPageFacade));
    const formAccess = component as unknown as {
      contactForm: {
        controls: {
          message: { setValue(value: string): void; value: string };
          website: { setValue(value: string): void };
        };
      };
      submit(): void;
    };

    formAccess.contactForm.controls.message.setValue('Une suggestion assez claire.');
    formAccess.contactForm.controls.website.setValue('');

    formAccess.submit();

    expect(facade.submissions).toEqual([{ message: 'Une suggestion assez claire.', website: '' }]);
    expect(formAccess.contactForm.controls.message.value).toBe('Une suggestion assez claire.');

    facade.submittedSignal.set(true);
    TestBed.flushEffects();

    expect(formAccess.contactForm.controls.message.value).toBe('');
  });
});
