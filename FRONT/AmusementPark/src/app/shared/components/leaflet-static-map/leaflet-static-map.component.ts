import { isPlatformBrowser } from '@angular/common';
import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, EventEmitter, Inject, Input, NgZone, OnChanges, OnDestroy, Output, PLATFORM_ID, SimpleChanges, ViewChild, ViewEncapsulation } from '@angular/core';
import { Router } from '@angular/router';

import { MapMarker } from '@app/models/map/map-marker';
import { MapMarkerPopupContentService } from '@shared/services/maps/map-marker-popup-content.service';
import { createLeafletMarkerIcon } from '@ui/maps/leaflet';
import type { TileLayerOptions } from 'leaflet';

type LeafletNamespace = typeof import('leaflet');
type LeafletModule = LeafletNamespace & { readonly default?: LeafletNamespace };

interface MapSizeRefreshOptions {
  readonly updateViewport?: boolean;
}

@Component({
  selector: 'app-leaflet-static-map',
  templateUrl: './leaflet-static-map.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  styleUrls: ['../leaflet-map/leaflet-map.component.scss']
})
export class LeafletStaticMapComponent implements AfterViewInit, OnChanges, OnDestroy {
  private static readonly FocusZoom = 14;
  private static readonly ReducedMobileTileMaxViewportWidth = 768;

  private resizeObserver: ResizeObserver | null = null;
  private mapSizeRefreshTimeoutIds: number[] = [];

  @ViewChild('mapContainer', { static: true }) mapContainer!: ElementRef<HTMLDivElement>;

  @Input() center: readonly [number, number] = [0, 0];
  @Input() zoom = 2;
  @Input() markers: readonly MapMarker[] = [];
  @Input() fitBounds = false;
  @Input() fitBoundsMaxZoom = 8;
  @Input() selectedMarkerId: string | null = null;

  @Output() markerClick: EventEmitter<MapMarker> = new EventEmitter<MapMarker>();

  private L: LeafletNamespace | null = null;
  private map: import('leaflet').Map | null = null;
  private tileLayer: import('leaflet').TileLayer | null = null;
  private markerLayer: import('leaflet').LayerGroup | null = null;
  private leafletMarkers: Map<string, import('leaflet').Marker> = new Map<string, import('leaflet').Marker>();
  private openPopupMarkerId: string | null = null;
  private pendingPopupMarkerId: string | null = null;

  private readonly isBrowser: boolean;

  constructor(
    @Inject(PLATFORM_ID) platformId: object,
    private readonly router: Router,
    private readonly ngZone: NgZone,
    private readonly mapMarkerPopupContentService: MapMarkerPopupContentService
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
  }

  async ngAfterViewInit(): Promise<void> {
    if (!this.isBrowser) {
      return;
    }

    try {
      const leafletModule: LeafletModule = await import('leaflet') as LeafletModule;
      this.L = this.resolveLeafletNamespace(leafletModule);

      await new Promise<void>((resolve: () => void): void => {
        window.setTimeout(resolve, 0);
      });

      this.configureDefaultMarkerIcon();
      this.initMap();
      this.scheduleMapSizeStabilization();
    } catch (error: unknown) {
      console.error('Unable to initialize static Leaflet map:', error);
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.isBrowser || !this.L || !this.map) {
      return;
    }

    const markersChanged: boolean = Boolean(changes['markers']);
    const viewportChanged: boolean = markersChanged
      || Boolean(changes['selectedMarkerId'])
      || Boolean(changes['fitBounds'])
      || Boolean(changes['center'] && !changes['center'].firstChange)
      || Boolean(changes['zoom'] && !changes['zoom'].firstChange);

    if (markersChanged) {
      this.renderMarkers();
    }

    if (viewportChanged) {
      this.applyViewport();
      this.refreshMapSize();
    }
  }

  ngOnDestroy(): void {
    this.clearMapSizeRefreshTimers();

    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
      this.resizeObserver = null;
    }

    if (this.map) {
      this.map.remove();
      this.map = null;
    }

    this.tileLayer = null;
  }

  public refreshMapSize(): void {
    if (!this.map) {
      return;
    }

    this.scheduleMapSizeRefresh(50);
  }

  private resolveLeafletNamespace(leafletModule: LeafletModule): LeafletNamespace {
    const resolvedNamespace: LeafletNamespace = leafletModule.default ?? leafletModule;

    if (typeof resolvedNamespace.icon !== 'function' || typeof resolvedNamespace.map !== 'function') {
      throw new Error('Leaflet browser namespace could not be resolved.');
    }

    return resolvedNamespace;
  }

  private configureDefaultMarkerIcon(): void {
    if (!this.L) {
      return;
    }

    const iconDefault: import('leaflet').Icon = this.L.icon({
      iconRetinaUrl: 'assets/leaflet/marker-icon-2x.png',
      iconUrl: 'assets/leaflet/marker-icon.png',
      shadowUrl: 'assets/leaflet/marker-shadow.png',
      iconSize: [25, 41],
      iconAnchor: [12, 41],
      shadowSize: [41, 41]
    });

    this.L.Marker.prototype.options.icon = iconDefault;
  }

  private initMap(): void {
    if (!this.L || !this.mapContainer) {
      return;
    }

    this.map = this.L.map(this.mapContainer.nativeElement, {
      center: this.toMutableLatLng(this.center),
      zoom: this.zoom
    });

    this.tileLayer = this.L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', this.buildTileLayerOptions()).addTo(this.map);
    this.markerLayer = this.L.layerGroup().addTo(this.map);

    this.renderMarkers();
    this.applyViewport();

    this.ngZone.runOutsideAngular((): void => {
      this.map?.on('popupopen', () => this.bindInternalPopupNavigationLinks());
    });

    if (typeof ResizeObserver !== 'undefined') {
      this.resizeObserver = new ResizeObserver((): void => {
        this.refreshMapSize();
      });
      this.resizeObserver.observe(this.mapContainer.nativeElement);
    }
  }

  private buildTileLayerOptions(): TileLayerOptions {
    const options: TileLayerOptions = {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap contributors',
      detectRetina: false,
      keepBuffer: 0,
      updateWhenIdle: true,
      updateWhenZooming: false
    };

    if (this.shouldUseReducedMobileTileRequests()) {
      options.tileSize = 512;
      options.zoomOffset = -1;
    }

    return options;
  }

  private shouldUseReducedMobileTileRequests(): boolean {
    if (!this.isBrowser || typeof window === 'undefined') {
      return false;
    }

    return window.innerWidth <= LeafletStaticMapComponent.ReducedMobileTileMaxViewportWidth;
  }

  private scheduleMapSizeStabilization(): void {
    if (!this.map) {
      return;
    }

    this.scheduleMapSizeRefresh(50, { updateViewport: true });
    this.scheduleMapSizeRefresh(250);
  }

  private scheduleMapSizeRefresh(delayMs: number, options: MapSizeRefreshOptions = {}): void {
    if (!this.map) {
      return;
    }

    const timeoutId: number = window.setTimeout((): void => {
      this.mapSizeRefreshTimeoutIds = this.mapSizeRefreshTimeoutIds.filter((candidateId: number): boolean => candidateId !== timeoutId);

      if (!this.map) {
        return;
      }

      this.map.invalidateSize({ pan: false, debounceMoveend: true });

      if (options.updateViewport === true) {
        this.applyViewport();
      }
    }, delayMs);

    this.mapSizeRefreshTimeoutIds.push(timeoutId);
  }

  private clearMapSizeRefreshTimers(): void {
    for (const timeoutId of this.mapSizeRefreshTimeoutIds) {
      window.clearTimeout(timeoutId);
    }

    this.mapSizeRefreshTimeoutIds = [];
  }

  private renderMarkers(): void {
    if (!this.L || !this.markerLayer) {
      return;
    }

    this.keepOpenPopupPendingForMarkerRefresh();
    this.markerLayer.clearLayers();
    this.leafletMarkers.clear();

    for (const marker of this.markers) {
      if (this.hasUsableCoordinates(marker)) {
        this.addMarker(marker);
      }
    }

    this.openPendingMarkerPopup();
  }

  private keepOpenPopupPendingForMarkerRefresh(): void {
    if (this.pendingPopupMarkerId !== null || this.openPopupMarkerId === null) {
      return;
    }

    this.pendingPopupMarkerId = this.openPopupMarkerId;
  }

  private addMarker(markerModel: MapMarker): void {
    if (!this.L || !this.markerLayer) {
      return;
    }

    const marker: import('leaflet').Marker = this.L.marker([markerModel.lat, markerModel.lng], {
      icon: createLeafletMarkerIcon(this.L, markerModel.iconKind)
    });
    const popupContent: string = this.mapMarkerPopupContentService.buildPopupContent(markerModel);

    if (popupContent.length > 0) {
      marker.bindPopup(popupContent);
      marker.on('popupopen', (): void => this.handleMarkerPopupOpen(markerModel.id));
      marker.on('popupclose', (): void => this.handleMarkerPopupClose(markerModel.id));
    }

    marker.addTo(this.markerLayer);
    marker.on('click', (): void => {
      this.openMarkerPopup(marker, markerModel.id);
      this.ngZone.run((): void => {
        this.markerClick.emit(markerModel);
      });
    });

    this.leafletMarkers.set(markerModel.id, marker);
  }

  private applyViewport(): void {
    if (!this.map) {
      return;
    }

    this.map.invalidateSize();

    if (this.focusSelectedMarker()) {
      return;
    }

    if (this.fitMapToMarkersIfNeeded()) {
      return;
    }

    this.applyDefaultViewport();
  }

  private focusSelectedMarker(): boolean {
    if (!this.map || !this.selectedMarkerId) {
      return false;
    }

    const marker: import('leaflet').Marker | undefined = this.leafletMarkers.get(this.selectedMarkerId);

    if (!marker) {
      return false;
    }

    const position: import('leaflet').LatLng = marker.getLatLng();
    this.map.setView(position, Math.max(this.map.getZoom(), LeafletStaticMapComponent.FocusZoom), { animate: true });
    this.openMarkerPopup(marker, this.selectedMarkerId);
    return true;
  }

  private fitMapToMarkersIfNeeded(): boolean {
    if (!this.fitBounds || !this.L || !this.map || this.markers.length === 0) {
      return false;
    }

    const fitMarkers: MapMarker[] = this.markers.filter((marker: MapMarker): boolean => this.hasUsableCoordinates(marker));

    if (fitMarkers.length === 0) {
      return false;
    }

    if (fitMarkers.length === 1) {
      const singleMarker: MapMarker = fitMarkers[0];
      this.map.setView([singleMarker.lat, singleMarker.lng], Math.max(this.zoom, 8));
      return true;
    }

    const bounds: import('leaflet').LatLngBounds = this.L.latLngBounds(fitMarkers.map((marker: MapMarker): [number, number] => [marker.lat, marker.lng]));
    this.map.fitBounds(bounds, { padding: [32, 32], maxZoom: this.fitBoundsMaxZoom });
    return true;
  }

  private applyDefaultViewport(): void {
    this.map?.setView(this.toMutableLatLng(this.center), this.zoom);
  }

  private openMarkerPopup(marker: import('leaflet').Marker, markerId: string): void {
    if (!marker.getPopup()) {
      return;
    }

    this.openPopupMarkerId = markerId;
    marker.openPopup();
  }

  private openPendingMarkerPopup(): void {
    if (this.pendingPopupMarkerId === null) {
      return;
    }

    const marker: import('leaflet').Marker | undefined = this.leafletMarkers.get(this.pendingPopupMarkerId);

    if (!marker) {
      return;
    }

    this.openMarkerPopup(marker, this.pendingPopupMarkerId);
    this.pendingPopupMarkerId = null;
  }

  private handleMarkerPopupOpen(markerId: string): void {
    this.openPopupMarkerId = markerId;
    this.bindInternalPopupNavigationLinks();
  }

  private handleMarkerPopupClose(markerId: string): void {
    if (this.openPopupMarkerId === markerId) {
      this.openPopupMarkerId = null;
    }
  }

  private hasUsableCoordinates(marker: MapMarker): boolean {
    return Number.isFinite(marker.lat)
      && Number.isFinite(marker.lng)
      && marker.lat >= -90
      && marker.lat <= 90
      && marker.lng >= -180
      && marker.lng <= 180;
  }

  private toMutableLatLng(position: readonly [number, number]): [number, number] {
    return [position[0], position[1]];
  }

  private bindInternalPopupNavigationLinks(): void {
    const links: NodeListOf<HTMLAnchorElement> = this.mapContainer.nativeElement
      .querySelectorAll<HTMLAnchorElement>('[data-app-map-popup-internal-link="true"]');

    links.forEach((link: HTMLAnchorElement): void => {
      if (link.dataset['appMapPopupNavigationBound'] === 'true') {
        return;
      }

      link.dataset['appMapPopupNavigationBound'] = 'true';
      link.addEventListener('click', (event: MouseEvent): void => this.handleInternalPopupNavigation(event));
    });
  }

  private handleInternalPopupNavigation(event: MouseEvent): void {
    event.preventDefault();

    const target: EventTarget | null = event.currentTarget;

    if (!(target instanceof HTMLAnchorElement)) {
      return;
    }

    const href: string = target.getAttribute('href') ?? '';

    if (!href) {
      return;
    }

    this.ngZone.run((): void => {
      void this.router.navigateByUrl(href);
    });
  }
}
