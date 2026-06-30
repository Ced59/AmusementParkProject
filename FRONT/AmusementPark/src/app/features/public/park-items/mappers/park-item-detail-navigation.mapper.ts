import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemSiblingNavigation, ParkItemSiblingNavigationItem } from '@app/models/parks/park-item-sibling-navigation';
import {
  buildPublicParkItemHistoryRouteCommands,
  buildPublicParkItemImagesRouteCommands,
  buildPublicParkItemRouteCommands,
  buildPublicParkItemVideosRouteCommands,
  buildPublicParkItemsRouteCommands,
  buildPublicParkRouteCommands
} from '@shared/utils/routing/public-detail-route.helpers';
import {
  ParkItemDetailNavigationLinkViewModel,
  ParkItemDetailSiblingNavigationItemViewModel,
  ParkItemDetailSiblingNavigationViewModel
} from '../models/park-item-detail-view.model';
import { trimOrNull } from './park-item-detail-formatters';

export function buildCategoryNavigation(itemsLink: string[] | null, category: string | null | undefined): ParkItemDetailNavigationLinkViewModel | null {
  return buildNavigation(itemsLink, buildCategoryQueryParams(category));
}

export function buildTypeNavigation(itemsLink: string[] | null, type: string | null | undefined): ParkItemDetailNavigationLinkViewModel | null {
  return buildNavigation(itemsLink, buildTypeQueryParams(type));
}

export function buildZoneNavigation(itemsLink: string[] | null, zoneId: string | null | undefined): ParkItemDetailNavigationLinkViewModel | null {
  return buildNavigation(itemsLink, buildZoneQueryParams(zoneId));
}

export function buildSearchNavigation(itemsLink: string[] | null, searchTerm: string | null | undefined): ParkItemDetailNavigationLinkViewModel | null {
  return buildNavigation(itemsLink, buildSearchQueryParams(searchTerm));
}

function buildNavigation(
  itemsLink: string[] | null,
  queryParams: Record<string, string> | null
): ParkItemDetailNavigationLinkViewModel | null {
  if (!itemsLink || !queryParams) {
    return null;
  }

  return {
    routerLink: itemsLink,
    queryParams
  };
}

export function buildCategoryQueryParams(category: string | null | undefined): Record<string, string> | null {
  const normalizedCategory: string | null = trimOrNull(category);
  return normalizedCategory ? { category: normalizedCategory } : null;
}

export function buildTypeQueryParams(type: string | null | undefined): Record<string, string> | null {
  const normalizedType: string | null = trimOrNull(type);
  return normalizedType ? { type: normalizedType } : null;
}

export function buildZoneQueryParams(zoneId: string | null | undefined): Record<string, string> | null {
  const normalizedZoneId: string | null = trimOrNull(zoneId);
  return normalizedZoneId ? { zone: normalizedZoneId } : null;
}

export function buildSearchQueryParams(searchTerm: string | null | undefined): Record<string, string> | null {
  const normalizedSearchTerm: string | null = trimOrNull(searchTerm);
  return normalizedSearchTerm ? { search: normalizedSearchTerm } : null;
}

export function buildParkLink(park: Park | null, currentLanguage: string): string[] | null {
  return buildPublicParkRouteCommands({
    language: currentLanguage,
    parkId: park?.id,
    parkName: park?.name
  });
}

export function buildItemsLink(park: Park | null, currentLanguage: string): string[] | null {
  return buildPublicParkItemsRouteCommands({
    language: currentLanguage,
    parkId: park?.id,
    parkName: park?.name
  });
}

export function buildImagesLink(park: Park | null, item: ParkItem | null, currentLanguage: string): string[] | null {
  return buildPublicParkItemImagesRouteCommands({
    language: currentLanguage,
    parkId: park?.id,
    parkName: park?.name,
    itemId: item?.id,
    itemName: item?.name
  });
}

export function buildVideosLink(park: Park | null, item: ParkItem | null, currentLanguage: string): string[] | null {
  return buildPublicParkItemVideosRouteCommands({
    language: currentLanguage,
    parkId: park?.id,
    parkName: park?.name,
    itemId: item?.id,
    itemName: item?.name
  });
}

export function buildHistoryLink(park: Park | null, item: ParkItem | null, currentLanguage: string): string[] | null {
  return buildPublicParkItemHistoryRouteCommands({
    language: currentLanguage,
    parkId: park?.id,
    parkName: park?.name,
    itemId: item?.id,
    itemName: item?.name
  });
}

export function buildSiblingNavigation(
  navigation: ParkItemSiblingNavigation | null,
  park: Park | null,
  currentLanguage: string
): ParkItemDetailSiblingNavigationViewModel | null {
  if (!navigation || navigation.totalItems <= 1) {
    return null;
  }

  return {
    currentPosition: navigation.currentPosition,
    totalItems: navigation.totalItems,
    remainingItems: navigation.remainingItems,
    previous: buildSiblingNavigationItem(navigation.previous, park, currentLanguage),
    next: buildSiblingNavigationItem(navigation.next, park, currentLanguage)
  };
}

function buildSiblingNavigationItem(
  item: ParkItemSiblingNavigationItem | null,
  park: Park | null,
  currentLanguage: string
): ParkItemDetailSiblingNavigationItemViewModel | null {
  const routerLink: string[] | null = buildPublicParkItemRouteCommands({
    language: currentLanguage,
    parkId: park?.id,
    parkName: park?.name,
    itemId: item?.id,
    itemName: item?.name
  });

  if (!item || !routerLink) {
    return null;
  }

  return {
    name: item.name,
    routerLink
  };
}
