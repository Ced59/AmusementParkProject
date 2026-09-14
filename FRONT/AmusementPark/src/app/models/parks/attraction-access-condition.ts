import { LocalizedItem } from '../shared/localized-item';
import { AttractionAccessConditionType } from './attraction-access-condition-type';
import { AttractionAccessConditionUnit } from './attraction-access-condition-unit';
import { AttractionAccessConditionSourceKind } from './attraction-access-condition-source-kind';
import { AttractionAccessConditionConfidence } from './attraction-access-condition-confidence';
import { AttractionAccessConditionScope } from './attraction-access-condition-scope';

export interface AttractionAccessCondition {
  type: AttractionAccessConditionType;
  typeKey?: string | null;
  isCustom?: boolean | null;
  customTypeKey?: string | null;
  customTypeLabel?: LocalizedItem<string>[] | null;
  value?: number | null;
  unit?: AttractionAccessConditionUnit | null;
  requiresAccompaniment?: boolean | null;
  minimumCompanionAge?: number | null;
  label?: LocalizedItem<string>[] | null;
  description?: LocalizedItem<string>[] | null;
  displayOrder?: number | null;
  provenanceSchemaVersion?: number | null;
  sourceKind?: AttractionAccessConditionSourceKind | null;
  sourceUrl?: string | null;
  sourceReference?: string | null;
  collectedAtUtc?: string | null;
  verifiedAtUtc?: string | null;
  sourceLanguageCode?: string | null;
  sourceSummary?: LocalizedItem<string>[] | null;
  sourceConfidence?: AttractionAccessConditionConfidence | null;
  scope?: AttractionAccessConditionScope | null;
  scopeDetail?: string | null;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
}
