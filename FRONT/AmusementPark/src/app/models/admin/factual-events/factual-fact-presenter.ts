import { FactualFactPresentation } from './factual-fact-presentation.model';
import { FactualFactValueAdmin } from './factual-event-administration.models';

export function presentFactualFact(value: FactualFactValueAdmin | null): FactualFactPresentation {
  if (!value) {
    return emptyPresentation();
  }

  const fields: ReadonlyMap<string, string> = new Map<string, string>(
    value.canonicalValue.split(';').map((part: string): [string, string] => {
      const separator: number = part.indexOf('=');
      return separator < 0
        ? [part, '']
        : [part.slice(0, separator), part.slice(separator + 1)];
    }),
  );
  const coverage: string | undefined = fields.get('coverage');
  const coverageParts: string[] = coverage && coverage !== 'none' ? coverage.split('/') : [];
  const isOpeningCalendar: boolean = fields.has('timezone')
    && fields.has('rules')
    && fields.has('overrides')
    && fields.has('sha256');
  return {
    isOpeningCalendar,
    isPresent: true,
    rawValue: isOpeningCalendar ? '' : value.canonicalValue,
    timeZone: isOpeningCalendar ? fields.get('timezone') ?? null : null,
    coverageStart: coverageParts[0] ?? null,
    coverageEnd: coverageParts[1] ?? null,
    rulesCount: isOpeningCalendar ? parseCount(fields.get('rules')) : null,
    overridesCount: isOpeningCalendar ? parseCount(fields.get('overrides')) : null,
  };
}

function emptyPresentation(): FactualFactPresentation {
  return {
    isOpeningCalendar: false,
    isPresent: false,
    rawValue: '',
    timeZone: null,
    coverageStart: null,
    coverageEnd: null,
    rulesCount: null,
    overridesCount: null,
  };
}

function parseCount(value: string | undefined): number | null {
  const parsed: number = Number(value);
  return Number.isInteger(parsed) && parsed >= 0 ? parsed : null;
}
