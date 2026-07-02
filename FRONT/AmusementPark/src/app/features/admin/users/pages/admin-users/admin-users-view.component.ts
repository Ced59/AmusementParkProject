import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { TableLazyLoadEvent, TableModule } from '@shared/ui/primitives/table';
import { UserDto } from '@app/models/users/user_dto';
import { Card } from '@shared/ui/primitives/card';
import { UiTemplate } from '@shared/ui/primitives/api';
import { Tag } from '@shared/ui/primitives/tag';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { Tooltip } from '@shared/ui/primitives/tooltip';
import { TranslateModule } from '@ngx-translate/core';
import { EmptyStateComponent } from '@shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-admin-users-view',
  templateUrl: './admin-users-view.component.html',
  styleUrls: ['./admin-users.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Card, UiTemplate, TableModule, Tag, ButtonDirective, Tooltip, TranslateModule, EmptyStateComponent]
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

  userAvatarAlt(user: UserDto): string {
    const displayName: string = [user.firstName, user.lastName]
      .map((value: string | undefined): string => value?.trim() ?? '')
      .filter((value: string): boolean => value.length > 0)
      .join(' ')
      || user.email?.trim()
      || user.id;

    return `${displayName} avatar`;
  }

  trackByUserId(index: number, user: UserDto): string {
    return this.trackByUserIdFn(index, user);
  }
}
