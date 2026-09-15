import { ChangeDetectionStrategy, Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import {
  UserCollectionKind,
  UserCollectionTargetType
} from '@app/models/watchlists/user-collection-entry.model';
import { UiButtonDirective } from '@ui/primitives';
import { UserCollectionActionsFacade } from '../state/user-collection-actions.facade';

@Component({
  selector: 'app-user-collection-actions',
  templateUrl: './user-collection-actions.component.html',
  styleUrl: './user-collection-actions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [UserCollectionActionsFacade],
  imports: [TranslateModule, UiButtonDirective]
})
export class UserCollectionActionsComponent implements OnChanges {
  @Input({ required: true }) targetType: UserCollectionTargetType = 'Park';
  @Input({ required: true }) targetId: string = '';

  constructor(protected readonly facade: UserCollectionActionsFacade) {
  }

  ngOnChanges(_changes: SimpleChanges): void {
    this.facade.configure(this.targetType, this.targetId);
  }

  protected get intentKind(): UserCollectionKind {
    return this.targetType === 'Park' ? 'WantToVisit' : 'WantToExperience';
  }

  protected labelKey(kind: UserCollectionKind): string {
    if (kind === 'Favorite') {
      return this.facade.has(kind)
        ? 'collections.actions.favoriteActive'
        : 'collections.actions.favorite';
    }

    if (kind === 'Planned') {
      return this.facade.has(kind)
        ? 'collections.actions.plannedActive'
        : 'collections.actions.planned';
    }

    return this.facade.has(kind)
      ? 'collections.actions.intentActive'
      : this.targetType === 'Park'
        ? 'collections.actions.wantToVisit'
        : 'collections.actions.wantToExperience';
  }

  protected icon(kind: UserCollectionKind): string {
    if (kind === 'Favorite') {
      return this.facade.has(kind) ? 'pi pi-heart-fill' : 'pi pi-heart';
    }

    if (kind === 'Planned') {
      return this.facade.has(kind) ? 'pi pi-calendar' : 'pi pi-calendar-plus';
    }

    return this.facade.has(kind) ? 'pi pi-bookmark-fill' : 'pi pi-bookmark';
  }
}
