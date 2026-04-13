import { ChangeDetectionStrategy, Component, OnInit, computed } from '@angular/core';
import { Router } from '@angular/router';
import { TableLazyLoadEvent } from 'primeng/table';

import { UserDto } from '../../../../models/users/user_dto';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { AdminUsersStateFacade } from '@features/admin/users/state/admin-users-state.facade';
import { AdminUsersViewComponent } from './admin-users-view.component';

@Component({
  selector: 'app-admin-users',
  templateUrl: './admin-users.component.html',
  styleUrls: ['./admin-users.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminUsersStateFacade],
  imports: [AdminUsersViewComponent]
})
export class AdminUsersComponent implements OnInit {
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
    return this.imagesApiService.resolveAvatarUrl(avatarUrl);
  }

  trackByUserId(_: number, user: UserDto): string {
    return user.id;
  }
}
