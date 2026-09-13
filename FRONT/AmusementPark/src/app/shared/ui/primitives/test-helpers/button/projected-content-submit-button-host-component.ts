import { Component, WritableSignal, signal } from '@angular/core';

import { FormsModule } from '@angular/forms';

import { ButtonDirective } from '../../button';

export @Component({
  standalone: true,
  imports: [ButtonDirective, FormsModule],
  template: `
    <form (ngSubmit)="submitCount += 1">
      <button appUiButton type="submit" [loading]="loading()">{{ label() }}</button>
    </form>
  `
})
class ProjectedContentSubmitButtonHostComponent {
  loading: WritableSignal<boolean> = signal(false);
  label: WritableSignal<string> = signal('Save');
  submitCount: number = 0;
}
