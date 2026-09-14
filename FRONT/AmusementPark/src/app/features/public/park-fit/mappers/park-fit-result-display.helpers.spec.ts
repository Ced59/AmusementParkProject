import { ParkFitCriticalSource, ParkFitOpeningTimeRange } from '@app/models/park-fit/park-fit-search.models';
import {
  formatParkFitDate,
  formatParkFitOpeningTimeRange,
  parkFitComponentKindKey,
  parkFitScoreReasonKey,
  resolveParkFitSourceSummary,
  resolveParkFitSourceUrl
} from './park-fit-result-display.helpers';

describe('park fit result display helpers', () => {
  it('never exposes an unknown backend code as a translation key', () => {
    expect(parkFitComponentKindKey('InternalFutureCode')).toBe(
      'parkFit.results.componentKinds.Unknown'
    );
    expect(parkFitScoreReasonKey('InternalFutureCode')).toBe(
      'parkFit.results.scoreReasons.Unknown'
    );
  });

  it('chooses the requested localized proof before the English fallback', () => {
    const source: ParkFitCriticalSource = buildSource();

    expect(resolveParkFitSourceSummary(source, 'fr')).toBe('Condition officielle.');
    expect(resolveParkFitSourceSummary(source, 'nl')).toBe('Official condition.');
  });

  it('only turns secure public URLs into links', () => {
    expect(resolveParkFitSourceUrl(buildSource())).toBe('https://example.test/access');
    expect(resolveParkFitSourceUrl({ ...buildSource(), url: 'javascript:alert(1)' })).toBeNull();
    expect(resolveParkFitSourceUrl({ ...buildSource(), url: null })).toBeNull();
  });

  it('formats verification timestamps against their UTC calendar date', () => {
    const formatter = { format: vi.fn().mockReturnValue('1 septembre 2026') };
    const dateTimeFormat = vi.spyOn(Intl, 'DateTimeFormat').mockImplementation(
      function (): Intl.DateTimeFormat {
        return formatter as unknown as Intl.DateTimeFormat;
      }
    );

    expect(formatParkFitDate('2026-09-01T00:00:00Z', 'fr')).toBe('1 septembre 2026');
    expect(dateTimeFormat).toHaveBeenCalledWith('fr', expect.objectContaining({ timeZone: 'UTC' }));

    dateTimeFormat.mockRestore();
  });

  it('keeps the last-admission cutoff and next-day marker in a displayed range', () => {
    const range: ParkFitOpeningTimeRange = {
      opensAt: '20:00',
      closesAt: '01:00',
      closesNextDay: true,
      lastAdmissionAt: '00:15',
      lastAdmissionNextDay: true
    };
    const translate = (
      key: string,
      params: Record<string, string | number> = {}
    ): string => {
      if (key.endsWith('.nextDay')) {
        return ' le lendemain';
      }

      if (key.endsWith('.timeRange')) {
        return `${params['opensAt']} – ${params['closesAt']}${params['nextDay']}`;
      }

      return `Dernière admission : ${params['time']}${params['nextDay']}`;
    };

    expect(formatParkFitOpeningTimeRange(range, translate))
      .toBe('20:00 – 01:00 le lendemain · Dernière admission : 00:15 le lendemain');
  });
});

function buildSource(): ParkFitCriticalSource {
  return {
    kind: 'Official',
    url: 'https://example.test/access',
    reference: 'internal-reference-never-rendered',
    languageCode: 'fr',
    collectedAtUtc: '2026-08-01T00:00:00Z',
    verifiedAtUtc: '2026-09-01T00:00:00Z',
    confidence: 'High',
    summaries: [
      { languageCode: 'en', value: 'Official condition.' },
      { languageCode: 'fr', value: 'Condition officielle.' }
    ]
  };
}
