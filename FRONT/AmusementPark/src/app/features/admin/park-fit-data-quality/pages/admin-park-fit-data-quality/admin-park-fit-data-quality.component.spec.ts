import type { MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';

import { ParkFitDataQualityPage } from '@app/models/admin/park-fit/park-fit-data-quality.models';
import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies
} from '@app/testing/common-test-providers';
import {
  ADMIN_PARK_FIT_DATA_QUALITY_STATE_PORT,
  AdminParkFitDataQualityStatePort
} from '../../state/admin-park-fit-data-quality-state-data.ports';
import { AdminParkFitDataQualityComponent } from './admin-park-fit-data-quality.component';

describe('AdminParkFitDataQualityComponent', () => {
  let port: MockedObject<AdminParkFitDataQualityStatePort>;

  beforeEach(async () => {
    port = {
      getPage: vi.fn().mockName('AdminParkFitDataQualityStatePort.getPage')
    } as unknown as MockedObject<AdminParkFitDataQualityStatePort>;
    port.getPage.mockReturnValue(of(createPage()));

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminParkFitDataQualityComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: ADMIN_PARK_FIT_DATA_QUALITY_STATE_PORT, useValue: port }
      ]
    }).compileComponents();
  });

  it('renders readiness cards and links administrators to the affected attraction', () => {
    const fixture: ComponentFixture<AdminParkFitDataQualityComponent> =
      TestBed.createComponent(AdminParkFitDataQualityComponent);
    fixture.detectChanges();

    const cards = fixture.debugElement.queryAll(
      By.css('.admin-park-fit-data-quality-park')
    );
    const attractionLink = fixture.debugElement.query(
      By.css('.admin-park-fit-data-quality-samples a')
    );

    expect(port.getPage).toHaveBeenCalledWith(1, 12);
    expect(cards).toHaveLength(1);
    expect(cards[0].nativeElement.textContent).toContain('Parc des essais');
    expect(attractionLink.nativeElement.textContent).toContain('Montagnes russes');
    expect(attractionLink.attributes['href']).toBe(
      '/fr/admin/parks/edit/park-1/items/item-1'
    );
  });
});

function createPage(): ParkFitDataQualityPage {
  return {
    items: [
      {
        parkId: 'park-1',
        parkName: 'Parc des essais',
        status: 'Insufficient',
        coveragePercent: 50,
        visibleAttractionCount: 2,
        attractionWithConditionsCount: 1,
        decisionEligibleAttractionCount: 1,
        conditionCount: 1,
        decisionEligibleConditionCount: 1,
        issueItemCount: 1,
        missingSourceItemCount: 1,
        missingTimestampItemCount: 0,
        staleEvidenceItemCount: 0,
        ambiguousItemCount: 0,
        issues: ['MissingAuthoritativeSource'],
        issueSamples: [
          {
            parkItemId: 'item-1',
            parkItemName: 'Montagnes russes',
            issues: ['MissingAuthoritativeSource']
          }
        ]
      }
    ],
    pagination: {
      totalItems: 1,
      totalPages: 1,
      currentPage: 1,
      itemsPerPage: 12
    }
  };
}
