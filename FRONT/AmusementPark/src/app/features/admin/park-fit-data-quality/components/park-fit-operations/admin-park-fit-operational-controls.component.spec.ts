import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { ParkFitDataQuality } from '@app/models/admin/park-fit/park-fit-data-quality.models';
import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies
} from '@app/testing/common-test-providers';
import { AdminParkFitDataQualityFacade } from '../../state/admin-park-fit-data-quality.facade';
import { AdminParkFitOperationalControlsComponent } from './admin-park-fit-operational-controls.component';

describe('AdminParkFitOperationalControlsComponent', () => {
  const facade = {
    isProcessing: vi.fn().mockReturnValue(false),
    changeOperationalStatus: vi.fn()
  };

  beforeEach(async () => {
    facade.isProcessing.mockClear();
    facade.changeOperationalStatus.mockClear();

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminParkFitOperationalControlsComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: AdminParkFitDataQualityFacade, useValue: facade }
      ]
    }).compileComponents();
  });

  it('closes a pending transition when the refreshed operational revision arrives', () => {
    const fixture: ComponentFixture<AdminParkFitOperationalControlsComponent> =
      TestBed.createComponent(AdminParkFitOperationalControlsComponent);
    fixture.componentRef.setInput('park', createPark('NotActivated', 0));
    fixture.detectChanges();

    fixture.debugElement.query(By.css('.park-fit-operational-controls__actions button'))
      .nativeElement.click();
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('form'))).not.toBeNull();

    fixture.componentRef.setInput('park', createPark('Active', 1));
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('form'))).toBeNull();
  });
});

function createPark(
  recommendationState: ParkFitDataQuality['recommendationState'],
  operationalRevision: number
): ParkFitDataQuality {
  return {
    parkId: 'park-1',
    parkName: 'Parc des essais',
    status: 'EligibleForFitComparison',
    coveragePercent: 100,
    visibleAttractionCount: 1,
    attractionWithConditionsCount: 1,
    decisionEligibleAttractionCount: 1,
    conditionCount: 1,
    decisionEligibleConditionCount: 1,
    issueItemCount: 0,
    missingSourceItemCount: 0,
    missingTimestampItemCount: 0,
    staleEvidenceItemCount: 0,
    ambiguousItemCount: 0,
    issues: [],
    issueSamples: [],
    recommendationState,
    operationalRevision,
    pendingReportCount: 0,
    recentDecisions: []
  };
}
