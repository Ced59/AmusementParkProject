import { Component, WritableSignal, signal } from '@angular/core';

import { ButtonDirective } from '../../button';

export @Component({
  standalone: true,
  imports: [ButtonDirective],
  template: `
    <button appUiButton type="button" icon="pi pi-save" [label]="label()" [loading]="loading()" (click)="clickCount += 1"></button>
  `
})
class GeneratedContentButtonHostComponent {
  loading: WritableSignal<boolean> = signal(false);
  label: WritableSignal<string> = signal('Save');
  clickCount: number = 0;
}
