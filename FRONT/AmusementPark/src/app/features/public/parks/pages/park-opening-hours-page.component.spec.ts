import type { MockedObject } from 'vitest';
import { EventEmitter } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, ParamMap, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subject, of } from 'rxjs';

import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkStatus } from '@app/models/parks/park-status';
import {
  ParkOpeningHoursCalendar,
  ParkOpeningHoursDay,
  ParkOpeningHoursTimeRange,
} from '@app/models/parks/park-opening-hours';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { ParkOpeningHoursPageComponent } from './park-opening-hours-page.component';

class FakeTranslationService {
  public readonly languageChanged: EventEmitter<string> =
    new EventEmitter<string>();

  getCurrentLang(): string {
    return 'fr';
  }
}

describe('ParkOpeningHoursPageComponent', () => {
  let fixture: ComponentFixture<ParkOpeningHoursPageComponent>;
  let component: ParkOpeningHoursPageComponent;
  let paramMapSubject: Subject<ParamMap>;
  let parksApiService: MockedObject<ParksApiService>;
  let seoService: MockedObject<SeoService>;
  let ssrHttpStatusService: MockedObject<SsrHttpStatusService>;

  beforeEach(async () => {
    paramMapSubject = new Subject<ParamMap>();
    parksApiService = {
      getParkDetailSummary: vi
        .fn()
        .mockName('ParksApiService.getParkDetailSummary'),
      getParkOpeningHours: vi
        .fn()
        .mockName('ParksApiService.getParkOpeningHours'),
    } as unknown as MockedObject<ParksApiService>;
    seoService = {
      applyParkOpeningHoursSeo: vi
        .fn()
        .mockName('SeoService.applyParkOpeningHoursSeo'),
      applyParkUnavailableFeatureSeo: vi
        .fn()
        .mockName('SeoService.applyParkUnavailableFeatureSeo'),
    } as unknown as MockedObject<SeoService>;
    ssrHttpStatusService = {
      setNotFound: vi.fn().mockName('SsrHttpStatusService.setNotFound'),
      setStatus: vi.fn().mockName('SsrHttpStatusService.setStatus'),
    } as unknown as MockedObject<SsrHttpStatusService>;

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, ParkOpeningHoursPageComponent],
      providers: [
        ...provideCommonTestDependencies(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ id: 'park-1', lang: 'fr' }),
            },
            parent: null,
            paramMap: paramMapSubject.asObservable(),
          },
        },
        { provide: ParksApiService, useValue: parksApiService },
        { provide: SeoService, useValue: seoService },
        { provide: SsrHttpStatusService, useValue: ssrHttpStatusService },
        { provide: TranslationService, useClass: FakeTranslationService },
      ],
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      parkOpeningHours: {
        page: {
          open: 'Ouvert',
          closed: 'Fermé',
          notDefined: 'Non renseigné',
          dayHasInformation: 'Information ajoutée',
          daysCount: 'Dates renseignées ce mois',
          nextDaySuffix: 'lendemain',
          kicker: 'Horaires',
          unavailableTitle: 'Horaires indisponibles pour {{name}}',
          unavailableMessage:
            'Les horaires actuels sont uniquement proposés pour les parcs ouverts.',
          backToPark: 'Retour à {{name}}',
        },
      },
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(ParkOpeningHoursPageComponent);
    component = fixture.componentInstance;
  });

  it('builds month groups as calendar weeks with the year in the month label', () => {
    const group = component.monthGroup(
      createCalendar([
        createOpenDay('2026-07-04', '10:00', '18:00'),
        createOpenDay('2026-07-05', '10:00', '18:00'),
        createClosedDay('2026-07-06'),
      ]),
    );

    const cells = group.weeks.flatMap((week) => week.cells);

    expect(group.label).toContain('2026');
    expect(group.openDays).toBe(2);
    expect(group.closedDays).toBe(1);
    expect(
      cells.filter((cell) => cell.localDate?.startsWith('2026-07')).length,
    ).toBe(31);
    expect(cells.length % 7).toBe(0);
  });

  it('does not expose technical or closed-day notes on the public calendar', () => {
    component['currentLanguage'].set('fr');

    const technicalClosedDay: ParkOpeningHoursDay = createClosedDay(
      '2026-07-01',
      'Regle de couverture pour eviter les dates non definies',
    );
    const technicalOpenDay: ParkOpeningHoursDay = createOpenDay(
      '2026-07-02',
      '10:00',
      '18:00',
      'Regle de couverture pour eviter les dates non definies',
    );
    const publicOpenDay: ParkOpeningHoursDay = createOpenDay(
      '2026-07-03',
      '10:00',
      '18:00',
      'Ouverture estivale 2026',
    );
    const internalClosedDay: ParkOpeningHoursDay = createClosedDay(
      '2026-07-04',
      'Maintenance equipe',
    );

    expect(component.publicDayNote(technicalClosedDay)).toBeNull();
    expect(component.publicDayNote(internalClosedDay)).toBeNull();
    expect(component.publicDayNote(technicalOpenDay)).toBeNull();
    expect(component.publicDayNote(publicOpenDay)).toBe(
      'Ouverture estivale 2026',
    );
  });

  it('exposes public notes in the selected day details instead of expanding calendar cells', () => {
    component['currentLanguage'].set('fr');

    const calendar: ParkOpeningHoursCalendar = createCalendar([
      createOpenDay('2026-07-04', '10:00', '22:00', 'Soirée Halloween'),
    ]);
    const group = component.monthGroup(calendar);
    const selectedCell = group.weeks
      .flatMap((week) => week.cells)
      .find((cell) => cell.localDate === '2026-07-04');

    expect(selectedCell).toBeDefined();
    component.selectDay(selectedCell!);

    const details = component.selectedDayDetails(calendar);

    expect(component.hasPublicDayNote(selectedCell!.day)).toBe(true);
    expect(details.localDate).toBe('2026-07-04');
    expect(details.status).toBe('open');
    expect(details.note).toBe('Soirée Halloween');
  });

  it('only exposes day notes when they are explicitly localized for the current page language', () => {
    const frenchOnlyDay: ParkOpeningHoursDay = createOpenDay(
      '2026-07-04',
      '10:00',
      '18:00',
      'Ouverture estivale 2026',
    );
    const localizedGermanDay: ParkOpeningHoursDay = {
      ...frenchOnlyDay,
      labels: [{ languageCode: 'de', value: 'Sommeröffnung 2026' }],
    };

    component['currentLanguage'].set('de');

    expect(component.publicDayNote(frenchOnlyDay)).toBeNull();
    expect(component.publicDayNote(localizedGermanDay)).toBe(
      'Sommeröffnung 2026',
    );
  });

  it('classifies opening amplitudes for calendar colors', () => {
    expect(component.dayTone(createClosedDay('2026-07-01'))).toBe('closed');
    expect(
      component.dayTone(createOpenDay('2026-07-02', '10:00', '15:00')),
    ).toBe('short');
    expect(
      component.dayTone(createOpenDay('2026-07-03', '10:00', '18:00')),
    ).toBe('standard');
    expect(
      component.dayTone(createOpenDay('2026-07-04', '09:00', '20:00')),
    ).toBe('long');
  });

  it('renders the opening hours sharing panel for the park', () => {
    parksApiService.getParkDetailSummary.mockReturnValue(
      of(createSummary('park-1')),
    );
    parksApiService.getParkOpeningHours.mockReturnValue(
      of(createCalendar([createOpenDay('2026-07-04', '10:00', '18:00')])),
    );

    fixture.detectChanges();
    paramMapSubject.next(convertToParamMap({ id: 'park-1', lang: 'fr' }));
    fixture.detectChanges();

    expect(
      (fixture.nativeElement as HTMLElement).querySelector(
        '.park-opening-hours-page.public-page',
      ),
    ).not.toBeNull();

    const sharePanel: PublicSharePanelComponent = fixture.debugElement.query(
      By.directive(PublicSharePanelComponent),
    ).componentInstance as PublicSharePanelComponent;

    expect(sharePanel.targetType).toBe('Park');
    expect(sharePanel.targetId).toBe('park-1');
    expect(sharePanel.targetTitle).toBe('First Park');
    expect(sharePanel.titleKey).toBe('shareSocial.openingHours.title');
    expect(sharePanel.descriptionKey).toBe(
      'shareSocial.openingHours.description',
    );
    expect(sharePanel.textKey).toBe('shareSocial.openingHours.text');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'Dates renseignées ce mois',
    );
  });

  it.each<ParkStatus>([
    'ClosedDefinitively',
    'Planned',
    'UnderConstruction',
    'TemporarilyClosed',
    'Cancelled',
  ])('does not load or index opening hours when the park status is %s', (status: ParkStatus) => {
    parksApiService.getParkDetailSummary.mockReturnValue(
      of(createSummary(`park-${status}`, status)),
    );

    fixture.detectChanges();
    paramMapSubject.next(
      convertToParamMap({ id: `park-${status}`, lang: 'fr' }),
    );
    fixture.detectChanges();

    expect(parksApiService.getParkOpeningHours).not.toHaveBeenCalled();
    expect(ssrHttpStatusService.setNotFound).toHaveBeenCalledTimes(1);
    expect(seoService.applyParkUnavailableFeatureSeo).toHaveBeenCalledWith(
      expect.objectContaining({ status }),
      'openingHours',
      'fr',
      expect.any(String),
      null,
      expect.stringContaining(`/park-${status}/`),
    );
    expect(seoService.applyParkOpeningHoursSeo).not.toHaveBeenCalled();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'Les horaires actuels sont uniquement proposés pour les parcs ouverts.',
    );
  });

  it('refreshes unavailable opening-hours SEO when the language changes', () => {
    const translationService: FakeTranslationService = TestBed.inject(
      TranslationService,
    ) as unknown as FakeTranslationService;
    parksApiService.getParkDetailSummary.mockReturnValue(
      of(createSummary('park-planned', 'Planned')),
    );

    fixture.detectChanges();
    paramMapSubject.next(
      convertToParamMap({ id: 'park-planned', lang: 'fr' }),
    );
    fixture.detectChanges();
    seoService.applyParkUnavailableFeatureSeo.mockClear();

    translationService.languageChanged.emit('en');
    fixture.detectChanges();

    expect(seoService.applyParkUnavailableFeatureSeo).toHaveBeenLastCalledWith(
      expect.objectContaining({ status: 'Planned' }),
      'openingHours',
      'en',
      expect.any(String),
      null,
      expect.stringContaining('/en/park/park-planned/'),
    );
  });

  it('ignores pending month responses from a previously loaded park', () => {
    const currentMonthKey: string = component['resolveCurrentMonthKey'](null);
    const nextMonthKey: string = component['addMonths'](currentMonthKey, 1);
    const currentRange: {
      from: string;
      to: string;
    } = monthRange(currentMonthKey);
    const nextRange: {
      from: string;
      to: string;
    } = monthRange(nextMonthKey);
    const delayedMonthResponse: Subject<ParkOpeningHoursCalendar> =
      new Subject<ParkOpeningHoursCalendar>();

    parksApiService.getParkDetailSummary.mockImplementation(
      (parkId: string): Observable<ParkDetailSummary> =>
        of(createSummary(parkId)),
    );
    parksApiService.getParkOpeningHours.mockImplementation(
      (
        parkId: string,
        fromDate?: string | null,
        toDate?: string | null,
      ): Observable<ParkOpeningHoursCalendar> => {
        if (parkId === 'park-1' && fromDate === nextRange.from) {
          return delayedMonthResponse.asObservable();
        }

        const range: {
          from: string;
          to: string;
        } = {
          from: fromDate ?? currentRange.from,
          to: toDate ?? currentRange.to,
        };
        const coverageLastDate: string =
          parkId === 'park-1' ? nextRange.to : range.to;

        return of(
          createCalendar([createOpenDay(range.from, '10:00', '18:00')], {
            parkId,
            firstDate: currentRange.from,
            lastDate: coverageLastDate,
            fromDate: range.from,
            toDate: range.to,
          }),
        );
      },
    );

    fixture.detectChanges();
    paramMapSubject.next(convertToParamMap({ id: 'park-1', lang: 'fr' }));
    fixture.detectChanges();

    const firstPageData = component['state']().data;
    expect(firstPageData).toBeDefined();
    component.showNextMonth(firstPageData!);
    paramMapSubject.next(convertToParamMap({ id: 'park-2', lang: 'fr' }));
    fixture.detectChanges();

    delayedMonthResponse.next(
      createCalendar([createOpenDay(nextRange.from, '10:00', '18:00')], {
        parkId: 'park-1',
        firstDate: currentRange.from,
        lastDate: nextRange.to,
        fromDate: nextRange.from,
        toDate: nextRange.to,
      }),
    );
    fixture.detectChanges();

    expect(component['state']().data?.park.id).toBe('park-2');
  });
});

function createCalendar(
  days: ParkOpeningHoursDay[],
  options: Partial<
    Pick<
      ParkOpeningHoursCalendar,
      'parkId' | 'firstDate' | 'lastDate' | 'fromDate' | 'toDate'
    >
  > = {},
): ParkOpeningHoursCalendar {
  const firstDate: string = days[0]?.localDate ?? '2026-07-01';
  const lastDate: string = days[days.length - 1]?.localDate ?? firstDate;

  return {
    parkId: options.parkId ?? 'park-1',
    timeZoneId: 'Europe/Paris',
    updatedAtUtc: '2026-06-29T00:00:00Z',
    firstDate: options.firstDate ?? firstDate,
    lastDate: options.lastDate ?? lastDate,
    fromDate: options.fromDate ?? firstDate,
    toDate: options.toDate ?? lastDate,
    days,
  };
}

function createSummary(
  parkId: string,
  status: ParkStatus = 'Operating',
): ParkDetailSummary {
  return {
    park: {
      id: parkId,
      name: parkId === 'park-1' ? 'First Park' : 'Second Park',
      latitude: 50,
      longitude: 3,
      status,
    },
    mainImage: null,
    references: {},
    stats: {
      totalItems: 0,
      zoneCount: 0,
      attractionCount: 0,
      restaurantCount: 0,
      showCount: 0,
      shopCount: 0,
      hotelCount: 0,
      countsByCategory: {},
    },
  };
}

function monthRange(monthKey: string): {
  from: string;
  to: string;
} {
  const [yearText, monthText] = monthKey.split('-');
  const year: number = Number.parseInt(yearText, 10);
  const month: number = Number.parseInt(monthText, 10);
  const lastDay: number = new Date(Date.UTC(year, month, 0)).getUTCDate();

  return {
    from: `${monthKey}-01`,
    to: `${monthKey}-${lastDay.toString().padStart(2, '0')}`,
  };
}

function createOpenDay(
  localDate: string,
  opensAt: string,
  closesAt: string,
  label: string | null = null,
): ParkOpeningHoursDay {
  return {
    localDate,
    isClosed: false,
    isDefined: true,
    sourceKind: 'Rule',
    labels: label ? [{ languageCode: 'fr', value: label }] : [],
    reasons: [],
    timeRanges: [createRange(opensAt, closesAt)],
  };
}

function createClosedDay(
  localDate: string,
  reason: string | null = null,
): ParkOpeningHoursDay {
  return {
    localDate,
    isClosed: true,
    isDefined: true,
    sourceKind: 'Rule',
    labels: [],
    reasons: reason ? [{ languageCode: 'fr', value: reason }] : [],
    timeRanges: [],
  };
}

function createRange(
  opensAt: string,
  closesAt: string,
): ParkOpeningHoursTimeRange {
  return {
    opensAt,
    closesAt,
    closesNextDay: false,
    lastAdmissionAt: null,
    lastAdmissionNextDay: false,
  };
}
