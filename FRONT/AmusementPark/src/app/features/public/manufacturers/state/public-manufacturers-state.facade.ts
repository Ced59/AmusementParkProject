import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { PUBLIC_MANUFACTURERS_PORT, PublicManufacturersPort } from './public-manufacturers-state-data.ports';

export interface PublicManufacturerGroup {
  readonly letter: string;
  readonly manufacturers: readonly AttractionManufacturer[];
}

@Injectable()
export class PublicManufacturersStateFacade {
  private readonly manufacturersSignal = signal<AttractionManufacturer[]>([]);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly searchTermSignal = signal<string>('');

  public readonly manufacturers: Signal<AttractionManufacturer[]> = this.manufacturersSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  public readonly searchTerm: Signal<string> = this.searchTermSignal.asReadonly();
  public readonly filteredManufacturers: Signal<AttractionManufacturer[]> = computed(() => {
    const searchToken: string = normalizeSearchValue(this.searchTermSignal());
    const sortedManufacturers: AttractionManufacturer[] = [...this.manufacturersSignal()]
      .sort((left: AttractionManufacturer, right: AttractionManufacturer) => left.name.localeCompare(right.name));

    if (!searchToken) {
      return sortedManufacturers;
    }

    return sortedManufacturers.filter((manufacturer: AttractionManufacturer) => this.matchesSearch(manufacturer, searchToken));
  });
  public readonly groupedManufacturers: Signal<PublicManufacturerGroup[]> = computed(() => {
    const groups = new Map<string, AttractionManufacturer[]>();

    for (const manufacturer of this.filteredManufacturers()) {
      const letter: string = resolveGroupLetter(manufacturer.name);
      const group: AttractionManufacturer[] = groups.get(letter) ?? [];
      group.push(manufacturer);
      groups.set(letter, group);
    }

    return Array.from(groups.entries()).map(([letter, manufacturers]: [string, AttractionManufacturer[]]) => ({
      letter,
      manufacturers
    }));
  });

  constructor(
    @Inject(PUBLIC_MANUFACTURERS_PORT) private readonly manufacturersPort: PublicManufacturersPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(): void {
    this.loadingSignal.set(true);
    this.errorKeySignal.set(null);

    this.manufacturersPort.getAllAttractionManufacturers().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (manufacturers: AttractionManufacturer[]): void => {
        this.manufacturersSignal.set(manufacturers);
        this.loadingSignal.set(false);
      },
      error: (error: unknown): void => {
        console.error('Error loading attraction manufacturers', error);
        this.manufacturersSignal.set([]);
        this.loadingSignal.set(false);
        this.errorKeySignal.set('manufacturersPage.error');
      }
    });
  }

  updateSearchTerm(value: string): void {
    this.searchTermSignal.set(value.trim());
  }

  clearSearch(): void {
    this.searchTermSignal.set('');
  }

  private matchesSearch(manufacturer: AttractionManufacturer, searchToken: string): boolean {
    const searchableText: string = [
      manufacturer.name,
      manufacturer.legalName,
      manufacturer.foundedYear?.toString(),
      manufacturer.closedYear?.toString(),
      manufacturer.contactDetails?.city,
      manufacturer.contactDetails?.countryCode
    ]
      .filter((value: string | null | undefined): value is string => Boolean(value))
      .map((value: string) => normalizeSearchValue(value))
      .join(' ');

    return searchableText.includes(searchToken);
  }
}

function resolveGroupLetter(name: string): string {
  const normalizedName: string = name.trim();
  if (!normalizedName) {
    return '#';
  }

  const letter: string = normalizedName.charAt(0).toUpperCase();
  return /[A-Z]/.test(letter) ? letter : '#';
}

function normalizeSearchValue(value: string | null | undefined): string {
  return (value ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, ' ')
    .trim()
    .replace(/\s+/g, ' ');
}
