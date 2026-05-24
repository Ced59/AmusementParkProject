import { Params } from '@angular/router';

export interface PublicParkNavigationTreeItem {
  readonly id: string;
  readonly label: string;
  readonly icon: string;
  readonly routeCommands: string[];
  readonly queryParams?: Params;
  readonly level: number;
  readonly isCurrent: boolean;
}

export interface PublicParkNavigationTreeViewModel {
  readonly isAvailable: boolean;
  readonly isLoading: boolean;
  readonly items: readonly PublicParkNavigationTreeItem[];
}
