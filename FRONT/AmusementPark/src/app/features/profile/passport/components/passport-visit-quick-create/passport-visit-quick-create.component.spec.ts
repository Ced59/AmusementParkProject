import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { Router } from '@angular/router';

import { PassportVisit } from '@app/models/passport/passport-visit.models';
import { PassportVisitQuickCreateStateFacade } from '../../state/passport-visit-quick-create-state.facade';
import { PassportVisitQuickCreateComponent } from './passport-visit-quick-create.component';

@Component({
  template: `
    <main class="app-layout-main">
      <app-passport-visit-quick-create
        [visible]="visible"
        (visitCreated)="createdVisit = $event">
      </app-passport-visit-quick-create>
    </main>
  `,
  imports: [PassportVisitQuickCreateComponent]
})
class PassportVisitQuickCreateHostComponent {
  visible: boolean = true;
  createdVisit: PassportVisit | null = null;
}

const fakeFacade = {
  parkOptions: signal([]),
  searching: signal(false),
  searchErrorKey: signal<string | null>(null),
  saving: signal(false),
  errorKey: signal<string | null>(null),
  createdVisit: signal<PassportVisit | null>(null),
  createdLocalDraftId: signal<string | null>(null),
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

  it('raises and restores the main stacking layer above fixed public toolbars while the modal is open', async () => {
    await TestBed.configureTestingModule({
      imports: [TranslateModule.forRoot(), PassportVisitQuickCreateHostComponent],
      providers: [{ provide: Router, useValue: { url: '/fr/profile', navigate: vi.fn() } }]
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

  it('opens the localized visit editor from the successful creation step', async () => {
    const router = { url: '/fr/profile', navigate: vi.fn().mockResolvedValue(true) };
    fakeFacade.createdVisit.set({
      id: 'visit-1',
      parkId: 'park-1',
      date: { year: 2026, month: 9, day: 3, precision: 'Day', isApproximate: false },
      timeZoneId: 'Europe/Paris',
      serviceDayConvention: 'VisitStartLocalDate',
      status: 'Draft',
      privacy: 'Private',
      title: null,
      privateNote: null,
      version: 1,
      createdAtUtc: '2026-09-03T00:00:00Z',
      updatedAtUtc: '2026-09-03T00:00:00Z',
      completedAtUtc: null
    });
    await TestBed.configureTestingModule({
      imports: [TranslateModule.forRoot(), PassportVisitQuickCreateHostComponent],
      providers: [{ provide: Router, useValue: router }]
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
    const dialog: PassportVisitQuickCreateComponent = fixture.debugElement.children[0].children[0]
      .componentInstance as PassportVisitQuickCreateComponent;

    (dialog as unknown as { manageCreatedVisit(): void }).manageCreatedVisit();

    expect(router.navigate).toHaveBeenCalledWith(['/', 'fr', 'profile', 'visits', 'visit-1']);
    fakeFacade.createdVisit.set(null);
  });

  it('opens the local draft editor when an anonymous visit was saved on the device', async () => {
    const router = { url: '/fr/parks/parc-test', navigate: vi.fn().mockResolvedValue(true) };
    fakeFacade.createdLocalDraftId.set('draft-1');
    await TestBed.configureTestingModule({
      imports: [TranslateModule.forRoot(), PassportVisitQuickCreateHostComponent],
      providers: [{ provide: Router, useValue: router }]
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
    const dialog: PassportVisitQuickCreateComponent = fixture.debugElement.children[0].children[0]
      .componentInstance as PassportVisitQuickCreateComponent;

    (dialog as unknown as { manageCreatedVisit(): void }).manageCreatedVisit();

    expect(router.navigate).toHaveBeenCalledWith(['/', 'fr', 'passport', 'local', 'draft-1']);
    fakeFacade.createdLocalDraftId.set(null);
  });

  it('notifies its host when a new visit has been created', async () => {
    fakeFacade.createdVisit.set(null);
    fakeFacade.createdLocalDraftId.set(null);
    await TestBed.configureTestingModule({
      imports: [TranslateModule.forRoot(), PassportVisitQuickCreateHostComponent],
      providers: [{ provide: Router, useValue: { url: '/fr/profile', navigate: vi.fn() } }]
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
    const createdVisit: PassportVisit = {
      id: 'visit-2',
      parkId: 'park-1',
      date: { year: 2026, month: 9, day: 4, precision: 'Day', isApproximate: false },
      timeZoneId: null,
      serviceDayConvention: 'VisitStartLocalDate',
      status: 'Draft',
      privacy: 'Private',
      title: null,
      privateNote: null,
      version: 1,
      createdAtUtc: '2026-09-04T00:00:00Z',
      updatedAtUtc: '2026-09-04T00:00:00Z',
      completedAtUtc: null
    };

    fakeFacade.createdVisit.set(createdVisit);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.componentInstance.createdVisit).toBe(createdVisit);
    fakeFacade.createdVisit.set(null);
  });
});
