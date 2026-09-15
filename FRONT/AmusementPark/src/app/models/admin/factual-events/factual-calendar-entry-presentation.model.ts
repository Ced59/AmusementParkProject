export type FactualCalendarEntryKind = 'regular' | 'override';

export type FactualCalendarWeekday =
  | 'sunday'
  | 'monday'
  | 'tuesday'
  | 'wednesday'
  | 'thursday'
  | 'friday'
  | 'saturday';

export interface FactualCalendarEntryPresentation {
  readonly kind: FactualCalendarEntryKind;
  readonly startDate: string;
  readonly endDate: string;
  readonly days: readonly FactualCalendarWeekday[];
  readonly isClosed: boolean;
  readonly priority: number | null;
  readonly tieOrder: number | null;
  readonly isChanged: boolean;
  readonly openingWindows: readonly string[];
  readonly hiddenOpeningWindowsCount: number;
}
