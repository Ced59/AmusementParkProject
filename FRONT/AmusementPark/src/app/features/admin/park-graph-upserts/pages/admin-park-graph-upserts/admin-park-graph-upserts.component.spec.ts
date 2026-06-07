import { ComponentFixture, TestBed } from '@angular/core/testing';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AdminParkGraphUpsertsComponent } from './admin-park-graph-upserts.component';

interface AdminParkGraphUpsertsComponentHarness {
  activeWorkspaceTab: string;
  createIfMissing: boolean;
  graphMode: string;
  jsonText: string;
  replaceCollections: boolean;
  uiError: string | null;
  continueFromMode(): void;
  continueFromTarget(): void;
  openExpertJson(): void;
  selectGraphMode(mode: 'merge' | 'replaceCollections'): void;
}

describe('AdminParkGraphUpsertsComponent', () => {
  let component: AdminParkGraphUpsertsComponent;
  let fixture: ComponentFixture<AdminParkGraphUpsertsComponent>;
  let harness: AdminParkGraphUpsertsComponentHarness;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminParkGraphUpsertsComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(AdminParkGraphUpsertsComponent);
    component = fixture.componentInstance;
    harness = component as unknown as AdminParkGraphUpsertsComponentHarness;
  });

  it('starts on the target wizard step and keeps the expert JSON tab available', () => {
    fixture.detectChanges();

    expect(harness.activeWorkspaceTab).toBe('target');

    const expertButton: HTMLButtonElement = fixture.nativeElement.querySelector('.graph-wizard__step--expert') as HTMLButtonElement;
    expertButton.click();
    fixture.detectChanges();

    expect(harness.activeWorkspaceTab).toBe('expert');
    expect(fixture.nativeElement.querySelector('.json-editor')).not.toBeNull();
  });

  it('requires an existing park or explicit creation before leaving the target step', () => {
    harness.continueFromTarget();

    expect(harness.activeWorkspaceTab).toBe('target');
    expect(harness.uiError).toBe('admin.parkGraphUpserts.errors.noParkSelected');

    harness.createIfMissing = true;
    harness.continueFromTarget();

    expect(harness.activeWorkspaceTab).toBe('mode');
  });

  it('syncs the selected upsert mode into the JSON document before the graph step', () => {
    harness.selectGraphMode('replaceCollections');
    harness.continueFromMode();

    const document: { mode?: string } = JSON.parse(harness.jsonText) as { mode?: string };

    expect(harness.activeWorkspaceTab).toBe('graph');
    expect(harness.graphMode).toBe('replaceCollections');
    expect(harness.replaceCollections).toBeTrue();
    expect(document.mode).toBe('replace');
  });
});
