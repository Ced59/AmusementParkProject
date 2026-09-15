import { FactualFactPresentation } from './factual-fact-presentation.model';
import {
  FactualCalendarEntryPresentation,
  FactualCalendarWeekday,
} from './factual-calendar-entry-presentation.model';
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
  const isOpeningCalendar: boolean = fields.get('snapshot') === '2'
    && fields.has('timezone')
    && fields.has('rules')
    && fields.has('overrides')
    && fields.has('sha256');
  const changedCalendarEntryCount: number = isOpeningCalendar
    ? parseCount(fields.get('changedEntryCount')) ?? 0
    : 0;
  const calendarEntries: readonly FactualCalendarEntryPresentation[] = isOpeningCalendar
    ? parseCalendarEntries(fields.get('entries'), changedCalendarEntryCount)
    : [];
  const calendarEntryCount: number = isOpeningCalendar
    ? parseCount(fields.get('entryCount')) ?? calendarEntries.length
    : 0;
  return {
    isOpeningCalendar,
    isPresent: true,
    rawValue: isOpeningCalendar ? '' : presentCanonicalValue(value),
    timeZone: isOpeningCalendar ? fields.get('timezone') ?? null : null,
    coverageStart: coverageParts[0] ?? null,
    coverageEnd: coverageParts[1] ?? null,
    rulesCount: isOpeningCalendar ? parseCount(fields.get('rules')) : null,
    overridesCount: isOpeningCalendar ? parseCount(fields.get('overrides')) : null,
    evidenceAvailable: isOpeningCalendar && fields.get('evidence') === 'complete',
    calendarEntries,
    hiddenCalendarEntriesCount: Math.max(0, calendarEntryCount - calendarEntries.length),
    hiddenChangedCalendarEntriesCount: Math.max(
      0,
      changedCalendarEntryCount - calendarEntries.filter(
        (entry: FactualCalendarEntryPresentation): boolean => entry.isChanged,
      ).length,
    ),
  };
}

function presentCanonicalValue(value: FactualFactValueAdmin): string {
  const unitCode: string | null = value.unitCode?.trim() || null;
  return unitCode ? `${value.canonicalValue} ${unitCode}` : value.canonicalValue;
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
    evidenceAvailable: false,
    calendarEntries: [],
    hiddenCalendarEntriesCount: 0,
    hiddenChangedCalendarEntriesCount: 0,
  };
}

function parseCalendarEntries(
  value: string | undefined,
  changedEntryCount: number,
): readonly FactualCalendarEntryPresentation[] {
  if (!value) {
    return [];
  }

  return value
    .split('~')
    .map((entry: string, index: number): FactualCalendarEntryPresentation | null =>
      parseCalendarEntry(entry, index < changedEntryCount))
    .filter((entry: FactualCalendarEntryPresentation | null): entry is FactualCalendarEntryPresentation => entry !== null);
}

function parseCalendarEntry(value: string, isChanged: boolean): FactualCalendarEntryPresentation | null {
  const parts: string[] = value.split('|');
  const startDate: string = parts[1] ?? '';
  const endDate: string = parts[2] ?? '';
  const state: string = parts[4] ?? '';
  const kind: FactualCalendarEntryPresentation['kind'] | null = parts[0] === 'R'
    ? 'regular'
    : parts[0] === 'D' ? 'override' : null;
  if (!kind
    || !/^\d{4}-\d{2}-\d{2}$/.test(startDate)
    || !/^\d{4}-\d{2}-\d{2}$/.test(endDate)
    || !['C', 'O'].includes(state)) {
    return null;
  }

  const windows: readonly string[] = parseOpeningWindows(parts[6]);
  const windowCount: number = parseCount(parts[7]) ?? windows.length;
  return {
    kind,
    startDate,
    endDate,
    days: kind === 'regular' ? parseWeekdays(parts[3]) : [],
    isClosed: state === 'C',
    priority: kind === 'regular' ? parseCount(parts[5]) : null,
    tieOrder: kind === 'regular' ? parseCount(parts[8]) : null,
    isChanged,
    openingWindows: windows,
    hiddenOpeningWindowsCount: Math.max(0, windowCount - windows.length),
  };
}

function parseOpeningWindows(value: string | undefined): readonly string[] {
  return (value ?? '')
    .split(',')
    .filter((window: string): boolean => /^\d{2}:\d{2}-\d{2}:\d{2}(?:\+1)?(?:@\d{2}:\d{2}(?:\+1)?)?$/.test(window))
    .map((window: string): string => window.replace('-', ' → ').replace('@', ' · '));
}

function parseWeekdays(value: string | undefined): readonly FactualCalendarWeekday[] {
  const weekdays: readonly FactualCalendarWeekday[] = [
    'sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday',
  ];
  return (value ?? '')
    .split(',')
    .map((day: string): FactualCalendarWeekday | null => {
      const index: number = Number(day);
      return Number.isInteger(index) && index >= 0 && index < weekdays.length
        ? weekdays[index]
        : null;
    })
    .filter((day: FactualCalendarWeekday | null): day is FactualCalendarWeekday => day !== null);
}

function parseCount(value: string | undefined): number | null {
  const parsed: number = Number(value);
  return Number.isInteger(parsed) && parsed >= 0 ? parsed : null;
}
