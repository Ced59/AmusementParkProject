import { LocalizedItem } from '../shared/localized-item';
import { AttractionAccessConditionType } from './attraction-access-condition-type';
import { AttractionAccessConditionUnit } from './attraction-access-condition-unit';

export interface AttractionAccessCondition {
  type: AttractionAccessConditionType;
  isCustom?: boolean | null;
  value?: number | null;
  unit?: AttractionAccessConditionUnit | null;
  requiresAccompaniment?: boolean | null;
  minimumCompanionAge?: number | null;
  label?: LocalizedItem<string>[] | null;
  description?: LocalizedItem<string>[] | null;
  displayOrder?: number | null;
}
