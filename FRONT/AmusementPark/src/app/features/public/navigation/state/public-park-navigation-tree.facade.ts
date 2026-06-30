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
import { VideoDto } from '@app/models/videos/video-dto';
import { resolveLocalizedValue } from '@shared/utils/localization';
import {
  buildPublicParkImagesRouteCommands,
  buildPublicParkItemImagesRouteCommands,
  buildPublicParkItemVideoRouteCommands,
  buildPublicParkItemVideosRouteCommands,
  buildPublicParkItemRouteCommands,
  buildPublicParkItemsRouteCommands,
  buildPublicParkMapRouteCommands,
  buildPublicParkOpeningHoursRouteCommands,
  buildPublicParkRouteCommands,
  buildPublicParkVideoRouteCommands,
  buildPublicParkVideosRouteCommands,
  buildPublicParkWeatherRouteCommands,
  buildPublicParkZoneRouteCommands,
  buildPublicParkZonesRouteCommands
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
  PublicParkNavigationTreeParksApiServicePort,
  PUBLIC_PARK_NAVIGATION_TREE_VIDEOS_API_SERVICE_PORT,
  PublicParkNavigationTreeVideosApiServicePort
} from './public-park-navigation-tree-data.ports';

type PublicParkRoutePageKind =
  | 'park-detail'
  | 'park-items'
  | 'park-images'
  | 'park-videos'
  | 'park-video'
  | 'park-map'
  | 'park-zones'
  | 'park-zone'
  | 'park-weather'
  | 'park-opening-hours'
  | 'park-item-detail'
  | 'park-item-images'
  | 'park-item-videos'
  | 'park-item-video';

interface PublicParkRouteContext {
  readonly language: string;
  readonly parkId: string;
  readonly parkSlug: string;
  readonly itemId: string | null;
  readonly itemSlug: string | null;
  readonly zoneId: string | null;
  readonly zoneSlug: string | null;
  readonly selectedZoneId: string | null;
  readonly videoId: string | null;
  readonly videoSlug: string | null;
  readonly pageKind: PublicParkRoutePageKind;
}

interface PublicParkNavigationSourceData {
  readonly context: PublicParkRouteContext;
  readonly park: Park | null;
  readonly item: ParkItem | null;
  readonly zone: ParkZone | null;
  readonly video: VideoDto | null;
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
    @Inject(PUBLIC_PARK_NAVIGATION_TREE_VIDEOS_API_SERVICE_PORT) private readonly videosApiService: PublicParkNavigationTreeVideosApiServicePort,
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
      park: this.parksApiService.getParkDetailSummary(context.parkId, anonymousHttpOptions()).pipe(
        map((summary) => summary.park ?? null),
        catchError(() => of(null as Park | null))
      ),
      item: context.itemId
        ? this.parkItemsApiService.getParkItemById(context.itemId, anonymousHttpOptions()).pipe(catchError(() => of(null as ParkItem | null)))
        : of(null as ParkItem | null),
      video: context.videoId
        ? this.videosApiService.getVideoById(context.videoId, anonymousHttpOptions(), context.language).pipe(catchError(() => of(null as VideoDto | null)))
        : of(null as VideoDto | null)
    }).pipe(
      switchMap(({ park, item, video }: { park: Park | null; item: ParkItem | null; video: VideoDto | null }) => {
        const zoneId: string | null = context.zoneId ?? context.selectedZoneId ?? item?.zoneId ?? null;
        const zone$: Observable<ParkZone | null> = zoneId
          ? this.parkZonesApiService.getParkZoneById(zoneId, anonymousHttpOptions()).pipe(catchError(() => of(null as ParkZone | null)))
          : of(null as ParkZone | null);

        return zone$.pipe(
          map((zone: ParkZone | null): PublicParkNavigationSourceData => ({
            context,
            park,
            item,
            zone,
            video
          }))
        );
      })
    );
  }

  private buildTreeItems(sourceData: PublicParkNavigationSourceData): PublicParkNavigationTreeItem[] {
    const context: PublicParkRouteContext = sourceData.context;
    const parkLabel: string = this.resolveParkLabel(sourceData.park, context.parkId);
    const zoneId: string | null = sourceData.zone?.id ?? context.zoneId ?? null;
    const zoneLabel: string | null = sourceData.zone ? this.resolveZoneLabel(sourceData.zone, context.language) : context.zoneSlug;
    const itemLabel: string | null = sourceData.item?.name ?? context.itemSlug;
    const videoLabel: string | null = this.resolveVideoLabel(sourceData.video, context.language, context.videoSlug ?? context.videoId);
    const parkRoute: string[] = this.buildParkRoute(sourceData);
    const parkItemsRoute: string[] = this.buildParkItemsRoute(sourceData);
    const parkImagesRoute: string[] = this.buildParkImagesRoute(sourceData);
    const parkMapRoute: string[] = this.buildParkMapRoute(sourceData);
    const parkVideosRoute: string[] = this.buildParkVideosRoute(sourceData);
    const parkZonesRoute: string[] = this.buildParkZonesRoute(sourceData);
    const parkWeatherRoute: string[] = this.buildParkWeatherRoute(sourceData);
    const parkOpeningHoursRoute: string[] = this.buildParkOpeningHoursRoute(sourceData);
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

    if (zoneId && zoneLabel && (context.pageKind === 'park-zone' || context.pageKind === 'park-items' || !!context.itemId)) {
      items.push({
        id: `zone-${zoneId}`,
        label: zoneLabel,
        icon: 'pi pi-sitemap',
        routeCommands: context.pageKind === 'park-zone' ? this.buildParkZoneRoute(sourceData) : parkItemsRoute,
        queryParams: context.pageKind === 'park-zone' ? undefined : { zone: zoneId } satisfies Params,
        level: 2,
        isCurrent: context.pageKind === 'park-zone' || (context.pageKind === 'park-items' && context.selectedZoneId === zoneId)
      });
    }

    if (context.pageKind === 'park-items' && !zoneId) {
      items.push({
        id: `park-items-${context.parkId}`,
        label: this.resolveParkItemsListLabel(context.language, parkLabel),
        icon: 'pi pi-th-large',
        routeCommands: parkItemsRoute,
        level: 2,
        isCurrent: true
      });
    }

    if (context.pageKind === 'park-images') {
      items.push({
        id: `park-images-${context.parkId}`,
        label: this.resolveParkImagesLabel(context.language, parkLabel),
        icon: 'pi pi-images',
        routeCommands: parkImagesRoute,
        level: 2,
        isCurrent: true
      });
    }

    if (context.pageKind === 'park-videos' || context.pageKind === 'park-video') {
      items.push({
        id: `park-videos-${context.parkId}`,
        label: this.resolveParkVideosLabel(context.language, parkLabel),
        icon: 'pi pi-video',
        routeCommands: parkVideosRoute,
        level: 2,
        isCurrent: context.pageKind === 'park-videos'
      });
    }

    if (context.pageKind === 'park-video' && context.videoId && videoLabel) {
      items.push({
        id: `park-video-${context.videoId}`,
        label: videoLabel,
        icon: 'pi pi-play-circle',
        routeCommands: this.buildParkVideoRoute(sourceData),
        level: 3,
        isCurrent: true
      });
    }

    if (context.pageKind === 'park-map') {
      items.push({
        id: `park-map-${context.parkId}`,
        label: this.resolveParkMapLabel(context.language, parkLabel),
        icon: 'pi pi-map',
        routeCommands: parkMapRoute,
        level: 2,
        isCurrent: true
      });
    }

    if (context.pageKind === 'park-zones') {
      items.push({
        id: `park-zones-${context.parkId}`,
        label: this.resolveParkZonesLabel(context.language, parkLabel),
        icon: 'pi pi-sitemap',
        routeCommands: parkZonesRoute,
        level: 2,
        isCurrent: true
      });
    }

    if (context.pageKind === 'park-weather') {
      items.push({
        id: `park-weather-${context.parkId}`,
        label: this.resolveParkWeatherLabel(context.language, parkLabel),
        icon: 'pi pi-cloud',
        routeCommands: parkWeatherRoute,
        level: 2,
        isCurrent: true
      });
    }

    if (context.pageKind === 'park-opening-hours') {
      items.push({
        id: `park-opening-hours-${context.parkId}`,
        label: this.resolveParkOpeningHoursLabel(context.language, parkLabel),
        icon: 'pi pi-clock',
        routeCommands: parkOpeningHoursRoute,
        level: 2,
        isCurrent: true
      });
    }

    if (context.itemId && context.itemSlug) {
      const itemLevel: number = zoneId ? 3 : 2;
      items.push({
        id: `item-${context.itemId}`,
        label: itemLabel ?? context.itemSlug,
        icon: 'pi pi-star',
        routeCommands: this.buildParkItemRoute(sourceData),
        level: itemLevel,
        isCurrent: context.pageKind === 'park-item-detail'
      });

      if (context.pageKind === 'park-item-images') {
        items.push({
          id: `item-images-${context.itemId}`,
          label: this.resolveParkItemImagesLabel(context.language, itemLabel ?? context.itemSlug),
          icon: 'pi pi-images',
          routeCommands: this.buildParkItemImagesRoute(sourceData),
          level: itemLevel + 1,
          isCurrent: true
        });
      }

      if (context.pageKind === 'park-item-videos' || context.pageKind === 'park-item-video') {
        items.push({
          id: `item-videos-${context.itemId}`,
          label: this.resolveParkItemVideosLabel(context.language, itemLabel ?? context.itemSlug),
          icon: 'pi pi-video',
          routeCommands: this.buildParkItemVideosRoute(sourceData),
          level: itemLevel + 1,
          isCurrent: context.pageKind === 'park-item-videos'
        });
      }

      if (context.pageKind === 'park-item-video' && context.videoId && videoLabel) {
        items.push({
          id: `item-video-${context.videoId}`,
          label: videoLabel,
          icon: 'pi pi-play-circle',
          routeCommands: this.buildParkItemVideoRoute(sourceData),
          level: itemLevel + 2,
          isCurrent: true
        });
      }
    }

    return items;
  }

  private buildFallbackTreeItems(context: PublicParkRouteContext): PublicParkNavigationTreeItem[] {
    const parkLabel: string = context.parkSlug;
    const zoneLabel: string | null = context.zoneSlug;
    const itemLabel: string | null = context.itemSlug;
    const videoLabel: string | null = context.videoSlug;
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
        routeCommands: this.buildFallbackParkRoute(context),
        level: 1,
        isCurrent: context.pageKind === 'park-detail'
      }
    ];

    if (context.zoneId && zoneLabel && (context.pageKind === 'park-zone' || context.pageKind === 'park-items' || !!context.itemId)) {
      items.push({
        id: `zone-${context.zoneId}`,
        label: zoneLabel,
        icon: 'pi pi-sitemap',
        routeCommands: context.pageKind === 'park-zone' ? this.buildFallbackParkZoneRoute(context) : this.buildFallbackParkItemsRoute(context),
        queryParams: context.pageKind === 'park-zone' ? undefined : { zone: context.zoneId } satisfies Params,
        level: 2,
        isCurrent: context.pageKind === 'park-zone' || (context.pageKind === 'park-items' && context.selectedZoneId === context.zoneId)
      });
    }

    if (context.pageKind === 'park-items' && !context.zoneId) {
      items.push({
        id: `park-items-${context.parkId}`,
        label: this.resolveParkItemsListLabel(context.language, parkLabel),
        icon: 'pi pi-th-large',
        routeCommands: this.buildFallbackParkItemsRoute(context),
        level: 2,
        isCurrent: true
      });
    }

    if (context.pageKind === 'park-images') {
      items.push({
        id: `park-images-${context.parkId}`,
        label: this.resolveParkImagesLabel(context.language, parkLabel),
        icon: 'pi pi-images',
        routeCommands: this.buildFallbackParkImagesRoute(context),
        level: 2,
        isCurrent: true
      });
    }

    if (context.pageKind === 'park-videos' || context.pageKind === 'park-video') {
      items.push({
        id: `park-videos-${context.parkId}`,
        label: this.resolveParkVideosLabel(context.language, parkLabel),
        icon: 'pi pi-video',
        routeCommands: this.buildFallbackParkVideosRoute(context),
        level: 2,
        isCurrent: context.pageKind === 'park-videos'
      });
    }

    if (context.pageKind === 'park-video' && context.videoId && videoLabel) {
      items.push({
        id: `park-video-${context.videoId}`,
        label: videoLabel,
        icon: 'pi pi-play-circle',
        routeCommands: this.buildFallbackParkVideoRoute(context),
        level: 3,
        isCurrent: true
      });
    }

    if (context.pageKind === 'park-map') {
      items.push({
        id: `park-map-${context.parkId}`,
        label: this.resolveParkMapLabel(context.language, parkLabel),
        icon: 'pi pi-map',
        routeCommands: this.buildFallbackParkMapRoute(context),
        level: 2,
        isCurrent: true
      });
    }

    if (context.pageKind === 'park-zones') {
      items.push({
        id: `park-zones-${context.parkId}`,
        label: this.resolveParkZonesLabel(context.language, parkLabel),
        icon: 'pi pi-sitemap',
        routeCommands: this.buildFallbackParkZonesRoute(context),
        level: 2,
        isCurrent: true
      });
    }

    if (context.pageKind === 'park-weather') {
      items.push({
        id: `park-weather-${context.parkId}`,
        label: this.resolveParkWeatherLabel(context.language, parkLabel),
        icon: 'pi pi-cloud',
        routeCommands: this.buildFallbackParkWeatherRoute(context),
        level: 2,
        isCurrent: true
      });
    }

    if (context.pageKind === 'park-opening-hours') {
      items.push({
        id: `park-opening-hours-${context.parkId}`,
        label: this.resolveParkOpeningHoursLabel(context.language, parkLabel),
        icon: 'pi pi-clock',
        routeCommands: this.buildFallbackParkOpeningHoursRoute(context),
        level: 2,
        isCurrent: true
      });
    }

    if (context.itemId && context.itemSlug) {
      const itemLevel: number = context.zoneId ? 3 : 2;
      items.push({
        id: `item-${context.itemId}`,
        label: itemLabel ?? context.itemSlug,
        icon: 'pi pi-star',
        routeCommands: this.buildFallbackParkItemRoute(context),
        level: itemLevel,
        isCurrent: context.pageKind === 'park-item-detail'
      });

      if (context.pageKind === 'park-item-images') {
        items.push({
          id: `item-images-${context.itemId}`,
          label: this.resolveParkItemImagesLabel(context.language, context.itemSlug),
          icon: 'pi pi-images',
          routeCommands: this.buildFallbackParkItemImagesRoute(context),
          level: itemLevel + 1,
          isCurrent: true
        });
      }

      if (context.pageKind === 'park-item-videos' || context.pageKind === 'park-item-video') {
        items.push({
          id: `item-videos-${context.itemId}`,
          label: this.resolveParkItemVideosLabel(context.language, itemLabel ?? context.itemSlug),
          icon: 'pi pi-video',
          routeCommands: this.buildFallbackParkItemVideosRoute(context),
          level: itemLevel + 1,
          isCurrent: context.pageKind === 'park-item-videos'
        });
      }

      if (context.pageKind === 'park-item-video' && context.videoId && videoLabel) {
        items.push({
          id: `item-video-${context.videoId}`,
          label: videoLabel,
          icon: 'pi pi-play-circle',
          routeCommands: this.buildFallbackParkItemVideoRoute(context),
          level: itemLevel + 2,
          isCurrent: true
        });
      }
    }

    return items;
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

  private buildParkImagesRoute(sourceData: PublicParkNavigationSourceData): string[] {
    const context: PublicParkRouteContext = sourceData.context;
    return buildPublicParkImagesRouteCommands({
      language: context.language,
      parkId: context.parkId,
      parkName: sourceData.park?.name ?? context.parkSlug
    }) ?? this.buildFallbackParkImagesRoute(context);
  }

  private buildParkMapRoute(sourceData: PublicParkNavigationSourceData): string[] {
    const context: PublicParkRouteContext = sourceData.context;
    return buildPublicParkMapRouteCommands({
      language: context.language,
      parkId: context.parkId,
      parkName: sourceData.park?.name ?? context.parkSlug
    }) ?? this.buildFallbackParkMapRoute(context);
  }

  private buildParkVideosRoute(sourceData: PublicParkNavigationSourceData): string[] {
    const context: PublicParkRouteContext = sourceData.context;
    return buildPublicParkVideosRouteCommands({
      language: context.language,
      parkId: context.parkId,
      parkName: sourceData.park?.name ?? context.parkSlug
    }) ?? this.buildFallbackParkVideosRoute(context);
  }

  private buildParkVideoRoute(sourceData: PublicParkNavigationSourceData): string[] {
    const context: PublicParkRouteContext = sourceData.context;
    return buildPublicParkVideoRouteCommands({
      language: context.language,
      parkId: context.parkId,
      parkName: sourceData.park?.name ?? context.parkSlug,
      videoId: context.videoId,
      videoTitle: this.resolveVideoLabel(sourceData.video, context.language, context.videoSlug)
    }) ?? this.buildFallbackParkVideoRoute(context);
  }

  private buildParkZonesRoute(sourceData: PublicParkNavigationSourceData): string[] {
    const context: PublicParkRouteContext = sourceData.context;
    return buildPublicParkZonesRouteCommands({
      language: context.language,
      parkId: context.parkId,
      parkName: sourceData.park?.name ?? context.parkSlug
    }) ?? this.buildFallbackParkZonesRoute(context);
  }

  private buildParkZoneRoute(sourceData: PublicParkNavigationSourceData): string[] {
    const context: PublicParkRouteContext = sourceData.context;
    const zoneName: string | null = sourceData.zone ? this.resolveZoneLabel(sourceData.zone, context.language) : context.zoneSlug;
    return buildPublicParkZoneRouteCommands({
      language: context.language,
      parkId: context.parkId,
      parkName: sourceData.park?.name ?? context.parkSlug,
      zoneId: sourceData.zone?.id ?? context.zoneId,
      zoneName
    }) ?? this.buildFallbackParkZoneRoute(context);
  }

  private buildParkWeatherRoute(sourceData: PublicParkNavigationSourceData): string[] {
    const context: PublicParkRouteContext = sourceData.context;
    return buildPublicParkWeatherRouteCommands({
      language: context.language,
      parkId: context.parkId,
      parkName: sourceData.park?.name ?? context.parkSlug
    }) ?? this.buildFallbackParkWeatherRoute(context);
  }

  private buildParkOpeningHoursRoute(sourceData: PublicParkNavigationSourceData): string[] {
    const context: PublicParkRouteContext = sourceData.context;
    return buildPublicParkOpeningHoursRouteCommands({
      language: context.language,
      parkId: context.parkId,
      parkName: sourceData.park?.name ?? context.parkSlug
    }) ?? this.buildFallbackParkOpeningHoursRoute(context);
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

  private buildParkItemImagesRoute(sourceData: PublicParkNavigationSourceData): string[] {
    const context: PublicParkRouteContext = sourceData.context;
    return buildPublicParkItemImagesRouteCommands({
      language: context.language,
      parkId: context.parkId,
      parkName: sourceData.park?.name ?? context.parkSlug,
      itemId: context.itemId,
      itemName: sourceData.item?.name ?? context.itemSlug
    }) ?? this.buildFallbackParkItemImagesRoute(context);
  }

  private buildParkItemVideosRoute(sourceData: PublicParkNavigationSourceData): string[] {
    const context: PublicParkRouteContext = sourceData.context;
    return buildPublicParkItemVideosRouteCommands({
      language: context.language,
      parkId: context.parkId,
      parkName: sourceData.park?.name ?? context.parkSlug,
      itemId: context.itemId,
      itemName: sourceData.item?.name ?? context.itemSlug
    }) ?? this.buildFallbackParkItemVideosRoute(context);
  }

  private buildParkItemVideoRoute(sourceData: PublicParkNavigationSourceData): string[] {
    const context: PublicParkRouteContext = sourceData.context;
    return buildPublicParkItemVideoRouteCommands({
      language: context.language,
      parkId: context.parkId,
      parkName: sourceData.park?.name ?? context.parkSlug,
      itemId: context.itemId,
      itemName: sourceData.item?.name ?? context.itemSlug,
      videoId: context.videoId,
      videoTitle: this.resolveVideoLabel(sourceData.video, context.language, context.videoSlug)
    }) ?? this.buildFallbackParkItemVideoRoute(context);
  }

  private buildFallbackParkRoute(context: PublicParkRouteContext): string[] {
    return ['/', context.language, 'park', context.parkId, context.parkSlug];
  }

  private buildFallbackParkItemsRoute(context: PublicParkRouteContext): string[] {
    return [...this.buildFallbackParkRoute(context), 'items'];
  }

  private buildFallbackParkImagesRoute(context: PublicParkRouteContext): string[] {
    return [...this.buildFallbackParkRoute(context), 'images'];
  }

  private buildFallbackParkVideosRoute(context: PublicParkRouteContext): string[] {
    return [...this.buildFallbackParkRoute(context), 'videos'];
  }

  private buildFallbackParkVideoRoute(context: PublicParkRouteContext): string[] {
    return [...this.buildFallbackParkVideosRoute(context), context.videoId ?? '', context.videoSlug ?? ''];
  }

  private buildFallbackParkMapRoute(context: PublicParkRouteContext): string[] {
    return [...this.buildFallbackParkRoute(context), 'map'];
  }

  private buildFallbackParkZonesRoute(context: PublicParkRouteContext): string[] {
    return [...this.buildFallbackParkRoute(context), 'zones'];
  }

  private buildFallbackParkZoneRoute(context: PublicParkRouteContext): string[] {
    return [...this.buildFallbackParkRoute(context), 'zone', context.zoneId ?? '', context.zoneSlug ?? ''];
  }

  private buildFallbackParkWeatherRoute(context: PublicParkRouteContext): string[] {
    return [...this.buildFallbackParkRoute(context), 'weather'];
  }

  private buildFallbackParkOpeningHoursRoute(context: PublicParkRouteContext): string[] {
    return [...this.buildFallbackParkRoute(context), 'opening-hours'];
  }

  private buildFallbackParkItemRoute(context: PublicParkRouteContext): string[] {
    return [...this.buildFallbackParkRoute(context), 'item', context.itemId ?? '', context.itemSlug ?? ''];
  }

  private buildFallbackParkItemImagesRoute(context: PublicParkRouteContext): string[] {
    return [...this.buildFallbackParkItemRoute(context), 'images'];
  }

  private buildFallbackParkItemVideosRoute(context: PublicParkRouteContext): string[] {
    return [...this.buildFallbackParkItemRoute(context), 'videos'];
  }

  private buildFallbackParkItemVideoRoute(context: PublicParkRouteContext): string[] {
    return [...this.buildFallbackParkItemVideosRoute(context), context.videoId ?? '', context.videoSlug ?? ''];
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
      const baseItemContext = {
        language,
        parkId,
        parkSlug,
        itemId: paths[5],
        itemSlug: paths[6],
        zoneId: null,
        zoneSlug: null,
        selectedZoneId: null
      };

      if (paths[7] === 'images') {
        return {
          ...baseItemContext,
          videoId: null,
          videoSlug: null,
          pageKind: 'park-item-images'
        };
      }

      if (paths[7] === 'videos' && paths[8] && paths[9]) {
        return {
          ...baseItemContext,
          videoId: paths[8],
          videoSlug: paths[9],
          pageKind: 'park-item-video'
        };
      }

      if (paths[7] === 'videos') {
        return {
          ...baseItemContext,
          videoId: null,
          videoSlug: null,
          pageKind: 'park-item-videos'
        };
      }

      return {
        ...baseItemContext,
        videoId: null,
        videoSlug: null,
        pageKind: 'park-item-detail'
      };
    }

    if (paths[4] === 'images') {
      return {
        language,
        parkId,
        parkSlug,
        itemId: null,
        itemSlug: null,
        zoneId: null,
        zoneSlug: null,
        selectedZoneId: null,
        videoId: null,
        videoSlug: null,
        pageKind: 'park-images'
      };
    }

    if (paths[4] === 'videos' && paths[5] && paths[6]) {
      return {
        language,
        parkId,
        parkSlug,
        itemId: null,
        itemSlug: null,
        zoneId: null,
        zoneSlug: null,
        selectedZoneId: null,
        videoId: paths[5],
        videoSlug: paths[6],
        pageKind: 'park-video'
      };
    }

    if (paths[4] === 'videos') {
      return {
        language,
        parkId,
        parkSlug,
        itemId: null,
        itemSlug: null,
        zoneId: null,
        zoneSlug: null,
        selectedZoneId: null,
        videoId: null,
        videoSlug: null,
        pageKind: 'park-videos'
      };
    }

    if (paths[4] === 'map') {
      return {
        language,
        parkId,
        parkSlug,
        itemId: null,
        itemSlug: null,
        zoneId: null,
        zoneSlug: null,
        selectedZoneId: null,
        videoId: null,
        videoSlug: null,
        pageKind: 'park-map'
      };
    }

    if (paths[4] === 'zones') {
      return {
        language,
        parkId,
        parkSlug,
        itemId: null,
        itemSlug: null,
        zoneId: null,
        zoneSlug: null,
        selectedZoneId: null,
        videoId: null,
        videoSlug: null,
        pageKind: 'park-zones'
      };
    }

    if (paths[4] === 'zone' && paths[5] && paths[6]) {
      return {
        language,
        parkId,
        parkSlug,
        itemId: null,
        itemSlug: null,
        zoneId: paths[5],
        zoneSlug: paths[6],
        selectedZoneId: null,
        videoId: null,
        videoSlug: null,
        pageKind: 'park-zone'
      };
    }

    if (paths[4] === 'weather') {
      return {
        language,
        parkId,
        parkSlug,
        itemId: null,
        itemSlug: null,
        zoneId: null,
        zoneSlug: null,
        selectedZoneId: null,
        videoId: null,
        videoSlug: null,
        pageKind: 'park-weather'
      };
    }

    if (paths[4] === 'opening-hours') {
      return {
        language,
        parkId,
        parkSlug,
        itemId: null,
        itemSlug: null,
        zoneId: null,
        zoneSlug: null,
        selectedZoneId: null,
        videoId: null,
        videoSlug: null,
        pageKind: 'park-opening-hours'
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
        zoneId: selectedZoneId,
        zoneSlug: null,
        selectedZoneId,
        videoId: null,
        videoSlug: null,
        pageKind: 'park-items'
      };
    }

    return {
      language,
      parkId,
      parkSlug,
      itemId: null,
      itemSlug: null,
      zoneId: null,
      zoneSlug: null,
      selectedZoneId: null,
      videoId: null,
      videoSlug: null,
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
    const labels: Record<string, string> = {
      fr: 'Liste des parcs',
      en: 'Parks list',
      es: 'Lista de parques',
      de: 'Parkliste',
      it: 'Elenco dei parchi',
      nl: 'Parkenlijst',
      pl: 'Lista parków',
      pt: 'Lista de parques'
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkItemsListLabel(language: string, parkLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Lieux de ${parkLabel}`,
      en: `Places at ${parkLabel}`,
      es: `Lugares de ${parkLabel}`,
      de: `Orte in ${parkLabel}`,
      it: `Luoghi di ${parkLabel}`,
      nl: `Plekken in ${parkLabel}`,
      pl: `Miejsca w ${parkLabel}`,
      pt: `Locais de ${parkLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkImagesLabel(language: string, parkLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Images de ${parkLabel}`,
      en: `${parkLabel} images`,
      es: `Imágenes de ${parkLabel}`,
      de: `Bilder von ${parkLabel}`,
      it: `Immagini di ${parkLabel}`,
      nl: `Afbeeldingen van ${parkLabel}`,
      pl: `Zdjęcia ${parkLabel}`,
      pt: `Imagens de ${parkLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkItemImagesLabel(language: string, itemLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Images de ${itemLabel}`,
      en: `${itemLabel} images`,
      es: `Imágenes de ${itemLabel}`,
      de: `Bilder von ${itemLabel}`,
      it: `Immagini di ${itemLabel}`,
      nl: `Afbeeldingen van ${itemLabel}`,
      pl: `Zdjęcia ${itemLabel}`,
      pt: `Imagens de ${itemLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkMapLabel(language: string, parkLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Carte de ${parkLabel}`,
      en: `${parkLabel} map`,
      es: `Mapa de ${parkLabel}`,
      de: `Karte von ${parkLabel}`,
      it: `Mappa di ${parkLabel}`,
      nl: `Kaart van ${parkLabel}`,
      pl: `Mapa ${parkLabel}`,
      pt: `Mapa de ${parkLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkVideosLabel(language: string, parkLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Vidéos de ${parkLabel}`,
      en: `${parkLabel} videos`,
      es: `Vídeos de ${parkLabel}`,
      de: `Videos von ${parkLabel}`,
      it: `Video di ${parkLabel}`,
      nl: `Video's van ${parkLabel}`,
      pl: `Filmy z ${parkLabel}`,
      pt: `Vídeos de ${parkLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkZonesLabel(language: string, parkLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Zones de ${parkLabel}`,
      en: `${parkLabel} zones`,
      es: `Zonas de ${parkLabel}`,
      de: `Bereiche von ${parkLabel}`,
      it: `Zone di ${parkLabel}`,
      nl: `Zones van ${parkLabel}`,
      pl: `Strefy ${parkLabel}`,
      pt: `Zonas de ${parkLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkWeatherLabel(language: string, parkLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Météo de ${parkLabel}`,
      en: `Weather for ${parkLabel}`,
      es: `Tiempo de ${parkLabel}`,
      de: `Wetter für ${parkLabel}`,
      it: `Meteo di ${parkLabel}`,
      nl: `Weer voor ${parkLabel}`,
      pl: `Pogoda dla ${parkLabel}`,
      pt: `Meteorologia de ${parkLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkOpeningHoursLabel(language: string, parkLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Dates et horaires de ${parkLabel}`,
      en: `Opening hours for ${parkLabel}`,
      es: `Fechas y horarios de ${parkLabel}`,
      de: `Öffnungszeiten von ${parkLabel}`,
      it: `Date e orari di ${parkLabel}`,
      nl: `Datums en openingstijden van ${parkLabel}`,
      pl: `Godziny otwarcia ${parkLabel}`,
      pt: `Datas e horários de ${parkLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkItemVideosLabel(language: string, itemLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Vidéos de ${itemLabel}`,
      en: `${itemLabel} videos`,
      es: `Vídeos de ${itemLabel}`,
      de: `Videos von ${itemLabel}`,
      it: `Video di ${itemLabel}`,
      nl: `Video's van ${itemLabel}`,
      pl: `Filmy z ${itemLabel}`,
      pt: `Vídeos de ${itemLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveVideoLabel(video: VideoDto | null, language: string, fallback: string | null | undefined): string | null {
    if (video) {
      const localizedTitle: string | undefined = resolveLocalizedValue(video.titles, language);
      const title: string = localizedTitle ?? video.title;
      const normalizedTitle: string = title.trim();

      if (normalizedTitle.length > 0) {
        return normalizedTitle;
      }
    }

    const normalizedFallback: string = fallback?.trim() ?? '';
    return normalizedFallback.length > 0 ? normalizedFallback : null;
  }

  private resolveFallbackZoneLabel(language: string): string {
    return language === 'fr' ? 'Zone' : 'Zone';
  }
}
