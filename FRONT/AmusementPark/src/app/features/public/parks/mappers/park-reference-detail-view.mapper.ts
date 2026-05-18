import { ParkFounder } from '@app/models/parks/park-founder';
import { ParkOperator } from '@app/models/parks/park-operator';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { ParkReferenceDetailViewModel } from '../models/park-reference-detail-view.model';

export function mapParkFounderToReferenceDetailViewModel(
  founder: ParkFounder,
  currentLanguage: string
): ParkReferenceDetailViewModel {
  return {
    id: founder.id ?? null,
    kind: 'founder',
    name: founder.name?.trim() ?? '',
    richDescription: normalizeOptionalString(resolveLocalizedValue(founder.biography, currentLanguage) ?? null),
    badgeKey: 'parks.reference.founder.badge',
    titleKey: 'parks.reference.founder.title',
    descriptionTitleKey: 'parks.reference.founder.descriptionTitle',
    emptyDescriptionKey: 'parks.reference.founder.emptyDescription'
  };
}

export function mapParkOperatorToReferenceDetailViewModel(
  operator: ParkOperator,
  currentLanguage: string
): ParkReferenceDetailViewModel {
  return {
    id: operator.id ?? null,
    kind: 'operator',
    name: operator.name?.trim() ?? '',
    richDescription: normalizeOptionalString(resolveLocalizedValue(operator.description, currentLanguage) ?? null),
    badgeKey: 'parks.reference.operator.badge',
    titleKey: 'parks.reference.operator.title',
    descriptionTitleKey: 'parks.reference.operator.descriptionTitle',
    emptyDescriptionKey: 'parks.reference.operator.emptyDescription'
  };
}

function normalizeOptionalString(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';
  return trimmedValue.length > 0 ? trimmedValue : null;
}
