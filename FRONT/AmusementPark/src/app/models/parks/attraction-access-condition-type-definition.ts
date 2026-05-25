import { LocalizedItem } from '../shared/localized-item';
import { AttractionAccessConditionType } from './attraction-access-condition-type';

export interface AttractionAccessConditionTypeDefinition {
  id: string;
  key: string;
  legacyType: AttractionAccessConditionType;
  isSystem: boolean;
  isActive: boolean;
  labels: LocalizedItem<string>[];
  descriptions: LocalizedItem<string>[];
  sortOrder: number;
}

export interface UpsertAttractionAccessConditionTypeDefinitionRequest {
  key: string;
  legacyType?: AttractionAccessConditionType | null;
  isActive: boolean;
  labels: LocalizedItem<string>[];
  descriptions?: LocalizedItem<string>[] | null;
  sortOrder?: number | null;
}
