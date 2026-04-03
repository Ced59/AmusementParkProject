import { Component, OnInit } from '@angular/core';
import {Pagination} from "../../../../models/shared/pagination";
import {UserDto} from "../../../../models/users/user_dto";
import {ApiService} from "../../../../services/api.service";
import {UsersApiResponse} from "../../../../models/users/users_api_response";


@Component({
    selector: 'app-admin-users',
    templateUrl: './admin-users.component.html',
    styleUrls: ['./admin-users.component.scss'],
    standalone: false
})
export class AdminUsersComponent implements OnInit {

  users: UserDto[] = [];
  loading = false;

  pagination: Pagination | null = null;
  totalRecords = 0;
  pageSize = 10;
  currentPage = 1; // 1-based pour l'API

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.loadUsers(this.currentPage, this.pageSize);
  }

  loadUsers(page: number, size: number): void {
    this.loading = true;

    this.apiService.getUsers(page, size).subscribe({
      next: (response: UsersApiResponse) => {
        this.users = response.data ?? [];
        this.pagination = response.pagination ?? null;

        this.totalRecords = this.pagination?.totalItems ?? this.users.length;
        this.pageSize = this.pagination?.itemsPerPage ?? size;
        this.currentPage = this.pagination?.currentPage ?? page;

        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading users', err);
        this.loading = false;
      }
    });
  }

  // appelé par (onLazyLoad)
  onPageChanged(event: any): void {
    const rows = event.rows ?? this.pageSize;
    const first = event.first ?? 0;

    // first = index du premier élément (0-based) -> page 1-based pour l’API
    const page = Math.floor(first / rows) + 1;

    // console.log('Lazy load users => page', page, 'rows', rows, 'event', event);

    this.loadUsers(page, rows);
  }
}
