export type SortDirection = 'asc' | 'desc';

export interface SortDefinition<TField extends string = string> {
  field: TField;
  direction: SortDirection;
}
