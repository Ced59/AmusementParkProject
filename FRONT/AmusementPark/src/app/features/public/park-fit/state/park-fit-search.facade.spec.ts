import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';

import { ParkFitSearchRequest, ParkFitSearchResponse } from '@app/models/park-fit/park-fit-search.models';
import { ParkFitSearchDataPort, PARK_FIT_SEARCH_DATA_PORT } from './park-fit-search-data.ports';
import { ParkFitSearchFacade } from './park-fit-search.facade';

describe('ParkFitSearchFacade', () => {
  it('keeps the latest anonymous search in memory and exposes the first result', () => {
    const response: ParkFitSearchResponse = buildResponse();
    const search = vi.fn().mockReturnValue(of(response));
    const facade: ParkFitSearchFacade = createFacade({ search });
    const request: ParkFitSearchRequest = buildRequest();
    request.originLatitude = 50.6292;
    request.originLongitude = 3.0573;

    facade.search(request);
    request.members[0]!.heightCentimeters = 190;

    expect(search).toHaveBeenCalledOnce();
    expect(facade.status()).toBe('success');
    expect(facade.firstPark()?.parkName).toBe('Parc Démo');
    expect(facade.visibleParks()).toHaveLength(1);
    expect(facade.lastRequest()?.members[0]?.heightCentimeters).toBe(120);
    expect(facade.lastRequest()?.originLatitude).toBeNull();
    expect(facade.lastRequest()?.originLongitude).toBeNull();
    expect(search.mock.calls[0]?.[0].originLatitude).toBe(50.6292);
  });

  it('prevents a duplicate calculation while a search is running', () => {
    const pending = new Subject<ParkFitSearchResponse>();
    const search = vi.fn().mockReturnValue(pending.asObservable());
    const facade: ParkFitSearchFacade = createFacade({ search });

    facade.search(buildRequest());
    facade.search(buildRequest());

    expect(search).toHaveBeenCalledOnce();
    expect(facade.status()).toBe('loading');
  });

  it('does not present an explicitly excluded park as a match', () => {
    const response: ParkFitSearchResponse = buildResponse();
    response.parks[0]!.scoreState = 'Excluded';
    const facade: ParkFitSearchFacade = createFacade({ search: () => of(response) });

    facade.search(buildRequest());

    expect(facade.status()).toBe('success');
    expect(facade.firstPark()).toBeNull();
    expect(facade.visibleParks()).toEqual([]);
  });

  it('turns public throttling into a dedicated visitor message', () => {
    const search = vi.fn().mockReturnValue(throwError(() => new HttpErrorResponse({ status: 429 })));
    const facade: ParkFitSearchFacade = createFacade({ search });

    facade.search(buildRequest());

    expect(facade.status()).toBe('error');
    expect(facade.errorKey()).toBe('parkFit.feedback.rateLimited');
  });

  it('keeps an ordered comparison selection between two and four visible parks', () => {
    const response: ParkFitSearchResponse = buildResponse();
    response.parks = [1, 2, 3, 4, 5].map((index: number): ParkFitSearchResponse['parks'][number] => ({
      ...response.parks[0]!,
      parkId: `park-${index}`,
      parkName: `Parc ${index}`
    }));
    const facade: ParkFitSearchFacade = createFacade({ search: () => of(response) });

    facade.search(buildRequest());
    facade.toggleComparisonPark('park-2');
    facade.toggleComparisonPark('park-5');

    expect(facade.canCompare()).toBe(true);
    expect(facade.comparisonParks().map((park) => park.parkId)).toEqual(['park-2', 'park-5']);
    expect(facade.comparisonSelections().map((selection) => selection.resultRank)).toEqual([2, 5]);

    facade.toggleComparisonPark('park-1');
    facade.toggleComparisonPark('park-3');
    facade.toggleComparisonPark('park-4');

    expect(facade.comparisonLimitReached()).toBe(true);
    expect(facade.comparisonParks()).toHaveLength(4);

    facade.toggleComparisonPark('park-2');
    expect(facade.comparisonParks().map((park) => park.parkId)).toEqual(['park-1', 'park-3', 'park-5']);
  });

  it('clears the comparison selection when a new private search starts', () => {
    const facade: ParkFitSearchFacade = createFacade({ search: () => of(buildResponse()) });

    facade.search(buildRequest());
    facade.toggleComparisonPark('park-1');
    facade.search(buildRequest());

    expect(facade.comparisonParks()).toEqual([]);
  });
});

function createFacade(port: ParkFitSearchDataPort): ParkFitSearchFacade {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      ParkFitSearchFacade,
      { provide: PARK_FIT_SEARCH_DATA_PORT, useValue: port }
    ]
  });
  return TestBed.inject(ParkFitSearchFacade);
}

function buildRequest(): ParkFitSearchRequest {
  return {
    evaluationDate: '2026-10-10',
    members: [{
      heightCentimeters: 120,
      minimumAgeYears: 8,
      maximumAgeYears: 8,
      canBeAccompanied: false,
      companionMinimumAgeYears: null,
      companionMaximumAgeYears: null
    }],
    preferredAttractionTypes: ['FamilyRide'],
    preferIndoor: false,
    countryCode: null,
    originLatitude: null,
    originLongitude: null,
    unknownDataPolicy: 'KeepWithWarning',
    maximumResults: 10
  };
}

function buildResponse(): ParkFitSearchResponse {
  return {
    methodVersion: 'park-fit-2026-01',
    evaluationDate: '2026-10-10',
    evaluatedAtUtc: '2026-09-14T10:00:00Z',
    totalCandidateCount: 1,
    inspectedCandidateCount: 1,
    qualityEligibleCandidateCount: 1,
    qualityRejectedCandidateCount: 0,
    candidatePoolTruncated: false,
    qualityStatusCounts: { Eligible: 1 },
    qualityIssueCounts: {},
    parks: [{
      parkId: 'park-1',
      parkName: 'Parc Démo',
      countryCode: 'FR',
      parkType: 'ThemePark',
      scoreState: 'Available',
      comparativeScore: 82,
      rawKnownScore: 82,
      coveragePercent: 100,
      knownWeightPercent: 100,
      scoreCeilingPercent: null,
      confidence: 'High',
      dateAvailabilityState: 'Available',
      calendarState: 'OpenConfirmed',
      openingTimeRanges: [],
      calendarTimeZoneId: 'Europe/Paris',
      calendarSourceUrl: null,
      calendarLastVerifiedAtUtc: null,
      distanceKilometers: null,
      distanceMethod: null,
      distanceEvaluatedAtUtc: null,
      unknownCount: 0,
      everyoneTogetherAttractionCount: 12,
      splitRequiredAttractionCount: 2,
      partialAttractionCount: 0,
      noCompatibleMemberAttractionCount: 1,
      unknownAttractionCount: 0,
      dataQualityStatus: 'Eligible',
      dataQualityCoveragePercent: 100,
      lastVerifiedAtUtc: '2026-09-01T00:00:00Z',
      reasons: [],
      components: [],
      memberSummaries: [{
        memberNumber: 1,
        compatibleAloneAttractionCount: 12,
        compatibleWithCompanionAttractionCount: 0,
        incompatibleAttractionCount: 1,
        unknownAttractionCount: 0,
        notApplicableAttractionCount: 0
      }],
      criticalSources: []
    }]
  };
}
