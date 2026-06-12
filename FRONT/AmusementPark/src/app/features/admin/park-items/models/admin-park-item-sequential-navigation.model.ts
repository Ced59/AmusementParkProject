export interface AdminParkItemSequentialNavigationState {
  readonly isLoading: boolean;
  readonly currentItemId: string | null;
  readonly currentPosition: number;
  readonly remainingItems: number;
  readonly totalItems: number;
  readonly previousItemId: string | null;
  readonly nextItemId: string | null;
}
