import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, forkJoin, of, timer } from 'rxjs';
import { catchError, filter, switchMap, takeWhile } from 'rxjs/operators';

import { ParkWeatherRun, ParkWeatherRunItem } from '@app/models/admin/park-weather/park-weather-admin.models';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { ADMIN_PARK_WEATHER_STATE_PORT, AdminParkWeatherStatePort } from './admin-park-weather-state-data.ports';

interface AdminParkWeatherViewModel {
  latestRun: ParkWeatherRun | null;
  failedItems: ParkWeatherRunItem[];
}

@Injectable()
export class AdminParkWeatherStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminParkWeatherViewModel>();
  private readonly startingManualRefreshSignal = signal(false);
  private readonly retryingFailedSignal = signal(false);
  private readonly refreshingParkIdsSignal = signal<ReadonlySet<string>>(new Set<string>());

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly latestRun: Signal<ParkWeatherRun | null> = computed(() => this.screenStateStore.data()?.latestRun ?? null);
  public readonly failedItems: Signal<ParkWeatherRunItem[]> = computed(() => this.screenStateStore.data()?.failedItems ?? []);
  public readonly startingManualRefresh = this.startingManualRefreshSignal.asReadonly();
  public readonly retryingFailed = this.retryingFailedSignal.asReadonly();
  public readonly refreshingParkIds = this.refreshingParkIdsSignal.asReadonly();
  public readonly isRunActive: Signal<boolean> = computed(() => this.isActiveStatus(this.latestRun()?.status));

  constructor(
    @Inject(ADMIN_PARK_WEATHER_STATE_PORT) private readonly dataPort: AdminParkWeatherStatePort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(): void {
    const previousData: AdminParkWeatherViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.loadData().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (viewModel: AdminParkWeatherViewModel) => {
        this.screenStateStore.setReady(viewModel);
        this.startPollingIfNeeded();
      },
      error: (error: unknown) => {
        console.error('Error loading park weather admin data', error);
        this.screenStateStore.setError('admin.parkWeather.loadError', previousData);
      }
    });
  }

  startManualRefresh(): void {
    const previousData: AdminParkWeatherViewModel | undefined = this.screenStateStore.data();
    this.startingManualRefreshSignal.set(true);

    this.dataPort.startManualRefresh().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (run: ParkWeatherRun) => {
        this.startingManualRefreshSignal.set(false);
        this.screenStateStore.setReady({ latestRun: run, failedItems: [] });
        this.startPollingIfNeeded();
      },
      error: (error: unknown) => {
        console.error('Error starting park weather refresh', error);
        this.startingManualRefreshSignal.set(false);
        this.screenStateStore.setError('admin.parkWeather.startError', previousData);
      }
    });
  }

  retryFailed(): void {
    const runId: string | null = this.latestRun()?.id ?? null;
    if (!runId) {
      return;
    }

    const previousData: AdminParkWeatherViewModel | undefined = this.screenStateStore.data();
    this.retryingFailedSignal.set(true);

    this.dataPort.retryFailedRun(runId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (run: ParkWeatherRun) => {
        this.retryingFailedSignal.set(false);
        this.screenStateStore.setReady({ latestRun: run, failedItems: [] });
        this.startPollingIfNeeded();
      },
      error: (error: unknown) => {
        console.error('Error retrying failed park weather run', error);
        this.retryingFailedSignal.set(false);
        this.screenStateStore.setError('admin.parkWeather.retryError', previousData);
      }
    });
  }

  refreshPark(parkId: string): void {
    if (!parkId?.trim()) {
      return;
    }

    const previousData: AdminParkWeatherViewModel | undefined = this.screenStateStore.data();
    this.setParkRefreshing(parkId, true);

    this.dataPort.refreshPark(parkId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (run: ParkWeatherRun) => {
        this.setParkRefreshing(parkId, false);
        this.screenStateStore.setReady({ latestRun: run, failedItems: [] });
        this.startPollingIfNeeded();
      },
      error: (error: unknown) => {
        console.error('Error refreshing park weather', error);
        this.setParkRefreshing(parkId, false);
        this.screenStateStore.setError('admin.parkWeather.retryError', previousData);
      }
    });
  }

  private loadData(): Observable<AdminParkWeatherViewModel> {
    return this.dataPort.getLatestRun().pipe(
      switchMap((latestRun: ParkWeatherRun | null): Observable<AdminParkWeatherViewModel> => {
        if (!latestRun) {
          return of({ latestRun: null, failedItems: [] });
        }

        return forkJoin({
          failedItems: this.dataPort.getRunItems(latestRun.id, 'Failed').pipe(catchError(() => of([] as ParkWeatherRunItem[])))
        }).pipe(
          switchMap((result: { failedItems: ParkWeatherRunItem[] }): Observable<AdminParkWeatherViewModel> => of({
            latestRun,
            failedItems: result.failedItems
          }))
        );
      })
    );
  }

  private startPollingIfNeeded(): void {
    if (!this.isActiveStatus(this.latestRun()?.status)) {
      return;
    }

    timer(2500, 2500).pipe(
      takeUntilDestroyed(this.destroyRef),
      takeWhile(() => this.isActiveStatus(this.latestRun()?.status), true),
      filter(() => this.isActiveStatus(this.latestRun()?.status)),
      switchMap(() => this.loadData())
    ).subscribe({
      next: (viewModel: AdminParkWeatherViewModel) => {
        this.screenStateStore.setReady(viewModel);
      },
      error: (error: unknown) => {
        console.error('Error polling park weather run', error);
      }
    });
  }

  private setParkRefreshing(parkId: string, isRefreshing: boolean): void {
    const nextIds: Set<string> = new Set<string>(this.refreshingParkIdsSignal());
    if (isRefreshing) {
      nextIds.add(parkId);
    } else {
      nextIds.delete(parkId);
    }

    this.refreshingParkIdsSignal.set(nextIds);
  }

  private isActiveStatus(status: string | null | undefined): boolean {
    return status === 'Queued' || status === 'Running';
  }
}
