import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';

import { PassportVisit } from '@app/models/passport/passport-visit.models';
import { PassportVisitQuickCreateStateFacade } from '../../state/passport-visit-quick-create-state.facade';
import { PassportVisitQuickCreateComponent } from './passport-visit-quick-create.component';

@Component({
  template: `
    <main class="app-layout-main">
      <app-passport-visit-quick-create [visible]="visible"></app-passport-visit-quick-create>
    </main>
  `,
  imports: [PassportVisitQuickCreateComponent]
})
class PassportVisitQuickCreateHostComponent {
  visible: boolean = true;
}

const fakeFacade = {
  parkOptions: signal([]),
  searching: signal(false),
  searchErrorKey: signal<string | null>(null),
  saving: signal(false),
  errorKey: signal<string | null>(null),
  createdVisit: signal<PassportVisit | null>(null),
  searchParks: vi.fn(),
  createVisit: vi.fn(),
  clearCreationResult: vi.fn(),
  clearParkSearch: vi.fn()
};

describe('PassportVisitQuickCreateComponent responsive contract', () => {
  it('bounds the dialog to the dynamic viewport and the safe areas', () => {
    const styles: string = (
      PassportVisitQuickCreateComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-height: min(52rem, 100dvh - 2rem)');
    expect(styles).toContain('max-width: 100vw !important');
    expect(styles).toContain('env(safe-area-inset-bottom)');
  });

  it('reflows date fields and actions to one column on narrow mobile viewports', () => {
    const styles: string = (
      PassportVisitQuickCreateComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('@media (max-width: 520px)');
    expect(styles).toContain('.passport-date-fields');
    expect(styles).toContain('grid-template-columns: 1fr');
    expect(styles).toContain('@media (max-width: 360px)');
  });

  it('raises and restores the main stacking layer while the modal is open', async () => {
    await TestBed.configureTestingModule({
      imports: [TranslateModule.forRoot(), PassportVisitQuickCreateHostComponent]
    })
      .overrideComponent(PassportVisitQuickCreateComponent, {
        set: {
          providers: [{ provide: PassportVisitQuickCreateStateFacade, useValue: fakeFacade }]
        }
      })
      .compileComponents();
    const fixture: ComponentFixture<PassportVisitQuickCreateHostComponent> = TestBed.createComponent(
      PassportVisitQuickCreateHostComponent
    );

    fixture.detectChanges();
    const main: HTMLElement = fixture.nativeElement.querySelector('.app-layout-main') as HTMLElement;
    expect(main.classList.contains('app-layout-main--modal-open')).toBe(true);

    const dialog: PassportVisitQuickCreateComponent = fixture.debugElement.children[0].children[0]
      .componentInstance as PassportVisitQuickCreateComponent;
    (dialog as unknown as { onDialogVisibleChange(visible: boolean): void }).onDialogVisibleChange(false);
    expect(main.classList.contains('app-layout-main--modal-open')).toBe(false);
  });
});
