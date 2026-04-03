import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TableLazyLoadEvent } from 'primeng/table';

import { Pagination } from '../../../../models/shared/pagination';
import { UserDto } from '../../../../models/users/user_dto';
import { UsersApiResponse } from '../../../../models/users/users_api_response';
import { ApiService } from '../../../../services/api.service';

@Component({
  selector: 'app-admin-users',
  templateUrl: './admin-users.component.html',
  standalone: false,
  styleUrls: ['./admin-users.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminUsersComponent implements OnInit {
  users: UserDto[] = [];
  loading: boolean = false;

  pagination: Pagination | null = null;
  totalRecords: number = 0;
  pageSize: number = 10;
  currentPage: number = 1;

  private static readonly DEFAULT_AVATAR: string =
    'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="40" height="40" viewBox="0 0 40 40">'
    + '<rect width="40" height="40" rx="20" fill="%23f1f5f9"/>'
    + '<circle cx="20" cy="16" r="7" fill="%2394a3b8"/>'
    + '<path d="M8 33c2-6 7-9 12-9s10 3 12 9" fill="%2394a3b8"/></svg>';

  constructor(
    private readonly apiService: ApiService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    this.loadUsers(this.currentPage, this.pageSize);
  }

  loadUsers(page: number, size: number): void {
    this.loading = true;
    this.cdr.markForCheck();

    this.apiService.getUsers(page, size).subscribe({
      next: (response: UsersApiResponse) => {
        this.users = response.data ?? [];
        this.pagination = response.pagination ?? null;

        this.totalRecords = this.pagination?.totalItems ?? this.users.length;
        this.pageSize = this.pagination?.itemsPerPage ?? size;
        this.currentPage = this.pagination?.currentPage ?? page;

        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (err: unknown) => {
        console.error('Error loading users', err);
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onPageChanged(event: TableLazyLoadEvent): void {
    const rows: number = event.rows ?? this.pageSize;
    const first: number = event.first ?? 0;
    const page: number = Math.floor(first / rows) + 1;

    this.loadUsers(page, rows);
  }

  openUserProfile(userId: string): void {
    const lang: string = this.router.url.split('/')[1] || 'en';
    this.router.navigate(['/', lang, 'admin', 'users', userId]);
  }

  resolveAvatarUrl(avatarUrl: string | null | undefined): string {
    const resolved: string | null = this.apiService.resolveImageUrl(avatarUrl);

    if (resolved) {
      return resolved;
    }

    return AdminUsersComponent.DEFAULT_AVATAR;
  }
}
