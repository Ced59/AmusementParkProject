import { PublicHistoricalPeriod } from '@app/models/history/public-park-history.models';
import { formatPublicHistoricalPeriod } from './historical-period-label';

describe('historical period label', () => {
  const translate = (key: string, parameters?: Record<string, string>): string => {
    const templates: Record<string, string> = {
      'history.explorer.unknownDate': 'Date inconnue',
      'history.explorer.approximateDate': 'Vers {{date}}',
      'history.explorer.datePeriod': 'Du {{start}} au {{end}}',
      'history.explorer.dateFrom': 'Depuis {{date}}',
      'history.explorer.dateUntil': 'Jusqu’au {{date}}',
      'history.explorer.dateQualifiers.Before': 'Avant {{date}}',
      'history.explorer.dateQualifiers.Circa': 'Vers {{date}}'
    };
    return Object.entries(parameters ?? {}).reduce(
      (value: string, [name, replacement]: [string, string]): string => value.replace(`{{${name}}}`, replacement),
      templates[key] ?? key
    );
  };

  it('renders both distinct boundaries of a historical period', () => {
    const period: PublicHistoricalPeriod = {
      start: { year: 2020, month: 1, precision: 'Month' },
      end: { year: 2020, month: 3, precision: 'Month' },
      startConfidence: 'Confirmed',
      endConfidence: 'Confirmed'
    };

    expect(formatPublicHistoricalPeriod(period, 'fr', translate)).toBe('Du janvier 2020 au mars 2020');
  });

  it('does not turn an open period into a punctual date', () => {
    const period: PublicHistoricalPeriod = {
      start: { year: 1998, precision: 'Year' },
      end: null,
      startConfidence: 'Confirmed',
      endConfidence: 'Confirmed'
    };

    expect(formatPublicHistoricalPeriod(period, 'fr', translate)).toBe('Depuis 1998');
  });

  it('preserves qualifiers and approximation without duplicating circa', () => {
    const beforePeriod: PublicHistoricalPeriod = {
      start: null,
      end: { year: 1998, precision: 'Year', qualifier: 'Before', isApproximate: true },
      startConfidence: 'Confirmed',
      endConfidence: 'Estimated'
    };
    const circaPeriod: PublicHistoricalPeriod = {
      start: { year: 2001, precision: 'Year', qualifier: 'Circa', isApproximate: true },
      end: { year: 2001, precision: 'Year', qualifier: 'Circa', isApproximate: true },
      startConfidence: 'Estimated',
      endConfidence: 'Estimated'
    };

    expect(formatPublicHistoricalPeriod(beforePeriod, 'fr', translate)).toBe('Jusqu’au Vers Avant 1998');
    expect(formatPublicHistoricalPeriod(circaPeriod, 'fr', translate)).toBe('Vers 2001');
  });
});
