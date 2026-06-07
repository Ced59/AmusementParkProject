import { ComponentFixture, TestBed } from '@angular/core/testing';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AdminParkGraphUpsertsComponent } from './admin-park-graph-upserts.component';
import { ParkGraphUpsertHistoryEntry } from '@app/models/admin/park-graph-upsert.models';

interface AdminParkGraphUpsertsComponentHarness {
  activeWorkspaceTab: string;
  builder: {
    itemName: string;
    parkName: string;
    countryCode: string;
    zoneName: string;
  };
  createIfMissing: boolean;
  graphMode: string;
  jsonText: string;
  replaceCollections: boolean;
  uiError: string | null;
  applyBuilderToJson(): void;
  applyTemplate(templateId: 'parkOnly' | 'zonesOnly' | 'zoneItems' | 'servicesNoZone' | 'references' | 'captainCoaster'): void;
  continueFromMode(): void;
  continueFromTarget(): void;
  openExpertJson(): void;
  reuseHistory(entry: ParkGraphUpsertHistoryEntry): void;
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

  it('builds safe hidden ToReview JSON from the graph builder', () => {
    harness.builder.parkName = 'Builder Park';
    harness.builder.countryCode = 'fr';
    harness.builder.zoneName = 'Main Street';
    harness.builder.itemName = 'Coaster';

    harness.applyBuilderToJson();

    const document = JSON.parse(harness.jsonText) as {
      park: { isVisible: boolean; adminReviewStatus: string };
      items: Array<{ isVisible: boolean; adminReviewStatus: string }>;
    };

    expect(document.park.isVisible).toBeFalse();
    expect(document.park.adminReviewStatus).toBe('ToReview');
    expect(document.items[0].isVisible).toBeFalse();
    expect(document.items[0].adminReviewStatus).toBe('ToReview');
  });

  it('loads templates into the JSON draft without leaving the graph workflow', () => {
    harness.builder.parkName = 'Template Park';

    harness.applyTemplate('parkOnly');

    const document = JSON.parse(harness.jsonText) as { park: { name: string }; items: unknown[]; zones: unknown[] };

    expect(harness.activeWorkspaceTab).toBe('graph');
    expect(document.park.name).toBe('Template Park');
    expect(document.items.length).toBe(0);
    expect(document.zones.length).toBe(0);
  });

  it('reuses history raw JSON as a graph draft and restores the target park', () => {
    const entry: ParkGraphUpsertHistoryEntry = {
      id: 'history-1',
      operationKind: 'preview',
      targetParkId: 'park-1',
      targetParkName: 'History Park',
      requestedByUserId: 'user-1',
      createdAtUtc: '2026-06-07T12:00:00Z',
      rawJson: '{"identity":{"name":"History Park","countryCode":"FR"},"park":{"name":"History Park"},"items":[]}',
      result: {
        operationId: 'op-1',
        mode: 'merge',
        isApplied: false,
        canApply: true,
        previewedAtUtc: '2026-06-07T12:00:00Z',
        counts: { created: 0, updated: 0, unchanged: 0, warnings: 0, errors: 0 },
        changes: [],
        warnings: [],
        errors: []
      }
    };

    harness.reuseHistory(entry);

    expect(harness.activeWorkspaceTab).toBe('graph');
    expect(harness.createIfMissing).toBeFalse();
    expect(harness.jsonText).toContain('History Park');
  });
});
