import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';
import { NearbyDestination, NearbyDestinationKind } from '@app/models/parks/nearby-destination';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { MeasurementPreferenceService } from '@app/services/measurements/measurement-preference.service';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { mapNearbyDestinationToCard } from './nearby-destination-card.mapper';
import { NEARBY_DESTINATIONS_PORT } from './nearby-destinations.ports';

@Injectable()
export class NearbyDestinationsFacade {
  private readonly port = inject(NEARBY_DESTINATIONS_PORT);
  private readonly destroyRef = inject(DestroyRef);
  private readonly countries = inject(CountryDisplayService);
  private readonly truncator = inject(NaturalTextTruncatorService);
  private readonly preference = inject(MeasurementPreferenceService);
  private readonly measurements = inject(MeasurementConversionService);
  private readonly store = new SignalScreenStateStore<NearbyDestination[]>();
  private readonly language = signal('en');
  private request: Subscription | null = null;

  readonly state = this.store.state;
  readonly cards = computed(() => (this.store.data() ?? []).map(target => mapNearbyDestinationToCard(
    target, this.language(), this.countries, this.truncator, this.preference.preferredSystem(), this.measurements
  )));

  setLanguage(language: string): void {
    this.language.set(language || 'en');
  }

  load(kind: NearbyDestinationKind, id: string | null): void {
    this.request?.unsubscribe();
    const sourceId = id?.trim();
    if (!sourceId) {
      this.store.setEmpty([]);
      return;
    }
    this.store.setLoading();
    this.request = this.port.getNearby(kind, sourceId, 4, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: targets => {
          const nearby = targets.filter(target => {
            const targetId = target.park?.id ?? target.standaloneAttraction?.id;
            return !!targetId && (target.kind !== kind || targetId !== sourceId);
          }).slice(0, 4);
          if (nearby.length) {
            this.store.setReady(nearby);
          } else {
            this.store.setEmpty([]);
          }
        },
        error: () => this.store.setError('parks.detail.nearby.errorMessage', [])
      });
  }
}
