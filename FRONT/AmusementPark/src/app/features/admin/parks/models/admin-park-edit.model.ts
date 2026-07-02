import { PARK_PHOTO_CATEGORIES, ParkPhotoCategoryDefinition } from '@app/models/images/park-photo-category';
import { ParkAudienceClassification } from '@app/models/parks/park-audience-classification';
import { ParkType } from '@app/models/parks/park-type';
import { ParkStatus } from '@app/models/parks/park-status';

export interface AdminParkTypeOption {
  labelKey: string;
  value: ParkType;
}

export interface AdminParkAudienceClassificationOption {
  labelKey: string;
  value: ParkAudienceClassification;
}

export interface AdminParkStatusOption {
  labelKey: string;
  value: ParkStatus;
}

export interface AdminParkCountryOption {
  code: string;
  label: string;
}


export interface AdminParkPhotoCategoryOption {
  slug: string;
  labelKey: string;
  labelFr: string;
  labelEn: string;
}

export const PARK_PHOTO_CATEGORY_OPTIONS: ReadonlyArray<AdminParkPhotoCategoryOption> = PARK_PHOTO_CATEGORIES.map((
  category: ParkPhotoCategoryDefinition
): AdminParkPhotoCategoryOption => ({
  slug: category.slug,
  labelKey: category.adminLabelKey,
  labelFr: category.labelFr,
  labelEn: category.labelEn
}));
