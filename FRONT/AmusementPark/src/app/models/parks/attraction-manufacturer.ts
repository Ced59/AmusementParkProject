import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { LocalizedItem } from '../shared/localized-item';
import { ParkReferenceContactDetails } from './park-reference-contact-details';

export interface AttractionManufacturer {
  id?: string;
  name: string;
  legalName?: string | null;
  foundedYear?: number | null;
  closedYear?: number | null;
  contactDetails?: ParkReferenceContactDetails | null;
  biography?: LocalizedItem<string>[];
  currentLogoImageId?: string | null;
  isVisible?: boolean;
  attractionCount?: number;
  adminReviewStatus?: AdminReviewStatus;
}
