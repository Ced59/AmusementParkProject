export interface UiSelectOptionModel {
  labelKey?: string;
  label?: string;
  value: string | null;
}

export interface UiSearchPanelSelectFilterModel {
  id: string;
  labelKey: string;
  selectedValue: string | null;
  options: UiSelectOptionModel[];
  hidden?: boolean;
}

export interface UiCategoryChipModel {
  labelKey?: string;
  label?: string;
  value: string | null;
  iconClass?: string;
  selected?: boolean;
  disabled?: boolean;
}
