import type { MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { ParkFitDataQualityPage } from '@app/models/admin/park-fit/park-fit-data-quality.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import {
  ADMIN_PARK_FIT_DATA_QUALITY_STATE_PORT,
  AdminParkFitDataQualityStatePort
} from './admin-park-fit-data-quality-state-data.ports';
import { AdminParkFitDataQualityFacade } from './admin-park-fit-data-quality.facade';

describe('AdminParkFitDataQualityFacade', () => {
  let facade: AdminParkFitDataQualityFacade;
  let port: MockedObject<AdminParkFitDataQualityStatePort>;

  const page: ParkFitDataQualityPage = {
    items: [
      {
        parkId: 'eligible',
        parkName: 'Parc prêt',
        status: 'EligibleForFitComparison',
        coveragePercent: 100,
        visibleAttractionCount: 2,
        attractionWithConditionsCount: 2,
        decisionEligibleAttractionCount: 2,
        conditionCount: 2,
        decisionEligibleConditionCount: 2,
        issueItemCount: 0,
        missingSourceItemCount: 0,
        missingTimestampItemCount: 0,
        staleEvidenceItemCount: 0,
        ambiguousItemCount: 0,
        issues: [],
        issueSamples: []
      },
      {
        parkId: 'work',
        parkName: 'Parc à compléter',
        status: 'Insufficient',
        coveragePercent: 50,
        visibleAttractionCount: 2,
        attractionWithConditionsCount: 1,
        decisionEligibleAttractionCount: 1,
        conditionCount: 1,
        decisionEligibleConditionCount: 1,
        issueItemCount: 1,
        missingSourceItemCount: 0,
        missingTimestampItemCount: 0,
        staleEvidenceItemCount: 0,
        ambiguousItemCount: 0,
        issues: ['MissingAccessConditions'],
        issueSamples: []
      }
    ],
    pagination: { totalItems: 14, totalPages: 2, currentPage: 1, itemsPerPage: 12 }
  };

  beforeEach(() => {
    port = {
      getPage: vi.fn().mockName('AdminParkFitDataQualityStatePort.getPage')
    } as unknown as MockedObject<AdminParkFitDataQualityStatePort>;
    TestBed.configureTestingModule({
      providers: [
        provideCommonTestDependencies(),
        AdminParkFitDataQualityFacade,
        { provide: ADMIN_PARK_FIT_DATA_QUALITY_STATE_PORT, useValue: port }
      ]
    });
    facade = TestBed.inject(AdminParkFitDataQualityFacade);
  });

  it('summarizes the business readiness of the current page', () => {
    port.getPage.mockReturnValue(of(page));

    facade.load();

    expect(port.getPage).toHaveBeenCalledWith(1, 12);
    expect(facade.eligibleCount()).toBe(1);
    expect(facade.actionRequiredCount()).toBe(1);
    expect(facade.averageCoveragePercent()).toBe(75);
  });

  it('moves through server pages without exceeding their bounds', () => {
    port.getPage.mockReturnValue(of(page));
    facade.load();
    const secondPage: ParkFitDataQualityPage = {
      items: [],
      pagination: { totalItems: 14, totalPages: 2, currentPage: 2, itemsPerPage: 12 }
    };
    port.getPage.mockReturnValue(of(secondPage));

    facade.nextPage();
    facade.nextPage();

    expect(port.getPage).toHaveBeenCalledTimes(2);
    expect(port.getPage).toHaveBeenLastCalledWith(2, 12);
  });

  it('counts a park-level correction even when no attraction sample is affected', () => {
    const parkLevelIssuePage: ParkFitDataQualityPage = {
      items: [
        {
          ...page.items[0],
          parkId: 'missing-coordinates',
          status: 'Insufficient',
          issueItemCount: 0,
          issues: ['MissingCoordinates']
        }
      ],
      pagination: { totalItems: 1, totalPages: 1, currentPage: 1, itemsPerPage: 12 }
    };
    port.getPage.mockReturnValue(of(parkLevelIssuePage));

    facade.load();

    expect(facade.actionRequiredCount()).toBe(1);
  });

  it('keeps the previous page visible when refresh fails', () => {
    port.getPage.mockReturnValue(of(page));
    facade.load();
    port.getPage.mockReturnValue(
      throwError(() => new Error('network')) as Observable<ParkFitDataQualityPage>
    );

    facade.load();

    expect(facade.state().kind).toBe('error');
    expect(facade.assessments()).toHaveLength(2);
  });
});
