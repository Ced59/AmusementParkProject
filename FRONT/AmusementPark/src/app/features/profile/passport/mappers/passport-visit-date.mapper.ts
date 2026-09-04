import { PassportVisitDate } from '@app/models/passport/passport-visit.models';

export function formatPassportVisitDate(date: PassportVisitDate, language: string): string {
  const value: Date = new Date(Date.UTC(date.year, (date.month ?? 1) - 1, date.day ?? 1));
  const options: Intl.DateTimeFormatOptions = date.precision === 'Year'
    ? { year: 'numeric', timeZone: 'UTC' }
    : date.precision === 'Month'
      ? { month: 'long', year: 'numeric', timeZone: 'UTC' }
      : { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' };
  const label: string = new Intl.DateTimeFormat(language, options).format(value);
  return date.isApproximate ? `≈ ${label}` : label;
}
