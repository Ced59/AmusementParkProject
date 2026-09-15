import { presentFactualFact } from './factual-fact-presenter';

describe('presentFactualFact', (): void => {
  it('turns an opening calendar snapshot into contextual evidence without exposing its hash', (): void => {
    const presentation = presentFactualFact({
      kind: 'Text',
      canonicalValue: 'snapshot=2;timezone=Europe/Paris;coverage=2026-04-01/2026-11-02;rules=7;overrides=3;evidence=complete;entries=R|2026-04-01|2026-06-30|1,2|O|2|09:30-18:00,10:00-23:30|2|1~D|2026-05-01|2026-05-01||C|||0|;entryCount=3;changedEntryCount=1;sha256=technical-secret',
      unitCode: null,
    });

    expect(presentation).toEqual({
      isOpeningCalendar: true,
      isPresent: true,
      rawValue: '',
      timeZone: 'Europe/Paris',
      coverageStart: '2026-04-01',
      coverageEnd: '2026-11-02',
      rulesCount: 7,
      overridesCount: 3,
      evidenceAvailable: true,
      calendarEntries: [
        {
          kind: 'regular',
          startDate: '2026-04-01',
          endDate: '2026-06-30',
          days: ['monday', 'tuesday'],
          isClosed: false,
          priority: 2,
          tieOrder: 1,
          isChanged: true,
          openingWindows: ['09:30 → 18:00', '10:00 → 23:30'],
          hiddenOpeningWindowsCount: 0,
        },
        {
          kind: 'override',
          startDate: '2026-05-01',
          endDate: '2026-05-01',
          days: [],
          isClosed: true,
          priority: null,
          tieOrder: null,
          isChanged: false,
          openingWindows: [],
          hiddenOpeningWindowsCount: 0,
        },
      ],
      hiddenCalendarEntriesCount: 1,
      hiddenChangedCalendarEntriesCount: 0,
    });
    expect(JSON.stringify(presentation)).not.toContain('technical-secret');
  });

  it('identifies migrated historical evidence that could not be reconstructed', (): void => {
    const presentation = presentFactualFact({
      kind: 'Text',
      canonicalValue: 'snapshot=2;timezone=Europe/Paris;coverage=2026-01-01/2026-03-31;rules=2;overrides=0;evidence=unavailable;entries=;entryCount=0;sha256=technical-secret',
      unitCode: null,
    });

    expect(presentation.isOpeningCalendar).toBe(true);
    expect(presentation.evidenceAvailable).toBe(false);
    expect(presentation.rawValue).toBe('');
    expect(JSON.stringify(presentation)).not.toContain('technical-secret');
  });

  it('preserves a non-calendar fact as readable raw content', (): void => {
    const presentation = presentFactualFact({
      kind: 'Text',
      canonicalValue: 'Nouveau nom',
      unitCode: null,
    });

    expect(presentation.isOpeningCalendar).toBe(false);
    expect(presentation.rawValue).toBe('Nouveau nom');
  });

  it('keeps the unit beside a numeric fact so monetary values stay unambiguous', (): void => {
    const presentation = presentFactualFact({
      kind: 'Money',
      canonicalValue: '49.00',
      unitCode: 'EUR',
    });

    expect(presentation.rawValue).toBe('49.00 EUR');
  });
});
