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
    fixture.componentRef.setInput('park', createPark('Suspended', 1));
    fixture.detectChanges();

    fixture.debugElement.query(By.css('.park-fit-operational-controls__actions button'))
      .nativeElement.click();
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('form'))).not.toBeNull();

    fixture.componentRef.setInput('park', createPark('Active', 2));
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('form'))).toBeNull();
  });

  it('closes a pending restoration when refreshed quality becomes ineligible', () => {
    const fixture: ComponentFixture<AdminParkFitOperationalControlsComponent> =
      TestBed.createComponent(AdminParkFitOperationalControlsComponent);
    fixture.componentRef.setInput('park', createPark('Suspended', 1));
    fixture.detectChanges();

    fixture.debugElement.query(By.css('.park-fit-operational-controls__actions button'))
      .nativeElement.click();
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('form'))).not.toBeNull();

    fixture.componentRef.setInput(
      'park',
      createPark('Suspended', 1, 'Insufficient')
    );
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('form'))).toBeNull();
  });

  it('exposes activation only when a non-activated park passes the quality gate', () => {
    const fixture: ComponentFixture<AdminParkFitOperationalControlsComponent> =
      TestBed.createComponent(AdminParkFitOperationalControlsComponent);
    fixture.componentRef.setInput('park', createPark('NotActivated', 0));
    fixture.detectChanges();

    const buttons = fixture.debugElement.queryAll(
      By.css('.park-fit-operational-controls__actions button')
    );
    expect(buttons).toHaveLength(1);
    expect(buttons[0].nativeElement.disabled).toBe(false);
  });

  it('submits an eligible activation with the current revision and a trimmed reason', () => {
    const fixture: ComponentFixture<AdminParkFitOperationalControlsComponent> =
      TestBed.createComponent(AdminParkFitOperationalControlsComponent);
    const park: ParkFitDataQuality = createPark('NotActivated', 4);
    fixture.componentRef.setInput('park', park);
    fixture.detectChanges();

    fixture.debugElement.query(
      By.css('.park-fit-operational-controls__actions button')
    ).nativeElement.click();
    fixture.detectChanges();

    const textarea = fixture.debugElement.query(By.css('textarea')).nativeElement;
    textarea.value = '  Audit qualité validé  ';
    textarea.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    fixture.debugElement.query(By.css('form')).triggerEventHandler('ngSubmit');

    expect(facade.changeOperationalStatus).toHaveBeenCalledWith(
      park,
      'Active',
      'Audit qualité validé'
    );
  });

  it('keeps activation disabled while a non-activated park is ineligible', () => {
    const fixture: ComponentFixture<AdminParkFitOperationalControlsComponent> =
      TestBed.createComponent(AdminParkFitOperationalControlsComponent);
    fixture.componentRef.setInput('park', createPark('NotActivated', 0, 'Insufficient'));
    fixture.detectChanges();

    const button = fixture.debugElement.query(
      By.css('.park-fit-operational-controls__actions button')
    );
    expect(button.nativeElement.disabled).toBe(true);
  });

  it('offers suspension and withdrawal for an active park', () => {
    const fixture: ComponentFixture<AdminParkFitOperationalControlsComponent> =
      TestBed.createComponent(AdminParkFitOperationalControlsComponent);
    fixture.componentRef.setInput('park', createPark('Active', 2));
    fixture.detectChanges();

    expect(
      fixture.debugElement.queryAll(By.css('.park-fit-operational-controls__actions button'))
    ).toHaveLength(2);
  });

  it('keeps withdrawal available when a suspended park cannot yet be restored', () => {
    const fixture: ComponentFixture<AdminParkFitOperationalControlsComponent> =
      TestBed.createComponent(AdminParkFitOperationalControlsComponent);
    fixture.componentRef.setInput('park', createPark('Suspended', 3, 'Insufficient'));
    fixture.detectChanges();

    const buttons = fixture.debugElement.queryAll(
      By.css('.park-fit-operational-controls__actions button')
    );
    expect(buttons).toHaveLength(2);
    expect(buttons[0].nativeElement.disabled).toBe(true);
    expect(buttons[1].nativeElement.disabled).toBe(false);
  });
});

function createPark(
  recommendationState: ParkFitDataQuality['recommendationState'],
  operationalRevision: number,
  status: ParkFitDataQuality['status'] = 'EligibleForFitComparison'
): ParkFitDataQuality {
  return {
    parkId: 'park-1',
    parkName: 'Parc des essais',
    status,
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
