import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { PUBLIC_MANUFACTURERS_PAGE_SIZE } from '@shared/utils/routing/public-directory-location';
import { buildPublicParkReferenceRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';

import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { PagedResult, PaginationContract } from '@shared/models/contracts';
import { PUBLIC_MANUFACTURERS_PORT, PublicManufacturersPort } from './public-manufacturers-state-data.ports';

export interface PublicManufacturerGroup {
  readonly letter: string;
  readonly manufacturers: readonly AttractionManufacturer[];
}

@Injectable()
export class PublicManufacturersStateFacade {
  public static readonly DefaultPageSize: number = PUBLIC_MANUFACTURERS_PAGE_SIZE;

  private readonly manufacturersSignal = signal<AttractionManufacturer[]>([]);
  private readonly paginationSignal = signal<PaginationContract | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly searchTermSignal = signal<string>('');
  private readonly currentPageSignal = signal<number>(1);
  private readonly pageSizeSignal = signal<number>(PublicManufacturersStateFacade.DefaultPageSize);
  private loadSequence: number = 0;
  private readonly resolvedPageSignal = signal<number | null>(null);
  public readonly resolvedPage = this.resolvedPageSignal.asReadonly();

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
    private readonly httpStatus: SsrHttpStatusService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(page: number = this.currentPageSignal(), size: number = this.pageSizeSignal()): void {
    if (!Number.isSafeInteger(page) || page < 1 || !Number.isSafeInteger(size) || size < 1) {
      this.rejectInvalidPage();
      return;
    }
    const standardPage: boolean = size === PUBLIC_MANUFACTURERS_PAGE_SIZE && !this.searchTermSignal();
    const safePage: number = page;
    const safeSize: number = Math.max(size, 1);
    const sequence: number = this.loadSequence + 1;
    this.loadSequence = sequence;
    this.currentPageSignal.set(safePage);
    this.pageSizeSignal.set(safeSize);
    this.resolvedPageSignal.set(null);
    this.manufacturersSignal.set([]);
    this.paginationSignal.set(null);
    this.loadingSignal.set(true);
    this.errorKeySignal.set(null);

    this.manufacturersPort.getAttractionManufacturersPage(safePage, safeSize, this.searchTermSignal())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (pageResult: PagedResult<AttractionManufacturer>): void => {
          if (sequence !== this.loadSequence) {
            return;
          }

          if (standardPage) {
            const pagination = pageResult.pagination;
            if (pagination.currentPage !== page || pagination.itemsPerPage !== size
              || !Number.isSafeInteger(pagination.totalItems) || pagination.totalItems < 0
              || pagination.totalPages !== Math.ceil(pagination.totalItems / size)
              || pageResult.items.length > size) {
              this.failUnavailable();
              return;
            }
            if (page > Math.max(1, pagination.totalPages) || pageResult.items.length === 0 && pagination.totalItems === 0) {
              this.rejectInvalidPage();
              return;
            }
            if (pageResult.items.length !== Math.min(size, pagination.totalItems - (page - 1) * size)
              || pageResult.items.some(manufacturer => !buildPublicParkReferenceRouteCommands({
                language: 'en', kind: 'manufacturer', referenceId: manufacturer.id, referenceName: manufacturer.name
              }))) {
              this.failUnavailable();
              return;
            }
            this.resolvedPageSignal.set(page);
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
          this.failUnavailable();
        }
      });
  }

  rejectInvalidPage(): void {
    ++this.loadSequence;
    this.clearFailedPage();
    this.httpStatus.setNotFound();
  }

  private failUnavailable(): void {
    this.clearFailedPage();
    this.httpStatus.setStatus(503);
  }

  private clearFailedPage(): void {
    this.resolvedPageSignal.set(null);
    this.manufacturersSignal.set([]);
    this.paginationSignal.set(null);
    this.loadingSignal.set(false);
    this.errorKeySignal.set('manufacturersPage.error');
  }

  updateSearchTerm(value: string): void {
    ++this.loadSequence;
    this.resolvedPageSignal.set(null);
    this.searchTermSignal.set(value.trim());
  }

  clearSearch(): void {
    this.updateSearchTerm('');
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
