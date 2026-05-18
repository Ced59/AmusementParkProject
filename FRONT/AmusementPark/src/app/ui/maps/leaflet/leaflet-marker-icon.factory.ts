import { MapMarkerIconKind } from '@app/models/map/map-marker';

type LeafletNamespace = typeof import('leaflet');

interface LeafletMarkerIconDefinition {
  readonly label: string;
  readonly color: string;
  readonly symbol: string;
  readonly family: 'park' | 'location' | 'item';
  readonly useLogo?: boolean;
}

const MarkerDefinitions: Record<MapMarkerIconKind, LeafletMarkerIconDefinition> = {
  park: {
    label: 'Parc',
    color: '#FF5A1F',
    symbol: '🎢',
    family: 'park',
    useLogo: true
  },
  site: {
    label: 'Site',
    color: '#FF5A1F',
    symbol: '🎢',
    family: 'park',
    useLogo: true
  },
  entrance: {
    label: 'Entrée',
    color: '#C4FF27',
    symbol: '↪',
    family: 'location'
  },
  exit: {
    label: 'Sortie',
    color: '#FF4D8B',
    symbol: '↩',
    family: 'location'
  },
  fastPassEntrance: {
    label: 'Entrée Fast Pass',
    color: '#38C5F5',
    symbol: '⚡',
    family: 'location'
  },
  accessibleEntrance: {
    label: 'Entrée PMR',
    color: '#9B5DE5',
    symbol: '♿',
    family: 'location'
  },
  attraction: {
    label: 'Attraction',
    color: '#FF5A1F',
    symbol: '🎡',
    family: 'item'
  },
  rollerCoaster: {
    label: 'Roller coaster',
    color: '#FF5A1F',
    symbol: '🎢',
    family: 'item'
  },
  waterRide: {
    label: 'Attraction aquatique',
    color: '#38C5F5',
    symbol: '🌊',
    family: 'item'
  },
  darkRide: {
    label: 'Dark ride',
    color: '#9B5DE5',
    symbol: '🌙',
    family: 'item'
  },
  flatRide: {
    label: 'Flat ride',
    color: '#FFB627',
    symbol: '🎠',
    family: 'item'
  },
  familyRide: {
    label: 'Attraction familiale',
    color: '#C4FF27',
    symbol: '👨‍👩‍👧',
    family: 'item'
  },
  thrillRide: {
    label: 'Sensation forte',
    color: '#FF4D8B',
    symbol: '⚡',
    family: 'item'
  },
  transportRide: {
    label: 'Transport ride',
    color: '#38C5F5',
    symbol: '🚝',
    family: 'item'
  },
  walkThrough: {
    label: 'Walkthrough',
    color: '#9B5DE5',
    symbol: '🚶',
    family: 'item'
  },
  playground: {
    label: 'Aire de jeux',
    color: '#C4FF27',
    symbol: '🧸',
    family: 'item'
  },
  interactiveExperience: {
    label: 'Expérience interactive',
    color: '#FF4D8B',
    symbol: '🎯',
    family: 'item'
  },
  observationRide: {
    label: 'Point de vue',
    color: '#38C5F5',
    symbol: '🔭',
    family: 'item'
  },
  animal: {
    label: 'Animal',
    color: '#C4FF27',
    symbol: '🦁',
    family: 'item'
  },
  show: {
    label: 'Spectacle',
    color: '#9B5DE5',
    symbol: '🎭',
    family: 'item'
  },
  restaurant: {
    label: 'Restaurant',
    color: '#FFB627',
    symbol: '🍔',
    family: 'item'
  },
  snack: {
    label: 'Snack',
    color: '#FFB627',
    symbol: '🍦',
    family: 'item'
  },
  shop: {
    label: 'Boutique',
    color: '#C4FF27',
    symbol: '🛍️',
    family: 'item'
  },
  hotel: {
    label: 'Hôtel',
    color: '#38C5F5',
    symbol: '🏨',
    family: 'item'
  },
  game: {
    label: 'Jeu',
    color: '#FF4D8B',
    symbol: '🎯',
    family: 'item'
  },
  meetAndGreet: {
    label: 'Rencontre personnage',
    color: '#FF4D8B',
    symbol: '⭐',
    family: 'item'
  },
  transport: {
    label: 'Transport',
    color: '#38C5F5',
    symbol: '🚝',
    family: 'item'
  },
  station: {
    label: 'Station',
    color: '#38C5F5',
    symbol: '🚉',
    family: 'item'
  },
  toilets: {
    label: 'Toilettes',
    color: '#B09D7A',
    symbol: '🚻',
    family: 'item'
  },
  firstAid: {
    label: 'Premiers secours',
    color: '#FF4D8B',
    symbol: '✚',
    family: 'item'
  },
  information: {
    label: 'Information',
    color: '#38C5F5',
    symbol: 'i',
    family: 'item'
  },
  locker: {
    label: 'Consigne',
    color: '#B09D7A',
    symbol: '🔐',
    family: 'item'
  },
  parking: {
    label: 'Parking',
    color: '#B09D7A',
    symbol: 'P',
    family: 'item'
  },
  service: {
    label: 'Service',
    color: '#B09D7A',
    symbol: '🛠️',
    family: 'item'
  },
  other: {
    label: 'Autre',
    color: '#B09D7A',
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

function buildMarkerHtml(iconKind: MapMarkerIconKind, definition: LeafletMarkerIconDefinition): string {
  const content: string = definition.useLogo
    ? '<img class="app-leaflet-marker__logo" src="assets/general-icon/logo-amusementpark.png" alt="" loading="lazy">'
    : `<span class="app-leaflet-marker__symbol">${definition.symbol}</span>`;

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
