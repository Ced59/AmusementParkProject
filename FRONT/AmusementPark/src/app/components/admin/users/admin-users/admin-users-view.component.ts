import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { UserDto } from '@app/models/users/user_dto';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { PrimeTemplate } from 'primeng/api';
import { Avatar } from 'primeng/avatar';
import { Tag } from 'primeng/tag';
import { ButtonDirective } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';
import { TranslateModule } from '@ngx-translate/core';
import { EmptyStateComponent } from '../../../shared/empty-state/empty-state.component';

@Component({
  selector: 'app-admin-users-view',
  templateUrl: './admin-users-view.component.html',
  styleUrls: ['./admin-users.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Bind, Card, PrimeTemplate, TableModule, Avatar, Tag, ButtonDirective, Tooltip, TranslateModule, EmptyStateComponent]
})
export class AdminUsersViewComponent {
  @Input() users!: Signal<UserDto[]>;
  @Input() loading!: Signal<boolean>;
  @Input() totalRecords!: Signal<number>;
  @Input() pageSize!: Signal<number>;
  @Input() currentPage!: Signal<number>;
  @Input() canShowHeaderTotal!: Signal<boolean>;
  @Input() resolveAvatarUrlFn: (avatarUrl: string | null | undefined) => string = () => '';
  @Input() trackByUserIdFn: (index: number, user: UserDto) => string = (_: number, user: UserDto) => user.id;

  @Output() pageChanged: EventEmitter<TableLazyLoadEvent> = new EventEmitter<TableLazyLoadEvent>();
  @Output() userOpened: EventEmitter<string> = new EventEmitter<string>();

  onPageChanged(event: TableLazyLoadEvent): void {
    this.pageChanged.emit(event);
  }

  openUserProfile(userId: string): void {
    this.userOpened.emit(userId);
  }

  resolveAvatarUrl(avatarUrl: string | null | undefined): string {
    return this.resolveAvatarUrlFn(avatarUrl);
  }

  trackByUserId(index: number, user: UserDto): string {
    return this.trackByUserIdFn(index, user);
  }
}
