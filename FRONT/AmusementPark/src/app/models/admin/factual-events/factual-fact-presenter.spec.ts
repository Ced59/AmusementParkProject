import { presentFactualFact } from './factual-fact-presenter';

describe('presentFactualFact', (): void => {
  it('turns an opening calendar fingerprint into readable evidence without exposing its hash', (): void => {
    const presentation = presentFactualFact({
      kind: 'Text',
      canonicalValue: 'timezone=Europe/Paris;coverage=2026-04-01/2026-11-02;rules=7;overrides=3;windows=09:30-18:00,10:00-23:30;windowCount=2;sha256=technical-secret',
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
      openingWindows: ['09:30 → 18:00', '10:00 → 23:30'],
      hiddenOpeningWindowsCount: 0,
    });
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
