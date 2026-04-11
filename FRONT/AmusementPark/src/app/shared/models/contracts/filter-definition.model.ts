export type FilterOperator =
  | 'eq'
  | 'contains'
  | 'startsWith'
  | 'endsWith'
  | 'in'
  | 'gt'
  | 'gte'
  | 'lt'
  | 'lte';

export type FilterValue = string | number | boolean | null | readonly string[] | readonly number[];

export interface FilterDefinition<TKey extends string = string> {
  key: TKey;
  operator: FilterOperator;
  value: FilterValue;
}
