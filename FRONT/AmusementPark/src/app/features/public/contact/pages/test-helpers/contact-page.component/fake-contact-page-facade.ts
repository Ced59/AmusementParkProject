import { signal, WritableSignal } from '@angular/core';

export class FakeContactPageFacade {
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
