import { ParkType } from '@app/models/parks/park-type';

export interface AdminParkTypeOption {
  labelKey: string;
  value: ParkType;
}

export interface AdminParkCountryOption {
  code: string;
  label: string;
}


export interface AdminParkPhotoCategoryOption {
  slug: string;
  labelKey: string;
}

export const PARK_PHOTO_CATEGORY_OPTIONS: ReadonlyArray<AdminParkPhotoCategoryOption> = [
  { slug: 'park-gallery', labelKey: 'admin.parks.photos.categories.gallery' },
  { slug: 'park-entrance', labelKey: 'admin.parks.photos.categories.entrance' },
  { slug: 'park-overview', labelKey: 'admin.parks.photos.categories.overview' },
  { slug: 'park-map', labelKey: 'admin.parks.photos.categories.map' },
  { slug: 'park-atmosphere', labelKey: 'admin.parks.photos.categories.atmosphere' },
  { slug: 'park-event', labelKey: 'admin.parks.photos.categories.event' },
  { slug: 'park-halloween', labelKey: 'admin.parks.photos.categories.halloween' },
  { slug: 'park-christmas', labelKey: 'admin.parks.photos.categories.christmas' },
  { slug: 'park-easter', labelKey: 'admin.parks.photos.categories.easter' },
  { slug: 'park-food', labelKey: 'admin.parks.photos.categories.food' },
  { slug: 'park-services', labelKey: 'admin.parks.photos.categories.services' }
];
