import { Component, WritableSignal, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ButtonDirective } from './button';

@Component({
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

@Component({
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

describe('ButtonDirective', () => {
  it('keeps projected Angular content stable when loading changes on a submit button', async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectedContentSubmitButtonHostComponent]
    }).compileComponents();

    const fixture: ComponentFixture<ProjectedContentSubmitButtonHostComponent> = TestBed.createComponent(ProjectedContentSubmitButtonHostComponent);
    fixture.detectChanges();

    const button: HTMLButtonElement = findButton(fixture);
    button.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.submitCount).toBe(1);

    fixture.componentInstance.loading.set(true);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(button.textContent).toContain('Save');
    expect(button.querySelector('.pi-spinner')).not.toBeNull();

    fixture.componentInstance.label.set('Saved');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(button.textContent).toContain('Saved');

    fixture.componentInstance.loading.set(false);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(button.querySelector('.pi-spinner')).toBeNull();
    expect(button.textContent).toContain('Saved');

    button.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.submitCount).toBe(2);
  });

  it('replaces only generated icon and label nodes when generated button content changes', async () => {
    await TestBed.configureTestingModule({
      imports: [GeneratedContentButtonHostComponent]
    }).compileComponents();

    const fixture: ComponentFixture<GeneratedContentButtonHostComponent> = TestBed.createComponent(GeneratedContentButtonHostComponent);
    fixture.detectChanges();

    const button: HTMLButtonElement = findButton(fixture);

    expect(button.textContent).toContain('Save');
    expect(button.querySelectorAll('.p-button-label').length).toBe(1);
    expect(button.querySelector('.pi-save')).not.toBeNull();

    fixture.componentInstance.label.set('Saving');
    fixture.componentInstance.loading.set(true);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(button.textContent).toContain('Saving');
    expect(button.querySelectorAll('.p-button-label').length).toBe(1);
    expect(button.querySelector('.pi-spinner')).not.toBeNull();

    button.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.clickCount).toBe(1);
  });
});

function findButton<TComponent>(fixture: ComponentFixture<TComponent>): HTMLButtonElement {
  const button: HTMLButtonElement | null = fixture.nativeElement.querySelector('button') as HTMLButtonElement | null;
  if (!button) {
    throw new Error('Expected button to be rendered.');
  }

  return button;
}
