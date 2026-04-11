import { ChangeDetectionStrategy, Component, OnInit, computed } from '@angular/core';
import { Router } from '@angular/router';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';

import { UserDto } from '../../../../models/users/user_dto';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { PrimeTemplate } from 'primeng/api';
import { Avatar } from 'primeng/avatar';
import { Tag } from 'primeng/tag';
import { ButtonDirective } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';
import { TranslateModule } from '@ngx-translate/core';
import { AdminUsersStateFacade } from '@features/admin/users/state/admin-users-state.facade';

@Component({
    selector: 'app-admin-users',
    templateUrl: './admin-users.component.html',
    styleUrls: ['./admin-users.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [AdminUsersStateFacade],
    imports: [Bind, Card, PrimeTemplate, TableModule, Avatar, Tag, ButtonDirective, Tooltip, TranslateModule]
})
export class AdminUsersComponent implements OnInit {
  private static readonly DEFAULT_AVATAR: string =
    'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="40" height="40" viewBox="0 0 40 40">'
    + '<rect width="40" height="40" rx="20" fill="%23f1f5f9"/>'
    + '<circle cx="20" cy="16" r="7" fill="%2394a3b8"/>'
    + '<path d="M8 33c2-6 7-9 12-9s10 3 12 9" fill="%2394a3b8"/></svg>';

  protected readonly users = this.stateFacade.users;
  protected readonly loading = this.stateFacade.loading;
  protected readonly totalRecords = this.stateFacade.totalRecords;
  protected readonly pageSize = this.stateFacade.pageSize;
  protected readonly currentPage = this.stateFacade.currentPage;
  protected readonly canShowHeaderTotal = computed(() => !this.loading());

  constructor(
    protected readonly stateFacade: AdminUsersStateFacade,
    private readonly imagesApiService: ImagesApiService,
    private readonly router: Router
  ) {
  }

  ngOnInit(): void {
    this.stateFacade.loadUsers(this.currentPage(), this.pageSize());
  }

  onPageChanged(event: TableLazyLoadEvent): void {
    const rows: number = event.rows ?? this.pageSize();
    const first: number = event.first ?? 0;
    const page: number = Math.floor(first / rows) + 1;

    this.stateFacade.loadUsers(page, rows);
  }

  openUserProfile(userId: string): void {
    const lang: string = this.router.url.split('/')[1] || 'en';
    this.router.navigate(['/', lang, 'admin', 'users', userId]);
  }

  resolveAvatarUrl(avatarUrl: string | null | undefined): string {
    const resolved: string | null = this.imagesApiService.resolveImageUrl(avatarUrl);

    if (resolved) {
      return resolved;
    }

    return AdminUsersComponent.DEFAULT_AVATAR;
  }

  trackByUserId(_: number, user: UserDto): string {
    return user.id;
  }
}
