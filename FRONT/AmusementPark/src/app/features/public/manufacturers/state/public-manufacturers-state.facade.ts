import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { PagedResult, PaginationContract } from '@shared/models/contracts';
import { PUBLIC_MANUFACTURERS_PORT, PublicManufacturersPort } from './public-manufacturers-state-data.ports';

export interface PublicManufacturerGroup {
  readonly letter: string;
  readonly manufacturers: readonly AttractionManufacturer[];
}

@Injectable()
export class PublicManufacturersStateFacade {
  public static readonly DefaultPageSize: number = 24;

  private readonly manufacturersSignal = signal<AttractionManufacturer[]>([]);
  private readonly paginationSignal = signal<PaginationContract | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly searchTermSignal = signal<string>('');
  private readonly currentPageSignal = signal<number>(1);
  private readonly pageSizeSignal = signal<number>(PublicManufacturersStateFacade.DefaultPageSize);
  private loadSequence: number = 0;

  public readonly manufacturers: Signal<AttractionManufacturer[]> = this.manufacturersSignal.asReadonly();
  public readonly pagination: Signal<PaginationContract | null> = this.paginationSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  public readonly searchTerm: Signal<string> = this.searchTermSignal.asReadonly();
  public readonly currentPage: Signal<number> = this.currentPageSignal.asReadonly();
  public readonly pageSize: Signal<number> = this.pageSizeSignal.asReadonly();
  public readonly totalCount: Signal<number> = computed(() => this.paginationSignal()?.totalItems ?? this.manufacturersSignal().length);
  public readonly filteredManufacturers: Signal<AttractionManufacturer[]> = computed(() => {
    return [...this.manufacturersSignal()]
      .sort((left: AttractionManufacturer, right: AttractionManufacturer) => left.name.localeCompare(right.name));
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

  load(page: number = this.currentPageSignal(), size: number = this.pageSizeSignal()): void {
    const safePage: number = Math.max(page, 1);
    const safeSize: number = Math.max(size, 1);
    const sequence: number = this.loadSequence + 1;
    this.loadSequence = sequence;
    this.currentPageSignal.set(safePage);
    this.pageSizeSignal.set(safeSize);
    this.loadingSignal.set(true);
    this.errorKeySignal.set(null);

    this.manufacturersPort.getAttractionManufacturersPage(safePage, safeSize, this.searchTermSignal())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (pageResult: PagedResult<AttractionManufacturer>): void => {
          if (sequence !== this.loadSequence) {
            return;
          }

          this.manufacturersSignal.set(pageResult.items);
          this.paginationSignal.set(pageResult.pagination);
          this.currentPageSignal.set(pageResult.pagination.currentPage);
          this.pageSizeSignal.set(pageResult.pagination.itemsPerPage || safeSize);
          this.loadingSignal.set(false);
        },
        error: (error: unknown): void => {
          if (sequence !== this.loadSequence) {
            return;
          }

          console.error('Error loading attraction manufacturers', error);
          this.manufacturersSignal.set([]);
          this.paginationSignal.set(null);
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

  setPage(page: number, size: number): void {
    this.load(page, size);
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
