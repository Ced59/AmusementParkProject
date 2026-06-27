export enum ParkPhotoCategory {
  Gallery = 'park-gallery',
  Entrance = 'park-entrance',
  Parking = 'park-parking',
  Transport = 'park-transport',
  Overview = 'park-overview',
  OfficialMap = 'park-map',
  Atmosphere = 'park-atmosphere',
  Event = 'park-event',
  Show = 'park-show',
  Halloween = 'park-halloween',
  Christmas = 'park-christmas',
  Easter = 'park-easter',
  Food = 'park-food',
  Shop = 'park-shop',
  Services = 'park-services',
  Hotel = 'park-hotel',
  Accessibility = 'park-accessibility',
  Safety = 'park-safety',
  Signage = 'park-signage'
}

export interface ParkPhotoCategoryDefinition {
  readonly slug: ParkPhotoCategory;
  readonly adminLabelKey: string;
  readonly publicLabelKey: string;
  readonly labelFr: string;
  readonly labelEn: string;
  readonly sortOrder: number;
  readonly matchTokens: readonly string[];
}

export const PARK_PHOTO_CATEGORY_FALLBACK: ParkPhotoCategoryDefinition = {
  slug: ParkPhotoCategory.Gallery,
  adminLabelKey: 'admin.parks.photos.categories.gallery',
  publicLabelKey: 'parks.photos.categories.gallery',
  labelFr: 'Galerie',
  labelEn: 'Gallery',
  sortOrder: 0,
  matchTokens: ['gallery']
};

export const PARK_PHOTO_CATEGORIES: ReadonlyArray<ParkPhotoCategoryDefinition> = [
  PARK_PHOTO_CATEGORY_FALLBACK,
  {
    slug: ParkPhotoCategory.Entrance,
    adminLabelKey: 'admin.parks.photos.categories.entrance',
    publicLabelKey: 'parks.photos.categories.entrance',
    labelFr: 'Entrée du parc',
    labelEn: 'Park entrance',
    sortOrder: 1,
    matchTokens: ['entrance']
  },
  {
    slug: ParkPhotoCategory.Parking,
    adminLabelKey: 'admin.parks.photos.categories.parking',
    publicLabelKey: 'parks.photos.categories.parking',
    labelFr: 'Parking / accès',
    labelEn: 'Parking / access',
    sortOrder: 2,
    matchTokens: ['parking', 'car-park', 'arrival']
  },
  {
    slug: ParkPhotoCategory.Transport,
    adminLabelKey: 'admin.parks.photos.categories.transport',
    publicLabelKey: 'parks.photos.categories.transport',
    labelFr: 'Transports / navettes',
    labelEn: 'Transport / shuttles',
    sortOrder: 3,
    matchTokens: ['transport', 'shuttle', 'bus', 'tram']
  },
  {
    slug: ParkPhotoCategory.Overview,
    adminLabelKey: 'admin.parks.photos.categories.overview',
    publicLabelKey: 'parks.photos.categories.overview',
    labelFr: 'Vue d’ensemble',
    labelEn: 'Overview',
    sortOrder: 4,
    matchTokens: ['overview']
  },
  {
    slug: ParkPhotoCategory.OfficialMap,
    adminLabelKey: 'admin.parks.photos.categories.map',
    publicLabelKey: 'parks.photos.categories.map',
    labelFr: 'Plan officiel',
    labelEn: 'Official park map',
    sortOrder: 5,
    matchTokens: ['park-map', 'official-map', 'map', 'plan']
  },
  {
    slug: ParkPhotoCategory.Atmosphere,
    adminLabelKey: 'admin.parks.photos.categories.atmosphere',
    publicLabelKey: 'parks.photos.categories.atmosphere',
    labelFr: 'Ambiance',
    labelEn: 'Atmosphere',
    sortOrder: 6,
    matchTokens: ['atmosphere', 'ambiance']
  },
  {
    slug: ParkPhotoCategory.Event,
    adminLabelKey: 'admin.parks.photos.categories.event',
    publicLabelKey: 'parks.photos.categories.event',
    labelFr: 'Événement',
    labelEn: 'Event',
    sortOrder: 7,
    matchTokens: ['event']
  },
  {
    slug: ParkPhotoCategory.Show,
    adminLabelKey: 'admin.parks.photos.categories.show',
    publicLabelKey: 'parks.photos.categories.show',
    labelFr: 'Spectacles',
    labelEn: 'Shows',
    sortOrder: 8,
    matchTokens: ['show', 'entertainment']
  },
  {
    slug: ParkPhotoCategory.Halloween,
    adminLabelKey: 'admin.parks.photos.categories.halloween',
    publicLabelKey: 'parks.photos.categories.halloween',
    labelFr: 'Halloween',
    labelEn: 'Halloween',
    sortOrder: 9,
    matchTokens: ['halloween']
  },
  {
    slug: ParkPhotoCategory.Christmas,
    adminLabelKey: 'admin.parks.photos.categories.christmas',
    publicLabelKey: 'parks.photos.categories.christmas',
    labelFr: 'Noël',
    labelEn: 'Christmas',
    sortOrder: 10,
    matchTokens: ['christmas', 'noel', 'noël']
  },
  {
    slug: ParkPhotoCategory.Easter,
    adminLabelKey: 'admin.parks.photos.categories.easter',
    publicLabelKey: 'parks.photos.categories.easter',
    labelFr: 'Pâques',
    labelEn: 'Easter',
    sortOrder: 11,
    matchTokens: ['easter', 'paques', 'pâques']
  },
  {
    slug: ParkPhotoCategory.Food,
    adminLabelKey: 'admin.parks.photos.categories.food',
    publicLabelKey: 'parks.photos.categories.food',
    labelFr: 'Restauration',
    labelEn: 'Food',
    sortOrder: 12,
    matchTokens: ['food']
  },
  {
    slug: ParkPhotoCategory.Shop,
    adminLabelKey: 'admin.parks.photos.categories.shop',
    publicLabelKey: 'parks.photos.categories.shop',
    labelFr: 'Boutiques',
    labelEn: 'Shops',
    sortOrder: 13,
    matchTokens: ['shop', 'store', 'retail']
  },
  {
    slug: ParkPhotoCategory.Services,
    adminLabelKey: 'admin.parks.photos.categories.services',
    publicLabelKey: 'parks.photos.categories.services',
    labelFr: 'Services',
    labelEn: 'Services',
    sortOrder: 14,
    matchTokens: ['service']
  },
  {
    slug: ParkPhotoCategory.Hotel,
    adminLabelKey: 'admin.parks.photos.categories.hotel',
    publicLabelKey: 'parks.photos.categories.hotel',
    labelFr: 'Hôtels / hébergements',
    labelEn: 'Hotels / accommodation',
    sortOrder: 15,
    matchTokens: ['hotel', 'accommodation', 'resort']
  },
  {
    slug: ParkPhotoCategory.Accessibility,
    adminLabelKey: 'admin.parks.photos.categories.accessibility',
    publicLabelKey: 'parks.photos.categories.accessibility',
    labelFr: 'Accessibilité',
    labelEn: 'Accessibility',
    sortOrder: 16,
    matchTokens: ['accessibility', 'accessible']
  },
  {
    slug: ParkPhotoCategory.Safety,
    adminLabelKey: 'admin.parks.photos.categories.safety',
    publicLabelKey: 'parks.photos.categories.safety',
    labelFr: 'Sécurité',
    labelEn: 'Safety',
    sortOrder: 17,
    matchTokens: ['safety', 'security']
  },
  {
    slug: ParkPhotoCategory.Signage,
    adminLabelKey: 'admin.parks.photos.categories.signage',
    publicLabelKey: 'parks.photos.categories.signage',
    labelFr: 'Signalétique',
    labelEn: 'Signage',
    sortOrder: 18,
    matchTokens: ['signage', 'info-sign', 'information-board', 'wayfinding']
  }
];

const PARK_PHOTO_CATEGORIES_BY_SLUG: ReadonlyMap<string, ParkPhotoCategoryDefinition> = new Map<string, ParkPhotoCategoryDefinition>(
  PARK_PHOTO_CATEGORIES.map((category: ParkPhotoCategoryDefinition) => [category.slug, category])
);

export function getParkPhotoCategoryBySlug(slug: string | null | undefined): ParkPhotoCategoryDefinition | null {
  const normalizedSlug: string = normalizeParkPhotoCategorySegment(slug);
  return PARK_PHOTO_CATEGORIES_BY_SLUG.get(normalizedSlug) ?? null;
}

export function resolveParkPhotoCategoryFromTagSlug(tagIdOrSlug: string | null | undefined): ParkPhotoCategoryDefinition {
  const normalizedTag: string = normalizeParkPhotoCategorySegment(tagIdOrSlug);
  const directCategory: ParkPhotoCategoryDefinition | null = getParkPhotoCategoryBySlug(normalizedTag);

  if (directCategory) {
    return directCategory;
  }

  return PARK_PHOTO_CATEGORIES.find((category: ParkPhotoCategoryDefinition) => {
    return category.matchTokens.some((token: string) => normalizedTag.includes(token));
  }) ?? PARK_PHOTO_CATEGORY_FALLBACK;
}

function normalizeParkPhotoCategorySegment(value: string | null | undefined): string {
  return value?.trim().toLowerCase() ?? '';
}
