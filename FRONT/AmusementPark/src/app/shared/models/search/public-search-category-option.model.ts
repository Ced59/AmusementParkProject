export interface PublicSearchCategoryOption {
  labelKey: string;
  value: string;
}

export type PublicPlaceDiscoveryScope = 'parks' | 'parksAndStandaloneAttractions' | 'standaloneAttractions';

export const PUBLIC_SEARCH_CATEGORY_OPTIONS: readonly PublicSearchCategoryOption[] = [
  { labelKey: 'home.categories.everywhere', value: '' },
  { labelKey: 'home.categories.park', value: 'park' },
  { labelKey: 'home.categories.parkItems', value: 'parkItems' },
  { labelKey: 'home.categories.attractionsWithStandalone', value: 'attractionsWithStandalone' },
  { labelKey: 'home.categories.standaloneAttractions', value: 'standaloneAttractions' },
  { labelKey: 'home.categories.operators', value: 'operators' },
  { labelKey: 'home.categories.manufacturers', value: 'manufacturers' }
];

export const PUBLIC_PLACE_DISCOVERY_SCOPE_OPTIONS: readonly PublicSearchCategoryOption[] = [
  { labelKey: 'home.categories.park', value: 'parks' },
  { labelKey: 'parks.discoveryScopes.parksAndStandaloneAttractions', value: 'parksAndStandaloneAttractions' },
  { labelKey: 'home.categories.standaloneAttractions', value: 'standaloneAttractions' }
];
