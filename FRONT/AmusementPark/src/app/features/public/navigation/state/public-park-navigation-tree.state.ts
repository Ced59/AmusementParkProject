import { Injectable, Signal, signal } from '@angular/core';

import {
  PublicParkNavigationTreeItem,
  PublicParkNavigationTreeViewModel
} from '../models/public-park-navigation-tree.model';

@Injectable({
  providedIn: 'root'
})
export class PublicParkNavigationTreeState {
  private readonly treeSignal = signal<PublicParkNavigationTreeViewModel>({
    isAvailable: false,
    isLoading: false,
    items: []
  });

  readonly tree: Signal<PublicParkNavigationTreeViewModel> = this.treeSignal.asReadonly();

  private loadSequence: number = 0;

  markUnavailable(): void {
    this.loadSequence++;
    this.treeSignal.set({
      isAvailable: false,
      isLoading: false,
      items: []
    });
  }

  beginLoad(): number {
    const loadId: number = ++this.loadSequence;
    this.treeSignal.update((current: PublicParkNavigationTreeViewModel): PublicParkNavigationTreeViewModel => ({
      isAvailable: true,
      isLoading: true,
      items: current.items
    }));
    return loadId;
  }

  completeLoad(loadId: number, items: readonly PublicParkNavigationTreeItem[]): void {
    if (!this.isCurrent(loadId)) {
      return;
    }

    this.treeSignal.set({
      isAvailable: true,
      isLoading: false,
      items
    });
  }

  isCurrent(loadId: number): boolean {
    return loadId === this.loadSequence;
  }
}
