import { Component, OnInit, signal } from '@angular/core';
import { Pagination } from '../../../../models/shared/pagination';
import { UserDto } from '../../../../models/users/user_dto';
import { UsersApiResponse } from '../../../../models/users/users_api_response';
import { ViewState } from '../../../../models/shared/view-state';
import { ApiService } from '../../../../services/api.service';

@Component({
  selector: 'app-admin-users',
  templateUrl: './admin-users.component.html',
  styleUrls: ['./admin-users.component.scss']
})
export class AdminUsersComponent implements OnInit {
  readonly users = signal<UserDto[]>([]);
  readonly pagination = signal<Pagination | null>(null);
  readonly viewState = signal<ViewState>(ViewState.Loading);

  totalRecords = 0;
  pageSize = 10;
  currentPage = 1;

  constructor(private readonly apiService: ApiService) {
  }

  ngOnInit(): void {
    this.loadUsers(this.currentPage, this.pageSize);
  }

  onPageChanged(event: any): void {
    const rows = event.rows ?? this.pageSize;
    const first = event.first ?? 0;
    const page = Math.floor(first / rows) + 1;

    this.loadUsers(page, rows);
  }

  private loadUsers(page: number, size: number): void {
    this.viewState.set(ViewState.Loading);

    this.apiService.getUsers(page, size).subscribe({
      next: (response: UsersApiResponse) => {
        const users = response.data ?? [];
        const pagination = response.pagination ?? null;

        this.users.set(users);
        this.pagination.set(pagination);
        this.totalRecords = pagination?.totalItems ?? users.length;
        this.pageSize = pagination?.itemsPerPage ?? size;
        this.currentPage = pagination?.currentPage ?? page;
        this.viewState.set(ViewState.Ready);
      },
      error: (err: unknown) => {
        console.error('Error loading users', err);
        this.viewState.set(ViewState.Error);
      }
    });
  }
}
