import { Injectable, Signal, computed, signal } from '@angular/core';
import { Pagination } from '../../../../models/shared/pagination';
import { UserDto } from '../../../../models/users/user_dto';
import { UsersApiResponse } from '../../../../models/users/users_api_response';
import { UsersApiService } from '@data-access/users/users-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

interface AdminUsersViewModel {
  users: UserDto[];
  pagination: Pagination | null;
  totalRecords: number;
  currentPage: number;
  pageSize: number;
}

@Injectable()
export class AdminUsersStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminUsersViewModel>();
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(10);

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly users: Signal<UserDto[]> = computed(() => this.screenStateStore.data()?.users ?? []);
  public readonly pagination: Signal<Pagination | null> = computed(() => this.screenStateStore.data()?.pagination ?? null);
  public readonly totalRecords = computed(() => this.screenStateStore.data()?.totalRecords ?? 0);
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();

  constructor(private readonly usersApiService: UsersApiService) {
  }

  loadUsers(page: number = this.currentPageSignal(), size: number = this.pageSizeSignal()): void {
    const previousData: AdminUsersViewModel | undefined = this.screenStateStore.data();

    this.currentPageSignal.set(page);
    this.pageSizeSignal.set(size);
    this.screenStateStore.setLoading(previousData);

    this.usersApiService.getUsers(page, size).subscribe({
      next: (response: UsersApiResponse) => {
        const users: UserDto[] = response.data ?? [];
        const pagination: Pagination | null = response.pagination ?? null;
        const viewModel: AdminUsersViewModel = {
          users,
          pagination,
          totalRecords: pagination?.totalItems ?? users.length,
          pageSize: pagination?.itemsPerPage ?? size,
          currentPage: pagination?.currentPage ?? page
        };

        this.currentPageSignal.set(viewModel.currentPage);
        this.pageSizeSignal.set(viewModel.pageSize);

        if (viewModel.totalRecords === 0) {
          this.screenStateStore.setEmpty(viewModel);
          return;
        }

        this.screenStateStore.setReady(viewModel);
      },
      error: (error: unknown) => {
        console.error('Error loading users', error);
        this.screenStateStore.setError('admin.users.loadError', previousData);
      }
    });
  }
}
