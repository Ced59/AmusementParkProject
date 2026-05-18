import { MapMarkerIconKind } from '@app/models/map/map-marker';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';

export type ParkItemMarkerSource = {
  readonly category?: ParkItemCategory | string | null;
  readonly type?: ParkItemType | string | null;
  readonly subtype?: string | null;
};

export function resolveParkItemMarkerIconKind(source: ParkItemMarkerSource): MapMarkerIconKind {
  const normalizedSubtype: string = normalizeMarkerSegment(source.subtype);
  const subtypeIconKind: MapMarkerIconKind | null = resolveSubtypeIconKind(normalizedSubtype);

  if (subtypeIconKind) {
    return subtypeIconKind;
  }

  const normalizedType: string = normalizeMarkerSegment(source.type);
  const typeIconKind: MapMarkerIconKind | null = resolveTypeIconKind(normalizedType);

  if (typeIconKind) {
    return typeIconKind;
  }

  const normalizedCategory: string = normalizeMarkerSegment(source.category);
  const categoryIconKind: MapMarkerIconKind | null = resolveCategoryIconKind(normalizedCategory);

  return categoryIconKind ?? 'other';
}

export function resolveLocationMarkerIconKind(locationKey: string | null | undefined): MapMarkerIconKind {
  switch (locationKey) {
    case 'entrance':
      return 'entrance';
    case 'exit':
      return 'exit';
    case 'fastPassEntrance':
      return 'fastPassEntrance';
    case 'reducedMobilityEntrance':
    case 'accessibleEntrance':
      return 'accessibleEntrance';
    default:
      return 'attraction';
  }
}

function resolveSubtypeIconKind(normalizedSubtype: string): MapMarkerIconKind | null {
  if (!normalizedSubtype) {
    return null;
  }

  if (containsAny(normalizedSubtype, ['rollercoaster', 'coaster', 'wooden', 'steel', 'inverted', 'launched', 'mine train', 'hybrid'])) {
    return 'rollerCoaster';
  }

  if (containsAny(normalizedSubtype, ['water', 'flume', 'splash', 'river rapids', 'log flume', 'boat ride', 'aquatic', 'aquatique'])) {
    return 'waterRide';
  }

  if (containsAny(normalizedSubtype, ['dark ride', 'darkride', 'indoor', 'omnimover', 'ghost train', 'shooting dark'])) {
    return 'darkRide';
  }

  if (containsAny(normalizedSubtype, ['flat ride', 'flatride', 'carousel', 'ferris wheel', 'wheel', 'tower', 'drop tower', 'swing', 'pendulum', 'top spin'])) {
    return 'flatRide';
  }

  if (containsAny(normalizedSubtype, ['thrill', 'adrenaline', 'extreme', 'intense'])) {
    return 'thrillRide';
  }

  if (containsAny(normalizedSubtype, ['family', 'kids', 'child', 'children', 'junior', 'kid', 'famille'])) {
    return 'familyRide';
  }

  if (containsAny(normalizedSubtype, ['walkthrough', 'walk through', 'maze', 'labyrinth', 'labyrinthe'])) {
    return 'walkThrough';
  }

  if (containsAny(normalizedSubtype, ['playground', 'play area', 'aire de jeux'])) {
    return 'playground';
  }

  if (containsAny(normalizedSubtype, ['interactive', 'arcade', 'game', 'jeux'])) {
    return 'interactiveExperience';
  }

  if (containsAny(normalizedSubtype, ['observation', 'panoramic', 'panorama', 'view tower'])) {
    return 'observationRide';
  }

  if (containsAny(normalizedSubtype, ['animal', 'zoo', 'aquarium', 'farm', 'ferme'])) {
    return 'animal';
  }

  if (containsAny(normalizedSubtype, ['show', 'spectacle', 'theater', 'theatre', 'cinema', 'parade'])) {
    return 'show';
  }

  if (containsAny(normalizedSubtype, ['restaurant', 'buffet', 'table service', 'food court'])) {
    return 'restaurant';
  }

  if (containsAny(normalizedSubtype, ['snack', 'kiosk', 'bar', 'cafe', 'coffee', 'ice cream', 'glace'])) {
    return 'snack';
  }

  if (containsAny(normalizedSubtype, ['shop', 'store', 'boutique', 'gift', 'souvenir'])) {
    return 'shop';
  }

  if (containsAny(normalizedSubtype, ['hotel', 'resort', 'lodging', 'hebergement'])) {
    return 'hotel';
  }

  if (containsAny(normalizedSubtype, ['meet', 'greet', 'character', 'personnage'])) {
    return 'meetAndGreet';
  }

  if (containsAny(normalizedSubtype, ['toilet', 'restroom', 'wc', 'bathroom', 'sanitaire'])) {
    return 'toilets';
  }

  if (containsAny(normalizedSubtype, ['first aid', 'medical', 'secours', 'infirmerie'])) {
    return 'firstAid';
  }

  if (containsAny(normalizedSubtype, ['information', 'guest service', 'reception'])) {
    return 'information';
  }

  if (containsAny(normalizedSubtype, ['locker', 'consigne'])) {
    return 'locker';
  }

  if (containsAny(normalizedSubtype, ['parking', 'car park'])) {
    return 'parking';
  }

  if (containsAny(normalizedSubtype, ['station', 'gare'])) {
    return 'station';
  }

  if (containsAny(normalizedSubtype, ['transport', 'train', 'monorail', 'tram', 'shuttle', 'navette'])) {
    return 'transport';
  }

  return null;
}

function resolveTypeIconKind(normalizedType: string): MapMarkerIconKind | null {
  switch (normalizedType) {
    case 'rollercoaster':
      return 'rollerCoaster';
    case 'waterride':
      return 'waterRide';
    case 'darkride':
      return 'darkRide';
    case 'flatride':
      return 'flatRide';
    case 'familyride':
      return 'familyRide';
    case 'thrillride':
      return 'thrillRide';
    case 'transportride':
      return 'transportRide';
    case 'walkthrough':
      return 'walkThrough';
    case 'playground':
      return 'playground';
    case 'interactiveexperience':
      return 'interactiveExperience';
    case 'observationride':
      return 'observationRide';
    case 'animalexhibit':
      return 'animal';
    case 'restaurant':
      return 'restaurant';
    case 'snack':
      return 'snack';
    case 'hotel':
      return 'hotel';
    case 'show':
      return 'show';
    case 'shop':
      return 'shop';
    case 'service':
      return 'service';
    case 'transport':
      return 'transport';
    case 'attraction':
      return 'attraction';
    case 'other':
      return 'other';
    default:
      return null;
  }
}

function resolveCategoryIconKind(normalizedCategory: string): MapMarkerIconKind | null {
  switch (normalizedCategory) {
    case 'attraction':
      return 'attraction';
    case 'restaurant':
      return 'restaurant';
    case 'hotel':
      return 'hotel';
    case 'animal':
      return 'animal';
    case 'show':
      return 'show';
    case 'shop':
      return 'shop';
    case 'service':
      return 'service';
    case 'transport':
      return 'transport';
    case 'other':
      return 'other';
    default:
      return null;
  }
}

function normalizeMarkerSegment(value: string | null | undefined): string {
  return (value ?? '')
    .trim()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[_-]+/g, ' ')
    .replace(/\s+/g, ' ')
    .toLowerCase();
}

function containsAny(source: string, candidates: readonly string[]): boolean {
  return candidates.some((candidate: string) => source.includes(candidate));
}
