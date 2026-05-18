import { DestroyRef, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ParkFounder } from '@app/models/parks/park-founder';
import { ParkOperator } from '@app/models/parks/park-operator';
import { ParkFoundersApiService } from '@data-access/parks/park-founders-api.service';
import { ParkOperatorsApiService } from '@data-access/parks/park-operators-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { mapNullable } from '@shared/utils/mapping';
import {
  mapParkFounderToReferenceDetailViewModel,
  mapParkOperatorToReferenceDetailViewModel
} from '../mappers/park-reference-detail-view.mapper';
import { ParkReferenceDetailViewModel, ParkReferenceKind } from '../models/park-reference-detail-view.model';

interface ParkReferenceDetailSourceData {
  kind: ParkReferenceKind;
  founder: ParkFounder | null;
  operator: ParkOperator | null;
}

@Injectable()
export class ParkReferenceDetailStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkReferenceDetailSourceData>();
  private readonly currentLanguageSignal = signal('en');

  public readonly state = this.screenStateStore.state;
  public readonly reference: Signal<ParkReferenceDetailViewModel | null> = computed(() => {
    const sourceData: ParkReferenceDetailSourceData | undefined = this.screenStateStore.data();
    const currentLanguage: string = this.currentLanguageSignal();

    if (!sourceData) {
      return null;
    }

    if (sourceData.kind === 'founder') {
      return mapNullable(sourceData.founder, (founder: ParkFounder) => mapParkFounderToReferenceDetailViewModel(founder, currentLanguage));
    }

    return mapNullable(sourceData.operator, (operator: ParkOperator) => mapParkOperatorToReferenceDetailViewModel(operator, currentLanguage));
  });

  constructor(
    private readonly parkFoundersApiService: ParkFoundersApiService,
    private readonly parkOperatorsApiService: ParkOperatorsApiService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadReference(kind: ParkReferenceKind, id: string): void {
    if (kind === 'founder') {
      this.loadFounder(id);
      return;
    }

    this.loadOperator(id);
  }

  private loadFounder(id: string): void {
    const previousData: ParkReferenceDetailSourceData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.parkFoundersApiService.getParkFounderById(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (founder: ParkFounder) => {
        this.screenStateStore.setReady({
          kind: 'founder',
          founder,
          operator: null
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park founder', error);
        this.screenStateStore.setError('parks.reference.errorMessage', previousData);
      }
    });
  }

  private loadOperator(id: string): void {
    const previousData: ParkReferenceDetailSourceData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.parkOperatorsApiService.getParkOperatorById(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (operator: ParkOperator) => {
        this.screenStateStore.setReady({
          kind: 'operator',
          founder: null,
          operator
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park operator', error);
        this.screenStateStore.setError('parks.reference.errorMessage', previousData);
      }
    });
  }
}
