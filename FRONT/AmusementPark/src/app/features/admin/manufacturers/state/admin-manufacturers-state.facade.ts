import { Injectable, Signal, computed, signal } from '@angular/core';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

interface AdminManufacturersViewModel {
  manufacturers: AttractionManufacturer[];
  filteredManufacturers: AttractionManufacturer[];
  searchQuery: string;
}

@Injectable()
export class AdminManufacturersStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminManufacturersViewModel>();
  private readonly manufacturersSignal = signal<AttractionManufacturer[]>([]);
  private readonly searchQuerySignal = signal('');

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly searchQuery = this.searchQuerySignal.asReadonly();
  public readonly filteredManufacturers: Signal<AttractionManufacturer[]> = computed(() => this.screenStateStore.data()?.filteredManufacturers ?? []);
  public readonly totalCount = computed(() => this.filteredManufacturers().length);

  constructor(private readonly manufacturersApiService: ManufacturersApiService) {
  }

  loadManufacturers(): void {
    const previousData: AdminManufacturersViewModel | undefined = this.screenStateStore.data();

    this.screenStateStore.setLoading(previousData);

    this.manufacturersApiService.getAttractionManufacturers().subscribe({
      next: (manufacturers: AttractionManufacturer[]) => {
        this.manufacturersSignal.set(manufacturers);
        this.pushDerivedState();
      },
      error: (error: unknown) => {
        console.error('Error loading manufacturers', error);
        this.screenStateStore.setError('admin.manufacturers.loadError', previousData);
      }
    });
  }

  setSearchQuery(searchQuery: string): void {
    this.searchQuerySignal.set(searchQuery);
    this.pushDerivedState();
  }

  private pushDerivedState(): void {
    const manufacturers: AttractionManufacturer[] = this.manufacturersSignal();
    const normalizedQuery: string = this.searchQuerySignal().trim().toLowerCase();
    const filteredManufacturers: AttractionManufacturer[] = normalizedQuery.length === 0
      ? [...manufacturers]
      : manufacturers.filter((manufacturer: AttractionManufacturer) => manufacturer.name.toLowerCase().includes(normalizedQuery));

    const viewModel: AdminManufacturersViewModel = {
      manufacturers,
      filteredManufacturers,
      searchQuery: this.searchQuerySignal()
    };

    if (manufacturers.length === 0) {
      this.screenStateStore.setEmpty(viewModel);
      return;
    }

    if (filteredManufacturers.length === 0) {
      this.screenStateStore.setEmpty(viewModel);
      return;
    }

    this.screenStateStore.setReady(viewModel);
  }
}
