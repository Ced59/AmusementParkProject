export interface AdminParkItemSequentialNavigationState {
  readonly isLoading: boolean;
  readonly currentItemId: string | null;
  readonly currentPosition: number;
  readonly remainingItems: number;
  readonly totalItems: number;
  readonly previousItemId: string | null;
  readonly nextItemId: string | null;
}

export const EMPTY_ADMIN_PARK_ITEM_SEQUENTIAL_NAVIGATION_STATE: AdminParkItemSequentialNavigationState = {
  isLoading: false,
  currentItemId: null,
  currentPosition: 0,
  remainingItems: 0,
  totalItems: 0,
  previousItemId: null,
  nextItemId: null
};
