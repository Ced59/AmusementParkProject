import { ChangeDetectionStrategy, Component, OnInit, Signal, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import {
  UserCollectionEntry,
  UserCollectionKind
} from '@app/models/watchlists/user-collection-entry.model';
import { TranslationService } from '@app/services/translation.service';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import {
  buildPublicParkItemRouteCommands,
  buildPublicParkRouteCommands
} from '@shared/utils/routing/public-detail-route.helpers';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { UserCollectionsPageFacade } from '../state/user-collections-page.facade';

type UserCollectionFilter = 'All' | UserCollectionKind;

@Component({
  selector: 'app-user-collections-page',
  templateUrl: './user-collections-page.component.html',
  styleUrl: './user-collections-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [UserCollectionsPageFacade],
  imports: [
    RouterLink,
    TranslateModule,
    ImageDisplayComponent,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class UserCollectionsPageComponent implements OnInit {
  protected readonly currentLang: string;
  protected readonly filter = signal<UserCollectionFilter>('All');
  protected readonly entries: Signal<UserCollectionEntry[]> = computed((): UserCollectionEntry[] => {
    const activeFilter: UserCollectionFilter = this.filter();
    return activeFilter === 'All'
      ? this.facade.entries()
      : this.facade.entries().filter(
        (entry: UserCollectionEntry): boolean => entry.kind === activeFilter
      );
  });
  protected readonly filters: readonly UserCollectionFilter[] = [
    'All',
    'Favorite',
    'WantToVisit',
    'WantToExperience',
    'Planned'
  ];
  protected readonly imageWidths: readonly number[] = [160, 240, 320, 480];

  constructor(
    protected readonly facade: UserCollectionsPageFacade,
    translationService: TranslationService
  ) {
    this.currentLang = translationService.getCurrentLang() || 'en';
  }

  ngOnInit(): void {
    this.facade.load();
  }

  protected routeFor(entry: UserCollectionEntry): string[] | null {
    if (!entry.targetName) {
      return null;
    }

    if (entry.targetType === 'Park') {
      return buildPublicParkRouteCommands({
        language: this.currentLang,
        parkId: entry.targetId,
        parkName: entry.targetName
      });
    }

    return buildPublicParkItemRouteCommands({
      language: this.currentLang,
      parkId: entry.parentParkId,
      parkName: entry.parentParkName,
      itemId: entry.targetId,
      itemName: entry.targetName
    });
  }

  protected filterLabelKey(filter: UserCollectionFilter): string {
    return `collections.filters.${filter.charAt(0).toLowerCase()}${filter.slice(1)}`;
  }

  protected kindLabelKey(kind: UserCollectionKind): string {
    return `collections.kinds.${kind.charAt(0).toLowerCase()}${kind.slice(1)}`;
  }

  protected statusLabelKey(entry: UserCollectionEntry): string {
    return `collections.status.${entry.targetStatus.charAt(0).toLowerCase()}${entry.targetStatus.slice(1)}`;
  }
}
