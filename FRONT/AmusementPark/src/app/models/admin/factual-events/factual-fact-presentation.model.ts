import { FactualCalendarEntryPresentation } from './factual-calendar-entry-presentation.model';

export interface FactualFactPresentation {
  readonly isOpeningCalendar: boolean;
  readonly isPresent: boolean;
  readonly rawValue: string;
  readonly timeZone: string | null;
  readonly coverageStart: string | null;
  readonly coverageEnd: string | null;
  readonly rulesCount: number | null;
  readonly overridesCount: number | null;
  readonly evidenceAvailable: boolean;
  readonly calendarEntries: readonly FactualCalendarEntryPresentation[];
  readonly hiddenCalendarEntriesCount: number;
  readonly hiddenChangedCalendarEntriesCount: number;
}
