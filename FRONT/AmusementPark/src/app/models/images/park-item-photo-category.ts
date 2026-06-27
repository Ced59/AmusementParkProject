export enum ParkItemPhotoCategory {
  Gallery = 'park-item-gallery',
  Entrance = 'park-item-entrance',
  Exit = 'park-item-exit',
  Facade = 'park-item-facade',
  Signage = 'park-item-signage',
  RestrictionSign = 'park-item-restriction-sign',
  WaitTimeSign = 'park-item-wait-time-sign',
  AccessibilitySign = 'park-item-accessibility-sign',
  Queue = 'park-item-queue',
  Station = 'park-item-station',
  Vehicle = 'park-item-vehicle',
  Theming = 'park-item-theming',
  Layout = 'park-item-layout',
  Interior = 'park-item-interior',
  Seating = 'park-item-seating',
  Menu = 'park-item-menu',
  FoodDrink = 'park-item-food-drink',
  Product = 'park-item-product',
  ShopFront = 'park-item-shop-front',
  ScheduleSign = 'park-item-schedule-sign',
  ServicePoint = 'park-item-service-point',
  Safety = 'park-item-safety'
}

export type ParkItemPhotoCategoryPublicKey =
  | 'gallery'
  | 'entrance'
  | 'exit'
  | 'facade'
  | 'signage'
  | 'restrictionSign'
  | 'waitTimeSign'
  | 'accessibilitySign'
  | 'queue'
  | 'station'
  | 'vehicle'
  | 'theming'
  | 'layout'
  | 'interior'
  | 'seating'
  | 'menu'
  | 'foodDrink'
  | 'product'
  | 'shopFront'
  | 'scheduleSign'
  | 'servicePoint'
  | 'safety';

export interface ParkItemPhotoCategoryDefinition {
  readonly slug: ParkItemPhotoCategory;
  readonly publicKey: ParkItemPhotoCategoryPublicKey;
  readonly adminLabelKey: string;
  readonly publicLabelKey: string;
  readonly labelFr: string;
  readonly labelEn: string;
  readonly sortOrder: number;
  readonly matchTokens: readonly string[];
}

export const PARK_ITEM_PHOTO_CATEGORY_FALLBACK: ParkItemPhotoCategoryDefinition = {
  slug: ParkItemPhotoCategory.Gallery,
  publicKey: 'gallery',
  adminLabelKey: 'admin.parks.items.photos.categories.gallery',
  publicLabelKey: 'parkItems.photos.categories.gallery',
  labelFr: 'Galerie générale',
  labelEn: 'General gallery',
  sortOrder: 0,
  matchTokens: ['gallery']
};

export const PARK_ITEM_PHOTO_CATEGORIES: ReadonlyArray<ParkItemPhotoCategoryDefinition> = [
  PARK_ITEM_PHOTO_CATEGORY_FALLBACK,
  {
    slug: ParkItemPhotoCategory.Entrance,
    publicKey: 'entrance',
    adminLabelKey: 'admin.parks.items.photos.categories.entrance',
    publicLabelKey: 'parkItems.photos.categories.entrance',
    labelFr: 'Entrée de l’attraction',
    labelEn: 'Attraction entrance',
    sortOrder: 1,
    matchTokens: ['entrance']
  },
  {
    slug: ParkItemPhotoCategory.Exit,
    publicKey: 'exit',
    adminLabelKey: 'admin.parks.items.photos.categories.exit',
    publicLabelKey: 'parkItems.photos.categories.exit',
    labelFr: 'Sortie',
    labelEn: 'Exit',
    sortOrder: 2,
    matchTokens: ['exit']
  },
  {
    slug: ParkItemPhotoCategory.Facade,
    publicKey: 'facade',
    adminLabelKey: 'admin.parks.items.photos.categories.facade',
    publicLabelKey: 'parkItems.photos.categories.facade',
    labelFr: 'Façade / extérieur',
    labelEn: 'Facade / exterior',
    sortOrder: 3,
    matchTokens: ['facade', 'façade', 'exterior', 'external']
  },
  {
    slug: ParkItemPhotoCategory.Signage,
    publicKey: 'signage',
    adminLabelKey: 'admin.parks.items.photos.categories.signage',
    publicLabelKey: 'parkItems.photos.categories.signage',
    labelFr: 'Signalétique',
    labelEn: 'Signage',
    sortOrder: 4,
    matchTokens: ['signage', 'name-sign', 'info-sign', 'information-board', 'wayfinding']
  },
  {
    slug: ParkItemPhotoCategory.RestrictionSign,
    publicKey: 'restrictionSign',
    adminLabelKey: 'admin.parks.items.photos.categories.restrictionSign',
    publicLabelKey: 'parkItems.photos.categories.restrictionSign',
    labelFr: 'Panneau de restrictions',
    labelEn: 'Restriction sign',
    sortOrder: 5,
    matchTokens: ['restriction-sign', 'restriction']
  },
  {
    slug: ParkItemPhotoCategory.WaitTimeSign,
    publicKey: 'waitTimeSign',
    adminLabelKey: 'admin.parks.items.photos.categories.waitTimeSign',
    publicLabelKey: 'parkItems.photos.categories.waitTimeSign',
    labelFr: 'Panneau temps d’attente',
    labelEn: 'Wait-time sign',
    sortOrder: 6,
    matchTokens: ['wait-time-sign', 'wait-time', 'waittime']
  },
  {
    slug: ParkItemPhotoCategory.AccessibilitySign,
    publicKey: 'accessibilitySign',
    adminLabelKey: 'admin.parks.items.photos.categories.accessibilitySign',
    publicLabelKey: 'parkItems.photos.categories.accessibilitySign',
    labelFr: 'Accessibilité',
    labelEn: 'Accessibility',
    sortOrder: 7,
    matchTokens: ['accessibility-sign', 'accessibility']
  },
  {
    slug: ParkItemPhotoCategory.Queue,
    publicKey: 'queue',
    adminLabelKey: 'admin.parks.items.photos.categories.queue',
    publicLabelKey: 'parkItems.photos.categories.queue',
    labelFr: 'File d’attente',
    labelEn: 'Queue',
    sortOrder: 8,
    matchTokens: ['queue']
  },
  {
    slug: ParkItemPhotoCategory.Station,
    publicKey: 'station',
    adminLabelKey: 'admin.parks.items.photos.categories.station',
    publicLabelKey: 'parkItems.photos.categories.station',
    labelFr: 'Gare / embarquement',
    labelEn: 'Station / boarding',
    sortOrder: 9,
    matchTokens: ['station', 'boarding']
  },
  {
    slug: ParkItemPhotoCategory.Vehicle,
    publicKey: 'vehicle',
    adminLabelKey: 'admin.parks.items.photos.categories.vehicle',
    publicLabelKey: 'parkItems.photos.categories.vehicle',
    labelFr: 'Véhicule / train',
    labelEn: 'Vehicle / train',
    sortOrder: 10,
    matchTokens: ['vehicle', 'train']
  },
  {
    slug: ParkItemPhotoCategory.Theming,
    publicKey: 'theming',
    adminLabelKey: 'admin.parks.items.photos.categories.theming',
    publicLabelKey: 'parkItems.photos.categories.theming',
    labelFr: 'Décors / ambiance',
    labelEn: 'Theming',
    sortOrder: 11,
    matchTokens: ['theming', 'theme']
  },
  {
    slug: ParkItemPhotoCategory.Layout,
    publicKey: 'layout',
    adminLabelKey: 'admin.parks.items.photos.categories.layout',
    publicLabelKey: 'parkItems.photos.categories.layout',
    labelFr: 'Vue d’ensemble',
    labelEn: 'Overview',
    sortOrder: 12,
    matchTokens: ['layout', 'overview']
  },
  {
    slug: ParkItemPhotoCategory.Interior,
    publicKey: 'interior',
    adminLabelKey: 'admin.parks.items.photos.categories.interior',
    publicLabelKey: 'parkItems.photos.categories.interior',
    labelFr: 'Intérieur',
    labelEn: 'Interior',
    sortOrder: 13,
    matchTokens: ['interior', 'inside', 'indoor']
  },
  {
    slug: ParkItemPhotoCategory.Seating,
    publicKey: 'seating',
    adminLabelKey: 'admin.parks.items.photos.categories.seating',
    publicLabelKey: 'parkItems.photos.categories.seating',
    labelFr: 'Assises / salle',
    labelEn: 'Seating / room',
    sortOrder: 14,
    matchTokens: ['seating', 'seat', 'room']
  },
  {
    slug: ParkItemPhotoCategory.Menu,
    publicKey: 'menu',
    adminLabelKey: 'admin.parks.items.photos.categories.menu',
    publicLabelKey: 'parkItems.photos.categories.menu',
    labelFr: 'Menu / tarifs',
    labelEn: 'Menu / prices',
    sortOrder: 15,
    matchTokens: ['menu']
  },
  {
    slug: ParkItemPhotoCategory.FoodDrink,
    publicKey: 'foodDrink',
    adminLabelKey: 'admin.parks.items.photos.categories.foodDrink',
    publicLabelKey: 'parkItems.photos.categories.foodDrink',
    labelFr: 'Plat / boisson',
    labelEn: 'Food / drink',
    sortOrder: 16,
    matchTokens: ['food-drink', 'food', 'drink']
  },
  {
    slug: ParkItemPhotoCategory.Product,
    publicKey: 'product',
    adminLabelKey: 'admin.parks.items.photos.categories.product',
    publicLabelKey: 'parkItems.photos.categories.product',
    labelFr: 'Produits / merchandising',
    labelEn: 'Products / merchandise',
    sortOrder: 17,
    matchTokens: ['product', 'merchandise', 'merch']
  },
  {
    slug: ParkItemPhotoCategory.ShopFront,
    publicKey: 'shopFront',
    adminLabelKey: 'admin.parks.items.photos.categories.shopFront',
    publicLabelKey: 'parkItems.photos.categories.shopFront',
    labelFr: 'Façade boutique / restaurant',
    labelEn: 'Shop / restaurant frontage',
    sortOrder: 18,
    matchTokens: ['shop-front', 'frontage']
  },
  {
    slug: ParkItemPhotoCategory.ScheduleSign,
    publicKey: 'scheduleSign',
    adminLabelKey: 'admin.parks.items.photos.categories.scheduleSign',
    publicLabelKey: 'parkItems.photos.categories.scheduleSign',
    labelFr: 'Horaires / programme',
    labelEn: 'Schedule / programme',
    sortOrder: 19,
    matchTokens: ['schedule-sign', 'schedule', 'programme', 'program']
  },
  {
    slug: ParkItemPhotoCategory.ServicePoint,
    publicKey: 'servicePoint',
    adminLabelKey: 'admin.parks.items.photos.categories.servicePoint',
    publicLabelKey: 'parkItems.photos.categories.servicePoint',
    labelFr: 'Point de service',
    labelEn: 'Service point',
    sortOrder: 20,
    matchTokens: ['service-point', 'counter', 'kiosk']
  },
  {
    slug: ParkItemPhotoCategory.Safety,
    publicKey: 'safety',
    adminLabelKey: 'admin.parks.items.photos.categories.safety',
    publicLabelKey: 'parkItems.photos.categories.safety',
    labelFr: 'Sécurité / consignes',
    labelEn: 'Safety instructions',
    sortOrder: 21,
    matchTokens: ['safety']
  }
];

const PARK_ITEM_PHOTO_CATEGORIES_BY_SLUG: ReadonlyMap<string, ParkItemPhotoCategoryDefinition> = new Map<string, ParkItemPhotoCategoryDefinition>(
  PARK_ITEM_PHOTO_CATEGORIES.map((category: ParkItemPhotoCategoryDefinition) => [category.slug, category])
);

const PARK_ITEM_PHOTO_CATEGORIES_BY_PUBLIC_KEY: ReadonlyMap<string, ParkItemPhotoCategoryDefinition> = new Map<string, ParkItemPhotoCategoryDefinition>(
  PARK_ITEM_PHOTO_CATEGORIES.map((category: ParkItemPhotoCategoryDefinition) => [normalizeParkItemPhotoCategorySegment(category.publicKey), category])
);

export function getParkItemPhotoCategoryBySlug(slug: string | null | undefined): ParkItemPhotoCategoryDefinition | null {
  const normalizedSlug: string = normalizeParkItemPhotoCategorySegment(slug);
  return PARK_ITEM_PHOTO_CATEGORIES_BY_SLUG.get(normalizedSlug) ?? null;
}

export function getParkItemPhotoCategoryByPublicKey(publicKey: string | null | undefined): ParkItemPhotoCategoryDefinition | null {
  const normalizedPublicKey: string = normalizeParkItemPhotoCategorySegment(publicKey);
  return PARK_ITEM_PHOTO_CATEGORIES_BY_PUBLIC_KEY.get(normalizedPublicKey) ?? null;
}

export function resolveParkItemPhotoCategoryFromTagSlug(tagIdOrSlug: string | null | undefined): ParkItemPhotoCategoryDefinition {
  const normalizedTag: string = normalizeParkItemPhotoCategorySegment(tagIdOrSlug);
  const directCategory: ParkItemPhotoCategoryDefinition | null = getParkItemPhotoCategoryBySlug(normalizedTag);

  if (directCategory) {
    return directCategory;
  }

  return PARK_ITEM_PHOTO_CATEGORIES.find((category: ParkItemPhotoCategoryDefinition) => {
    return category.matchTokens.some((token: string) => normalizedTag.includes(token));
  }) ?? PARK_ITEM_PHOTO_CATEGORY_FALLBACK;
}

function normalizeParkItemPhotoCategorySegment(value: string | null | undefined): string {
  return value?.trim().toLowerCase() ?? '';
}
