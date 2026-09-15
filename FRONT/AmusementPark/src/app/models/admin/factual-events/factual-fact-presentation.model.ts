export interface FactualFactPresentation {
  readonly isOpeningCalendar: boolean;
  readonly isPresent: boolean;
  readonly rawValue: string;
  readonly timeZone: string | null;
  readonly coverageStart: string | null;
  readonly coverageEnd: string | null;
  readonly rulesCount: number | null;
  readonly overridesCount: number | null;
}
