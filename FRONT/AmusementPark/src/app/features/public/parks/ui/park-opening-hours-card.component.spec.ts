import { WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { ParkOpeningHoursCalendar, ParkOpeningHoursDay, ParkOpeningHoursTimeRange } from '@app/models/parks/park-opening-hours';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ParkOpeningHoursCardComponent } from './park-opening-hours-card.component';

describe('ParkOpeningHoursCardComponent', () => {
  let fixture: ComponentFixture<ParkOpeningHoursCardComponent>;
  let component: ParkOpeningHoursCardComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, ParkOpeningHoursCardComponent],
      providers: [
        ...provideCommonTestDependencies()
      ]
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setDefaultLang('fr');
    translateService.setTranslation('fr', {
      parkOpeningHours: {
        card: {
          title: 'Dates et horaires',
          openNow: 'Ouvert en ce moment',
          closedNow: 'Fermé en ce moment',
          opensIn: 'Ouvre dans {{ duration }}',
          openBadge: 'Ouvert',
          closedBadge: 'Fermé',
          today: 'Aujourd\'hui',
          closedToday: 'Fermé aujourd\'hui',
          notDefinedToday: 'Non renseigné aujourd\'hui',
          parkTimeZone: 'Fuseau du parc',
          visitorTimeZone: 'Ton fuseau : {{ timeZone }}',
          viewAll: 'Voir toutes les dates et horaires',
          loading: 'Chargement des horaires',
          weeklyTitle: 'Horaires habituels',
          weeklyHint: 'Horaires habituels, susceptibles de varier selon la date.',
          viewSource: 'Consulter la source des horaires'
        },
        page: { notDefined: 'Non renseigné', closed: 'Fermé' }
      }
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(ParkOpeningHoursCardComponent);
    component = fixture.componentInstance;
  });

  it('shows usual weekly hours without inventing today status or a missing Monday closure', () => {
    const calendar: ParkOpeningHoursCalendar = createCalendar([]);
    calendar.weeklyRules = [{
      id: 'usual', daysOfWeek: ['Tuesday'], isClosed: false, sortOrder: 1,
      timeRanges: [createRange('11:00', '20:00')]
    }];
    component.calendar = calendar;
    component.openingHoursLink = ['/fr/park/park-1/hours'];
    setNow(component, new Date('2026-10-27T12:00:00Z'));

    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const rows: Element[] = Array.from(host.querySelectorAll('.park-opening-hours-card__weekly > div'));
    expect(rows).toHaveLength(7);
    expect(rows[0].textContent).toContain('lundi');
    expect(rows[0].textContent).toContain('Non renseigné');
    expect(rows[1].textContent).toContain('11:00 - 20:00');
    expect(readText(fixture)).toContain('Horaires habituels');
    expect(readText(fixture)).not.toContain('Ouvert en ce moment');
    expect(readText(fixture)).not.toContain('Fermé en ce moment');
    expect(host.querySelector('.park-opening-hours-card__status')).toBeNull();
    expect(host.querySelector('a[ng-reflect-router-link]')).toBeNull();
  });

  it('keeps a dated closure authoritative beside usual weekly hours', () => {
    setNow(component, new Date('2026-10-27T12:00:00Z'));
    const closed: ParkOpeningHoursDay = { ...createOpenDay('2026-10-27', '11:00', '20:00'), isClosed: true, timeRanges: [] };
    const calendar: ParkOpeningHoursCalendar = createCalendar([closed]);
    calendar.weeklyRules = [{ daysOfWeek: ['Tuesday'], isClosed: false, sortOrder: 1, timeRanges: [createRange('11:00', '20:00')] }];
    component.calendar = calendar;

    fixture.detectChanges();

    expect(readText(fixture)).toContain('Fermé en ce moment');
    expect(readText(fixture)).toContain('11:00 - 20:00');
    expect(readText(fixture)).toContain('Horaires habituels');
  });

  it('uses the park time zone to resolve today and open-now status', () => {
    setNow(component, new Date('2026-06-29T22:21:00Z'));
    component.calendar = createCalendar([
      createOpenDay('2026-06-29', '10:30', '20:00')
    ]);

    fixture.detectChanges();

    const textContent: string = readText(fixture);
    expect(textContent).toContain('Ouvert en ce moment');
    expect(textContent).toContain('10:30 - 20:00');
    expect(textContent).not.toContain('Non renseigné aujourd\'hui');
  });

  it('shows the next opening delay when the park is currently closed', () => {
    setNow(component, new Date('2026-06-29T10:30:00Z'));
    component.calendar = createCalendar([
      createOpenDay('2026-06-30', '10:30', '20:00')
    ]);

    fixture.detectChanges();

    expect(readText(fixture)).toContain('Ouvre dans 1 jour 4 h');
  });
});

function setNow(component: ParkOpeningHoursCardComponent, date: Date): void {
  (component as unknown as { now: WritableSignal<Date> }).now.set(date);
}

function readText(fixture: ComponentFixture<ParkOpeningHoursCardComponent>): string {
  const host: HTMLElement = fixture.nativeElement as HTMLElement;
  return host.textContent?.replace(/\s+/g, ' ').trim() ?? '';
}

function createCalendar(days: ParkOpeningHoursDay[]): ParkOpeningHoursCalendar {
  return {
    parkId: 'park-1',
    timeZoneId: 'America/New_York',
    updatedAtUtc: '2026-06-29T00:00:00Z',
    firstDate: '2026-06-01',
    lastDate: '2026-07-31',
    fromDate: '2026-06-28',
    toDate: '2026-07-13',
    days
  };
}

function createOpenDay(localDate: string, opensAt: string, closesAt: string): ParkOpeningHoursDay {
  return {
    localDate,
    isClosed: false,
    isDefined: true,
    sourceKind: 'Rule',
    labels: [],
    reasons: [],
    timeRanges: [createRange(opensAt, closesAt)]
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
