import { MapMarkerIconKind } from '@app/models/map/map-marker';

type LeafletNamespace = typeof import('leaflet');

interface LeafletMarkerIconDefinition {
  readonly label: string;
  readonly color: string;
  readonly symbol: string;
  readonly family: 'park' | 'location' | 'item';
}

const MarkerDefinitions: Record<MapMarkerIconKind, LeafletMarkerIconDefinition> = {
  park: {
    label: 'Parc',
    color: 'var(--c-orange)',
    symbol: '🎢',
    family: 'park'
  },
  site: {
    label: 'Site',
    color: 'var(--c-orange)',
    symbol: '🎢',
    family: 'park'
  },
  entrance: {
    label: 'Entrée',
    color: 'var(--c-lime)',
    symbol: '↪',
    family: 'location'
  },
  exit: {
    label: 'Sortie',
    color: 'var(--c-rose)',
    symbol: '↩',
    family: 'location'
  },
  fastPassEntrance: {
    label: 'Entrée Fast Pass',
    color: 'var(--c-sky)',
    symbol: '⚡',
    family: 'location'
  },
  accessibleEntrance: {
    label: 'Entrée PMR',
    color: 'var(--c-purple)',
    symbol: '♿',
    family: 'location'
  },
  attraction: {
    label: 'Attraction',
    color: 'var(--c-orange)',
    symbol: '🎡',
    family: 'item'
  },
  rollerCoaster: {
    label: 'Roller coaster',
    color: 'var(--c-orange)',
    symbol: '🎢',
    family: 'item'
  },
  waterRide: {
    label: 'Attraction aquatique',
    color: 'var(--c-sky)',
    symbol: '🌊',
    family: 'item'
  },
  darkRide: {
    label: 'Dark ride',
    color: 'var(--c-purple)',
    symbol: '🌙',
    family: 'item'
  },
  flatRide: {
    label: 'Flat ride',
    color: 'var(--c-gold)',
    symbol: '🎠',
    family: 'item'
  },
  familyRide: {
    label: 'Attraction familiale',
    color: 'var(--c-lime)',
    symbol: '👨‍👩‍👧',
    family: 'item'
  },
  thrillRide: {
    label: 'Sensation forte',
    color: 'var(--c-rose)',
    symbol: '⚡',
    family: 'item'
  },
  transportRide: {
    label: 'Transport ride',
    color: 'var(--c-sky)',
    symbol: '🚝',
    family: 'item'
  },
  walkThrough: {
    label: 'Walkthrough',
    color: 'var(--c-purple)',
    symbol: '🚶',
    family: 'item'
  },
  playground: {
    label: 'Aire de jeux',
    color: 'var(--c-lime)',
    symbol: '🧸',
    family: 'item'
  },
  interactiveExperience: {
    label: 'Expérience interactive',
    color: 'var(--c-rose)',
    symbol: '🎯',
    family: 'item'
  },
  observationRide: {
    label: 'Point de vue',
    color: 'var(--c-sky)',
    symbol: '🔭',
    family: 'item'
  },
  animal: {
    label: 'Animal',
    color: 'var(--c-lime)',
    symbol: '🦁',
    family: 'item'
  },
  show: {
    label: 'Spectacle',
    color: 'var(--c-purple)',
    symbol: '🎭',
    family: 'item'
  },
  restaurant: {
    label: 'Restaurant',
    color: 'var(--c-gold)',
    symbol: '🍔',
    family: 'item'
  },
  snack: {
    label: 'Snack',
    color: 'var(--c-gold)',
    symbol: '🍦',
    family: 'item'
  },
  shop: {
    label: 'Boutique',
    color: 'var(--c-lime)',
    symbol: '🛍️',
    family: 'item'
  },
  hotel: {
    label: 'Hôtel',
    color: 'var(--c-sky)',
    symbol: '🏨',
    family: 'item'
  },
  game: {
    label: 'Jeu',
    color: 'var(--c-rose)',
    symbol: '🎯',
    family: 'item'
  },
  meetAndGreet: {
    label: 'Rencontre personnage',
    color: 'var(--c-rose)',
    symbol: '⭐',
    family: 'item'
  },
  transport: {
    label: 'Transport',
    color: 'var(--c-sky)',
    symbol: '🚝',
    family: 'item'
  },
  station: {
    label: 'Station',
    color: 'var(--c-sky)',
    symbol: '🚉',
    family: 'item'
  },
  photo: {
    label: 'Photo',
    color: 'var(--c-sky)',
    symbol: 'IMG',
    family: 'item'
  },
  toilets: {
    label: 'Toilettes',
    color: 'var(--app-text-muted)',
    symbol: '🚻',
    family: 'item'
  },
  firstAid: {
    label: 'Premiers secours',
    color: 'var(--c-rose)',
    symbol: '✚',
    family: 'item'
  },
  information: {
    label: 'Information',
    color: 'var(--c-sky)',
    symbol: 'i',
    family: 'item'
  },
  locker: {
    label: 'Consigne',
    color: 'var(--app-text-muted)',
    symbol: '🔐',
    family: 'item'
  },
  parking: {
    label: 'Parking',
    color: 'var(--app-text-muted)',
    symbol: 'P',
    family: 'item'
  },
  service: {
    label: 'Service',
    color: 'var(--app-text-muted)',
    symbol: '🛠️',
    family: 'item'
  },
  other: {
    label: 'Autre',
    color: 'var(--app-text-muted)',
    symbol: '•',
    family: 'item'
  }
};

const LocationMarkerSize: import('leaflet').PointExpression = [46, 58];
const LocationMarkerAnchor: import('leaflet').PointExpression = [23, 58];
const LocationPopupAnchor: import('leaflet').PointExpression = [0, -54];
const ItemMarkerSize: import('leaflet').PointExpression = [42, 52];
const ItemMarkerAnchor: import('leaflet').PointExpression = [21, 52];
const ItemPopupAnchor: import('leaflet').PointExpression = [0, -48];
const ClusterMarkerSize: import('leaflet').PointExpression = [52, 52];
const ClusterMarkerAnchor: import('leaflet').PointExpression = [26, 26];

export function createLeafletMarkerIcon(
  leaflet: LeafletNamespace,
  iconKind: MapMarkerIconKind | null | undefined
): import('leaflet').DivIcon {
  const resolvedIconKind: MapMarkerIconKind = iconKind ?? 'park';
  const definition: LeafletMarkerIconDefinition = MarkerDefinitions[resolvedIconKind] ?? MarkerDefinitions.other;
  const isItemMarker: boolean = definition.family === 'item';

  return leaflet.divIcon({
    className: `app-leaflet-marker app-leaflet-marker--${definition.family} app-leaflet-marker--${resolvedIconKind}`,
    html: buildMarkerHtml(resolvedIconKind, definition),
    iconSize: isItemMarker ? ItemMarkerSize : LocationMarkerSize,
    iconAnchor: isItemMarker ? ItemMarkerAnchor : LocationMarkerAnchor,
    popupAnchor: isItemMarker ? ItemPopupAnchor : LocationPopupAnchor,
  });
}

export function createLeafletMarkerClusterIcon(
  leaflet: LeafletNamespace,
  count: number
): import('leaflet').DivIcon {
  const normalizedCount: number = Math.max(2, count);
  const sizeClass: string = normalizedCount >= 100
    ? 'large'
    : normalizedCount >= 20
      ? 'medium'
      : 'small';

  return leaflet.divIcon({
    className: `app-leaflet-marker-cluster app-leaflet-marker-cluster--${sizeClass}`,
    html: `<span class="app-leaflet-marker-cluster__count">${normalizedCount}</span>`,
    iconSize: ClusterMarkerSize,
    iconAnchor: ClusterMarkerAnchor
  });
}

function buildMarkerHtml(iconKind: MapMarkerIconKind, definition: LeafletMarkerIconDefinition): string {
  const content: string = `<span class="app-leaflet-marker__symbol">${definition.symbol}</span>`;

  return `
    <div
      class="app-leaflet-marker__pin app-leaflet-marker__pin--${definition.family}"
      style="--marker-color: ${definition.color};"
      title="${definition.label}"
      aria-label="${definition.label}"
      data-marker-kind="${iconKind}"
    >
      <div class="app-leaflet-marker__head">
        ${content}
      </div>
      <div class="app-leaflet-marker__tip"></div>
    </div>
  `;
}
