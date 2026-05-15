import { Injectable, Signal, computed, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, Observable } from 'rxjs';

import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { Park } from '@app/models/parks/park';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { ApiResponse } from '@app/models/shared/api_reponse';
import { Pagination } from '@app/models/shared/pagination';

interface AdminParkItemsIndexViewModel {
  rows: ParkItemAdminRow[];
  parkOptions: { label: string; value: string | null }[];
  totalRecords: number;
}

@Injectable()
export class AdminParkItemsIndexStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminParkItemsIndexViewModel>();
  private readonly allParksLabelSignal = signal('');
  private readonly selectedParkIdSignal = signal<string | null>(null);
  private readonly searchTermSignal = signal('');
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(20);

  public readonly state = this.screenStateStore.state;
  public readonly rows: Signal<ParkItemAdminRow[]> = computed(() => this.screenStateStore.data()?.rows ?? []);
  public readonly parkOptions: Signal<Array<{ label: string; value: string | null }>> = computed(() => this.screenStateStore.data()?.parkOptions ?? []);
  public readonly totalRecords = computed(() => this.screenStateStore.data()?.totalRecords ?? 0);
  public readonly selectedParkId: Signal<string | null> = this.selectedParkIdSignal.asReadonly();
  public readonly searchTerm: Signal<string> = this.searchTermSignal.asReadonly();
  public readonly pageSize: Signal<number> = this.pageSizeSignal.asReadonly();

  constructor(
    private readonly parkItemsApiService: ParkItemsApiService,
    private readonly parksApiService: ParksApiService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  initialize(allParksLabel: string): void {
    this.allParksLabelSignal.set(allParksLabel);
    this.loadData();
  }

  updateFilters(selectedParkId: string | null, searchTerm: string): void {
    this.selectedParkIdSignal.set(selectedParkId);
    this.searchTermSignal.set(searchTerm);
    this.currentPageSignal.set(1);
    this.loadData();
  }

  updatePage(event: { page?: number; rows?: number }): void {
    this.currentPageSignal.set((event.page ?? 0) + 1);
    this.pageSizeSignal.set(event.rows ?? this.pageSizeSignal());
    this.loadData();
  }

  private loadData(): void {
    const previousData: AdminParkItemsIndexViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      rowsResponse: this.parkItemsApiService.getParkItemsPaginated(
        this.currentPageSignal(),
        this.pageSizeSignal(),
        this.selectedParkIdSignal(),
        this.searchTermSignal()
      ),
      parks: this.getAllParks()
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ rowsResponse, parks }: { rowsResponse: ApiResponse<ParkItemAdminRow>; parks: Park[] }) => {
        const rows: ParkItemAdminRow[] = rowsResponse.data ?? [];
        const validParks: Park[] = parks.filter((park: Park) => !!park.id);
        const viewModel: AdminParkItemsIndexViewModel = {
          rows,
          totalRecords: rowsResponse.pagination?.totalItems ?? rows.length,
          parkOptions: [
            { label: this.allParksLabelSignal(), value: null },
            ...validParks.map((park: Park) => ({
              label: park.name ?? '',
              value: park.id ?? null
            }))
          ]
        };

        if (viewModel.totalRecords === 0) {
          this.screenStateStore.setEmpty(viewModel);
          return;
        }

        this.screenStateStore.setReady(viewModel);
      },
      error: (error: unknown) => {
        console.error('Error loading park items index', error);
        this.screenStateStore.setError('admin.parkItems.empty', previousData);
      }
    });
  }

  private getAllParks(): Observable<Park[]> {
    return new Observable<Park[]>((observer) => {
      this.parksApiService.getParksPaginated(1, 100).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (firstResponse: ParksApiResponse) => {
          const firstPageParks: Park[] = firstResponse.data ?? [];
          const pagination: Pagination | undefined = firstResponse.pagination;
          const totalPages: number = pagination?.totalPages ?? 1;

          if (totalPages <= 1) {
            observer.next(firstPageParks);
            observer.complete();
            return;
          }

          const requests: Observable<ParksApiResponse>[] = [];

          for (let currentPage: number = 2; currentPage <= totalPages; currentPage++) {
            requests.push(this.parksApiService.getParksPaginated(currentPage, 100));
          }

          forkJoin(requests).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: (responses: ParksApiResponse[]) => {
              const allParks: Park[] = [...firstPageParks];

              responses.forEach((response: ParksApiResponse) => {
                allParks.push(...(response.data ?? []));
              });

              observer.next(allParks);
              observer.complete();
            },
            error: (error: unknown) => {
              observer.error(error);
            }
          });
        },
        error: (error: unknown) => {
          observer.error(error);
        }
      });
    });
  }
}
