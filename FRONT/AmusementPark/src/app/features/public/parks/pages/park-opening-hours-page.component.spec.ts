import { EventEmitter } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';

import { ParkOpeningHoursDay, ParkOpeningHoursTimeRange } from '@app/models/parks/park-opening-hours';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ParkOpeningHoursPageComponent } from './park-opening-hours-page.component';

class FakeTranslationService {
  public readonly languageChanged: EventEmitter<string> = new EventEmitter<string>();

  getCurrentLang(): string {
    return 'fr';
  }
}

describe('ParkOpeningHoursPageComponent', () => {
  let fixture: ComponentFixture<ParkOpeningHoursPageComponent>;
  let component: ParkOpeningHoursPageComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, ParkOpeningHoursPageComponent],
      providers: [
        ...provideCommonTestDependencies(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ id: 'park-1', lang: 'fr' }) },
            parent: null,
            paramMap: of(convertToParamMap({ id: 'park-1', lang: 'fr' }))
          }
        },
        {
          provide: ParksApiService,
          useValue: jasmine.createSpyObj<ParksApiService>('ParksApiService', ['getParkDetailSummary', 'getParkOpeningHours'])
        },
        { provide: SeoService, useValue: jasmine.createSpyObj<SeoService>('SeoService', ['applyParkOpeningHoursSeo']) },
        { provide: SsrHttpStatusService, useValue: jasmine.createSpyObj<SsrHttpStatusService>('SsrHttpStatusService', ['setNotFound']) },
        { provide: TranslationService, useClass: FakeTranslationService }
      ]
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      parkOpeningHours: {
        page: {
          open: 'Ouvert',
          closed: 'Fermé'
        }
      }
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(ParkOpeningHoursPageComponent);
    component = fixture.componentInstance;
  });

  it('builds month groups as calendar weeks with the year in the month label', () => {
    const groups = component.monthGroups([
      createOpenDay('2026-07-04', '10:00', '18:00'),
      createOpenDay('2026-07-05', '10:00', '18:00'),
      createClosedDay('2026-07-06')
    ]);

    const cells = groups[0].weeks.flatMap((week) => week.cells);

    expect(groups.length).toBe(1);
    expect(groups[0].label).toContain('2026');
    expect(groups[0].openDays).toBe(2);
    expect(groups[0].closedDays).toBe(1);
    expect(cells.filter((cell) => cell.localDate?.startsWith('2026-07')).length).toBe(31);
    expect(cells.length % 7).toBe(0);
  });

  it('does not expose technical or closed-day notes on the public calendar', () => {
    const technicalClosedDay: ParkOpeningHoursDay = createClosedDay(
      '2026-07-01',
      'Regle de couverture pour eviter les dates non definies'
    );
    const technicalOpenDay: ParkOpeningHoursDay = createOpenDay(
      '2026-07-02',
      '10:00',
      '18:00',
      'Regle de couverture pour eviter les dates non definies'
    );
    const publicOpenDay: ParkOpeningHoursDay = createOpenDay('2026-07-03', '10:00', '18:00', 'Ouverture estivale 2026');

    expect(component.publicDayNote(technicalClosedDay)).toBeNull();
    expect(component.publicDayNote(technicalOpenDay)).toBeNull();
    expect(component.publicDayNote(publicOpenDay)).toBe('Ouverture estivale 2026');
  });

  it('classifies opening amplitudes for calendar colors', () => {
    expect(component.dayTone(createClosedDay('2026-07-01'))).toBe('closed');
    expect(component.dayTone(createOpenDay('2026-07-02', '10:00', '15:00'))).toBe('short');
    expect(component.dayTone(createOpenDay('2026-07-03', '10:00', '18:00'))).toBe('standard');
    expect(component.dayTone(createOpenDay('2026-07-04', '09:00', '20:00'))).toBe('long');
  });
});

function createOpenDay(localDate: string, opensAt: string, closesAt: string, label: string | null = null): ParkOpeningHoursDay {
  return {
    localDate,
    isClosed: false,
    isDefined: true,
    sourceKind: 'Rule',
    label,
    reason: null,
    timeRanges: [createRange(opensAt, closesAt)]
  };
}

function createClosedDay(localDate: string, reason: string | null = null): ParkOpeningHoursDay {
  return {
    localDate,
    isClosed: true,
    isDefined: true,
    sourceKind: 'Rule',
    label: null,
    reason,
    timeRanges: []
  };
}

function createRange(opensAt: string, closesAt: string): ParkOpeningHoursTimeRange {
  return {
    opensAt,
    closesAt,
    closesNextDay: false,
    lastAdmissionAt: null,
    lastAdmissionNextDay: false
  };
}
