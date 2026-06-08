import {
  DestroyRef,
  Injectable,
  Signal,
  signal,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Params, Router, UrlSegment, UrlTree } from '@angular/router';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, distinctUntilChanged, filter, map, switchMap } from 'rxjs/operators';

import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkZone } from '@app/models/parks/park-zone';
import { resolveLocalizedValue } from '@shared/utils/localization';
import {
  buildPublicParkItemRouteCommands,
  buildPublicParkItemsRouteCommands,
  buildPublicParkRouteCommands
} from '@shared/utils/routing/public-detail-route.helpers';
import {
  PublicParkNavigationTreeItem,
  PublicParkNavigationTreeViewModel
} from '../models/public-park-navigation-tree.model';

import {
  PUBLIC_PARK_NAVIGATION_TREE_PARK_ITEMS_API_SERVICE_PORT,
  PublicParkNavigationTreeParkItemsApiServicePort,
  PUBLIC_PARK_NAVIGATION_TREE_PARK_ZONES_API_SERVICE_PORT,
  PublicParkNavigationTreeParkZonesApiServicePort,
  PUBLIC_PARK_NAVIGATION_TREE_PARKS_API_SERVICE_PORT,
  PublicParkNavigationTreeParksApiServicePort
} from './public-park-navigation-tree-data.ports';
interface PublicParkRouteContext {
  readonly language: string;
  readonly parkId: string;
  readonly parkSlug: string;
  readonly itemId: string | null;
  readonly itemSlug: string | null;
  readonly selectedZoneId: string | null;
  readonly pageKind: 'park-detail' | 'park-items' | 'park-item-detail';
}

interface PublicParkNavigationSourceData {
  readonly context: PublicParkRouteContext;
  readonly park: Park | null;
  readonly item: ParkItem | null;
  readonly zone: ParkZone | null;
}

@Injectable()
export class PublicParkNavigationTreeFacade {
  private readonly treeSignal = signal<PublicParkNavigationTreeViewModel>({
    isAvailable: false,
    isLoading: false,
    items: []
  });

  public readonly tree: Signal<PublicParkNavigationTreeViewModel> = this.treeSignal.asReadonly();

  private loadSequence: number = 0;

  constructor(
    @Inject(PUBLIC_PARK_NAVIGATION_TREE_PARKS_API_SERVICE_PORT) private readonly parksApiService: PublicParkNavigationTreeParksApiServicePort,
    @Inject(PUBLIC_PARK_NAVIGATION_TREE_PARK_ITEMS_API_SERVICE_PORT) private readonly parkItemsApiService: PublicParkNavigationTreeParkItemsApiServicePort,
    @Inject(PUBLIC_PARK_NAVIGATION_TREE_PARK_ZONES_API_SERVICE_PORT) private readonly parkZonesApiService: PublicParkNavigationTreeParkZonesApiServicePort,
    private readonly router: Router,
    private readonly destroyRef: DestroyRef
  ) {
  }

  initialize(): void {
    this.loadFromUrl(this.router.url);

    this.router.events.pipe(
      filter((event: unknown): event is NavigationEnd => event instanceof NavigationEnd),
      map((event: NavigationEnd) => event.urlAfterRedirects),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((url: string): void => {
      this.loadFromUrl(url);
    });
  }

  private loadFromUrl(url: string): void {
    const context: PublicParkRouteContext | null = this.resolveRouteContext(url);

    if (!context) {
      this.loadSequence++;
      this.treeSignal.set({
        isAvailable: false,
        isLoading: false,
        items: []
      });
      return;
    }

    const loadId: number = ++this.loadSequence;
    const previousTree: PublicParkNavigationTreeViewModel = this.treeSignal();
    this.treeSignal.set({
      isAvailable: true,
      isLoading: true,
      items: previousTree.items
    });

    this.loadSourceData(context).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (sourceData: PublicParkNavigationSourceData): void => {
        if (loadId !== this.loadSequence) {
          return;
        }

        this.treeSignal.set({
          isAvailable: true,
          isLoading: false,
          items: this.buildTreeItems(sourceData)
        });
      },
      error: (): void => {
        if (loadId !== this.loadSequence) {
          return;
        }

        this.treeSignal.set({
          isAvailable: true,
          isLoading: false,
          items: this.buildFallbackTreeItems(context)
        });
      }
    });
  }

  private loadSourceData(context: PublicParkRouteContext): Observable<PublicParkNavigationSourceData> {
    return forkJoin({
      park: this.parksApiService.getParkById(context.parkId, anonymousHttpOptions()).pipe(catchError(() => of(null as Park | null))),
      item: context.itemId
        ? this.parkItemsApiService.getParkItemById(context.itemId, anonymousHttpOptions()).pipe(catchError(() => of(null as ParkItem | null)))
        : of(null as ParkItem | null)
    }).pipe(
      switchMap(({ park, item }: { park: Park | null; item: ParkItem | null }) => {
        const zoneId: string | null = context.selectedZoneId ?? item?.zoneId ?? null;
        const zone$: Observable<ParkZone | null> = zoneId
          ? this.parkZonesApiService.getParkZoneById(zoneId, anonymousHttpOptions()).pipe(catchError(() => of(null as ParkZone | null)))
          : of(null as ParkZone | null);

        return zone$.pipe(
          map((zone: ParkZone | null): PublicParkNavigationSourceData => ({
            context,
            park,
            item,
            zone
          }))
        );
      })
    );
  }

  private buildTreeItems(sourceData: PublicParkNavigationSourceData): PublicParkNavigationTreeItem[] {
    const context: PublicParkRouteContext = sourceData.context;
    const parkLabel: string = this.resolveParkLabel(sourceData.park, context.parkId);
    const zoneLabel: string | null = sourceData.zone ? this.resolveZoneLabel(sourceData.zone, context.language) : null;
    const itemLabel: string | null = sourceData.item?.name ?? null;
    const parkRoute: string[] = this.buildParkRoute(sourceData);
    const parkItemsRoute: string[] = this.buildParkItemsRoute(sourceData);
    const items: PublicParkNavigationTreeItem[] = [
      {
        id: 'parks-list',
        label: this.resolveParksListLabel(context.language),
        icon: 'pi pi-list',
        routeCommands: ['/', context.language, 'parks'],
        level: 0,
        isCurrent: false
      },
      {
        id: `park-${context.parkId}`,
        label: parkLabel,
        icon: 'pi pi-map-marker',
        routeCommands: parkRoute,
        level: 1,
        isCurrent: context.pageKind === 'park-detail'
      }
    ];

    if (sourceData.zone?.id && zoneLabel) {
      items.push({
        id: `zone-${sourceData.zone.id}`,
        label: zoneLabel,
        icon: 'pi pi-sitemap',
        routeCommands: parkItemsRoute,
        queryParams: { zone: sourceData.zone.id } satisfies Params,
        level: 2,
        isCurrent: context.pageKind === 'park-items' && context.selectedZoneId === sourceData.zone.id
      });
    }

    if (context.pageKind === 'park-items' && !sourceData.zone?.id) {
      items.push({
        id: `park-items-${context.parkId}`,
        label: this.resolveParkItemsListLabel(context.language),
        icon: 'pi pi-th-large',
        routeCommands: parkItemsRoute,
        level: 2,
        isCurrent: true
      });
    }

    if (context.itemId && context.itemSlug) {
      items.push({
        id: `item-${context.itemId}`,
        label: itemLabel ?? context.itemSlug,
        icon: 'pi pi-star',
        routeCommands: this.buildParkItemRoute(sourceData),
        level: sourceData.zone?.id ? 3 : 2,
        isCurrent: true
      });
    }

    return items;
  }

  private buildFallbackTreeItems(context: PublicParkRouteContext): PublicParkNavigationTreeItem[] {
    return [
      {
        id: 'parks-list',
        label: this.resolveParksListLabel(context.language),
        icon: 'pi pi-list',
        routeCommands: ['/', context.language, 'parks'],
        level: 0,
        isCurrent: false
      },
      {
        id: `park-${context.parkId}`,
        label: context.parkSlug,
        icon: 'pi pi-map-marker',
        routeCommands: this.buildFallbackParkRoute(context),
        level: 1,
        isCurrent: context.pageKind === 'park-detail'
      }
    ];
  }

  private buildParkRoute(sourceData: PublicParkNavigationSourceData): string[] {
    const context: PublicParkRouteContext = sourceData.context;
    return buildPublicParkRouteCommands({
      language: context.language,
      parkId: context.parkId,
      parkName: sourceData.park?.name ?? context.parkSlug
    }) ?? this.buildFallbackParkRoute(context);
  }

  private buildParkItemsRoute(sourceData: PublicParkNavigationSourceData): string[] {
    const context: PublicParkRouteContext = sourceData.context;
    return buildPublicParkItemsRouteCommands({
      language: context.language,
      parkId: context.parkId,
      parkName: sourceData.park?.name ?? context.parkSlug
    }) ?? [...this.buildFallbackParkRoute(context), 'items'];
  }

  private buildParkItemRoute(sourceData: PublicParkNavigationSourceData): string[] {
    const context: PublicParkRouteContext = sourceData.context;
    return buildPublicParkItemRouteCommands({
      language: context.language,
      parkId: context.parkId,
      parkName: sourceData.park?.name ?? context.parkSlug,
      itemId: context.itemId,
      itemName: sourceData.item?.name ?? context.itemSlug
    }) ?? [...this.buildFallbackParkRoute(context), 'item', context.itemId ?? '', context.itemSlug ?? ''];
  }

  private buildFallbackParkRoute(context: PublicParkRouteContext): string[] {
    return ['/', context.language, 'park', context.parkId, context.parkSlug];
  }

  private resolveRouteContext(url: string): PublicParkRouteContext | null {
    const tree: UrlTree = this.router.parseUrl(url);
    const segments: UrlSegment[] = tree.root.children['primary']?.segments ?? [];
    const paths: string[] = segments.map((segment: UrlSegment) => segment.path);

    if (paths.length < 4 || paths[1] !== 'park') {
      return null;
    }

    const language: string = paths[0] || 'en';
    const parkId: string = paths[2] ?? '';
    const parkSlug: string = paths[3] ?? '';

    if (!parkId || !parkSlug) {
      return null;
    }

    if (paths[4] === 'item' && paths[5] && paths[6]) {
      return {
        language,
        parkId,
        parkSlug,
        itemId: paths[5],
        itemSlug: paths[6],
        selectedZoneId: null,
        pageKind: 'park-item-detail'
      };
    }

    if (paths[4] === 'items') {
      const zoneParam: string | string[] | undefined = tree.queryParams['zone'];
      const selectedZoneId: string | null = typeof zoneParam === 'string' && zoneParam.trim().length > 0
        ? zoneParam
        : null;

      return {
        language,
        parkId,
        parkSlug,
        itemId: null,
        itemSlug: null,
        selectedZoneId,
        pageKind: 'park-items'
      };
    }

    return {
      language,
      parkId,
      parkSlug,
      itemId: null,
      itemSlug: null,
      selectedZoneId: null,
      pageKind: 'park-detail'
    };
  }

  private resolveParkLabel(park: Park | null, fallback: string): string {
    const parkName: string | undefined = park?.name;
    if (parkName && parkName.trim().length > 0) {
      return parkName;
    }

    return fallback;
  }

  private resolveZoneLabel(zone: ParkZone, language: string): string {
    return resolveLocalizedValue(zone.names, language) ?? zone.name ?? zone.id ?? this.resolveFallbackZoneLabel(language);
  }

  private resolveParksListLabel(language: string): string {
    return language === 'fr' ? 'Liste des parcs' : 'Parks list';
  }

  private resolveParkItemsListLabel(language: string): string {
    return language === 'fr' ? 'Explorer les éléments' : 'Explore items';
  }

  private resolveFallbackZoneLabel(language: string): string {
    return language === 'fr' ? 'Zone' : 'Zone';
  }
}
