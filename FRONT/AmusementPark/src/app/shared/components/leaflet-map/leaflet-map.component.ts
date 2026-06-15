import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, EventEmitter, Inject, Input, NgZone, OnChanges, OnDestroy, Output, PLATFORM_ID, SimpleChanges, ViewChild } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { MapMarker } from '@app/models/map/map-marker';
import { createLeafletMarkerClusterIcon, createLeafletMarkerIcon } from '@ui/maps/leaflet';
import { MapDirectionsUrlService } from '@shared/services/maps/map-directions-url.service';
import type { LeafletEvent, LeafletMouseEvent, Marker as LeafletMarker } from 'leaflet';
import { buildMarkerClusters, MarkerCluster, MarkerClusterPoint } from './leaflet-marker-cluster.builder';

type LeafletNamespace = typeof import('leaflet');
type LeafletModule = LeafletNamespace & { readonly default?: LeafletNamespace };

@Component({
    selector: 'app-leaflet-map',
    templateUrl: './leaflet-map.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ['./leaflet-map.component.scss']
})
export class LeafletMapComponent implements AfterViewInit, OnChanges, OnDestroy {

  private static readonly ClusterActivationMarkerCount = 80;
  private static readonly ClusterDisableZoom = 15;
  private static readonly ClusterFocusZoom = 14;
  private static readonly ViewportPaddingRatio = 0.35;

  private resizeObserver: ResizeObserver | null = null;
  private viewportUpdateTimeoutId: number | null = null;
  private viewportStabilizationTimeoutId: number | null = null;
  private markerRefreshTimeoutId: number | null = null;
  private pendingPopupMarkerId: string | null = null;

  @ViewChild('mapContainer', { static: true }) mapContainer!: ElementRef<HTMLDivElement>;

  /** Centre initial [lat, lng] */
  @Input() center: [number, number] = [0, 0];

  /** Zoom initial */
  @Input() zoom = 2;

  /** Marqueurs à afficher */
  @Input() markers: MapMarker[] = [];

  /** Centre automatiquement la carte sur l'ensemble des marqueurs. */
  @Input() fitBounds = false;

  /** Zoom maximal utilisé lorsqu'une carte est ajustée sur plusieurs marqueurs. */
  @Input() fitBoundsMaxZoom = 8;

  /** Marqueur à centrer et ouvrir, utile pour synchroniser une liste de résultats avec la carte. */
  @Input() selectedMarkerId: string | null = null;

  /**
   * Si true :
   *  - permet d’ajouter/déplacer un unique marker (0 ou 1)
   */
  @Input() editable = false;

  /** Emis lorsqu’une position est ajoutée/mise à jour via la carte */
  @Output() positionChange = new EventEmitter<{ lat: number; lng: number }>();

  /** Emis lorsqu’un marker est cliqué (utile pour d’autres écrans) */
  @Output() markerClick = new EventEmitter<MapMarker>();

  // Leaflet chargé dynamiquement
  private L: LeafletNamespace | null = null;
  private map: import('leaflet').Map | null = null;
  private markerLayer: import('leaflet').LayerGroup | null = null;
  private leafletMarkers: Map<string, import('leaflet').Marker> = new Map();

  private readonly isBrowser: boolean;

  constructor(
    @Inject(PLATFORM_ID) platformId: Object,
    private readonly router: Router,
    private readonly ngZone: NgZone,
    private readonly translateService: TranslateService,
    private readonly mapDirectionsUrlService: MapDirectionsUrlService
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
      console.error('Unable to initialize Leaflet map:', error);
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.isBrowser || !this.L || !this.map) {
      return;
    }

    const markersChanged: boolean = Boolean(changes['markers']);
    const selectedMarkerChanged: boolean = Boolean(changes['selectedMarkerId']);
    const fitBoundsChanged: boolean = Boolean(changes['fitBounds']);
    const centerChanged: boolean = Boolean(changes['center'] && !changes['center'].firstChange);
    const zoomChanged: boolean = Boolean(changes['zoom'] && !changes['zoom'].firstChange);

    if (markersChanged) {
      this.refreshMarkers();
    }

    if (markersChanged || selectedMarkerChanged || fitBoundsChanged) {
      this.scheduleViewportUpdate();
      return;
    }

    if (centerChanged || zoomChanged) {
      this.applyDefaultViewport();
      this.refreshMapSize();
    }
  }

  ngOnDestroy(): void {
    this.clearViewportUpdateTimers();
    this.clearMarkerRefreshTimer();

    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
      this.resizeObserver = null;
    }

    if (this.map) {
      this.map.remove();
      this.map = null;
    }
  }

  public refreshMapSize(): void {
    if (!this.map) {
      return;
    }

    window.setTimeout((): void => {
      this.map?.invalidateSize();
    }, 50);
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

  private scheduleMapSizeStabilization(): void {
    if (!this.map) {
      return;
    }

    window.setTimeout((): void => {
      this.map?.invalidateSize();
    }, 50);

    window.setTimeout((): void => {
      this.map?.invalidateSize();
    }, 250);
  }

  private initMap(): void {
    if (!this.L || !this.mapContainer) {
      return;
    }

    this.map = this.L.map(this.mapContainer.nativeElement, {
      center: this.center,
      zoom: this.zoom
    });

    this.L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap contributors'
    }).addTo(this.map);

    this.markerLayer = this.L.layerGroup().addTo(this.map);

    this.refreshMarkers();
    this.scheduleViewportUpdate();

    if (typeof ResizeObserver !== 'undefined') {
      this.resizeObserver = new ResizeObserver((): void => {
        this.refreshMapSize();
      });
      this.resizeObserver.observe(this.mapContainer.nativeElement);
    }

    this.ngZone.runOutsideAngular((): void => {
      this.map?.on('moveend zoomend', () => this.scheduleMarkerRefresh(40));
      this.map?.on('popupopen', () => this.bindInternalPopupNavigationLinks());
    });

    if (this.editable) {
      this.map.on('click', (event: LeafletMouseEvent) => this.handleMapClick(event));
    }
  }

  private scheduleViewportUpdate(): void {
    if (!this.map) {
      return;
    }

    this.clearViewportUpdateTimers();

    this.viewportUpdateTimeoutId = window.setTimeout((): void => {
      this.viewportUpdateTimeoutId = null;
      this.applyMarkerViewport();

      this.viewportStabilizationTimeoutId = window.setTimeout((): void => {
        this.viewportStabilizationTimeoutId = null;
        this.applyMarkerViewport();
      }, 120);
    }, 0);
  }

  private clearViewportUpdateTimers(): void {
    if (this.viewportUpdateTimeoutId !== null) {
      window.clearTimeout(this.viewportUpdateTimeoutId);
      this.viewportUpdateTimeoutId = null;
    }

    if (this.viewportStabilizationTimeoutId !== null) {
      window.clearTimeout(this.viewportStabilizationTimeoutId);
      this.viewportStabilizationTimeoutId = null;
    }
  }

  private scheduleMarkerRefresh(delayMs: number = 0): void {
    if (!this.map || !this.markerLayer) {
      return;
    }

    this.clearMarkerRefreshTimer();

    this.markerRefreshTimeoutId = window.setTimeout((): void => {
      this.markerRefreshTimeoutId = null;
      this.refreshMarkers();
    }, delayMs);
  }

  private clearMarkerRefreshTimer(): void {
    if (this.markerRefreshTimeoutId !== null) {
      window.clearTimeout(this.markerRefreshTimeoutId);
      this.markerRefreshTimeoutId = null;
    }
  }

  private applyMarkerViewport(): void {
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

  private applyDefaultViewport(): void {
    if (!this.map) {
      return;
    }

    if (this.fitBounds && this.markers.length > 0) {
      this.scheduleViewportUpdate();
      return;
    }

    this.map.setView(this.center, this.zoom);
  }

  private refreshMarkers(): void {
    if (!this.L || !this.map || !this.markerLayer) {
      return;
    }

    this.markerLayer.clearLayers();
    this.leafletMarkers.clear();

    const renderableMarkers: MapMarker[] = this.getRenderableMarkers();

    if (!this.shouldClusterMarkers()) {
      for (const marker of renderableMarkers) {
        this.addSingleMarker(marker);
      }

      this.openPendingSelectedMarkerPopup();
      return;
    }

    const clusters: MarkerCluster[] = buildMarkerClusters(this.toClusterPoints(renderableMarkers), {
      radiusPx: this.resolveClusterRadiusPx(),
      selectedMarkerId: this.selectedMarkerId
    });

    for (const cluster of clusters) {
      if (cluster.count === 1) {
        this.addSingleMarker(cluster.markers[0]);
        continue;
      }

      this.addClusterMarker(cluster);
    }

    this.openPendingSelectedMarkerPopup();
  }

  private getRenderableMarkers(): MapMarker[] {
    if (!this.map || this.editable || !this.shouldClusterMarkers()) {
      return this.markers.filter((marker: MapMarker): boolean => this.hasUsableCoordinates(marker));
    }

    const paddedBounds: import('leaflet').LatLngBounds = this.map.getBounds().pad(LeafletMapComponent.ViewportPaddingRatio);
    const renderableMarkers: MapMarker[] = [];
    const selectedMarker: MapMarker | undefined = this.selectedMarkerId
      ? this.markers.find((marker: MapMarker): boolean => marker.id === this.selectedMarkerId)
      : undefined;

    for (const marker of this.markers) {
      if (!this.hasUsableCoordinates(marker)) {
        continue;
      }

      if (paddedBounds.contains([marker.lat, marker.lng])) {
        renderableMarkers.push(marker);
      }
    }

    if (selectedMarker && this.hasUsableCoordinates(selectedMarker) && !renderableMarkers.some((marker: MapMarker): boolean => marker.id === selectedMarker.id)) {
      renderableMarkers.push(selectedMarker);
    }

    return renderableMarkers;
  }

  private shouldClusterMarkers(): boolean {
    return !this.editable
      && this.markers.length >= LeafletMapComponent.ClusterActivationMarkerCount
      && this.getCurrentZoom() < LeafletMapComponent.ClusterDisableZoom;
  }

  private toClusterPoints(markers: MapMarker[]): MarkerClusterPoint[] {
    if (!this.map) {
      return [];
    }

    return markers.map((marker: MapMarker): MarkerClusterPoint => {
      const point: import('leaflet').Point = this.map!.latLngToLayerPoint([marker.lat, marker.lng]);

      return {
        marker,
        x: point.x,
        y: point.y
      };
    });
  }

  private resolveClusterRadiusPx(): number {
    const zoom: number = this.getCurrentZoom();

    if (zoom <= 3) {
      return 96;
    }

    if (zoom <= 5) {
      return 78;
    }

    if (zoom <= 8) {
      return 60;
    }

    if (zoom <= 11) {
      return 44;
    }

    return 30;
  }

  private addSingleMarker(markerModel: MapMarker): void {
    if (!this.L || !this.markerLayer) {
      return;
    }

    const marker: import('leaflet').Marker = this.L.marker([markerModel.lat, markerModel.lng], {
      draggable: this.editable && (this.markers.length <= 1),
      icon: createLeafletMarkerIcon(this.L, markerModel.iconKind)
    });

    if (markerModel.title || markerModel.subtitle) {
      marker.bindPopup(this.buildPopupContent(markerModel));
    }

    marker.addTo(this.markerLayer);

    marker.on('click', () => {
      this.ngZone.run((): void => {
        this.markerClick.emit(markerModel);
      });
    });

    if (this.editable && this.markers.length <= 1) {
      marker.on('dragend', (event: LeafletEvent) => {
        const target = event.target as LeafletMarker;
        const pos = target.getLatLng();
        this.ngZone.run((): void => {
          this.positionChange.emit({ lat: pos.lat, lng: pos.lng });
        });
      });
    }

    this.leafletMarkers.set(markerModel.id, marker);
  }

  private addClusterMarker(cluster: MarkerCluster): void {
    if (!this.L || !this.map || !this.markerLayer) {
      return;
    }

    const clusterMarker: import('leaflet').Marker = this.L.marker([cluster.latitude, cluster.longitude], {
      icon: createLeafletMarkerClusterIcon(this.L, cluster.count),
      keyboard: true,
      title: `${cluster.count} markers`
    });

    clusterMarker.addTo(this.markerLayer);
    clusterMarker.on('click', (): void => this.zoomIntoCluster(cluster));
  }

  private zoomIntoCluster(cluster: MarkerCluster): void {
    if (!this.L || !this.map) {
      return;
    }

    if (cluster.markers.length <= 1) {
      return;
    }

    const bounds: import('leaflet').LatLngBounds = this.L.latLngBounds(
      cluster.markers.map((marker: MapMarker): [number, number] => [marker.lat, marker.lng])
    );
    const nextZoom: number = Math.min(this.getCurrentZoom() + 2, LeafletMapComponent.ClusterDisableZoom);

    if (bounds.isValid()) {
      this.map.fitBounds(bounds, { padding: [36, 36], maxZoom: nextZoom });
      return;
    }

    this.map.setView([cluster.latitude, cluster.longitude], nextZoom);
  }

  private openPendingSelectedMarkerPopup(): void {
    if (!this.pendingPopupMarkerId) {
      return;
    }

    const marker: import('leaflet').Marker | undefined = this.leafletMarkers.get(this.pendingPopupMarkerId);

    if (!marker) {
      return;
    }

    marker.openPopup();
    this.pendingPopupMarkerId = null;
  }

  private hasUsableCoordinates(marker: MapMarker): boolean {
    return Number.isFinite(marker.lat)
      && Number.isFinite(marker.lng)
      && marker.lat >= -90
      && marker.lat <= 90
      && marker.lng >= -180
      && marker.lng <= 180;
  }

  private getCurrentZoom(): number {
    return this.map?.getZoom() ?? this.zoom;
  }

  private focusSelectedMarker(): boolean {
    if (!this.map) {
      return false;
    }

    if (!this.selectedMarkerId) {
      this.pendingPopupMarkerId = null;
      return false;
    }

    const marker: import('leaflet').Marker | undefined = this.leafletMarkers.get(this.selectedMarkerId);

    if (marker) {
      const position = marker.getLatLng();
      this.pendingPopupMarkerId = this.selectedMarkerId;
      this.map.setView(position, Math.max(this.map.getZoom(), LeafletMapComponent.ClusterFocusZoom), { animate: true });
      marker.openPopup();
      return true;
    }

    const markerModel: MapMarker | undefined = this.markers.find((candidate: MapMarker): boolean => candidate.id === this.selectedMarkerId);

    if (!markerModel || !this.hasUsableCoordinates(markerModel)) {
      return false;
    }

    this.pendingPopupMarkerId = markerModel.id;
    this.map.setView([markerModel.lat, markerModel.lng], Math.max(this.map.getZoom(), LeafletMapComponent.ClusterFocusZoom), { animate: true });
    this.scheduleMarkerRefresh(160);
    return true;
  }

  private fitMapToMarkersIfNeeded(): boolean {
    if (!this.fitBounds || !this.L || !this.map || this.markers.length === 0) {
      return false;
    }

    if (this.markers.length === 1) {
      const singleMarker: MapMarker = this.markers[0];
      this.map.setView([singleMarker.lat, singleMarker.lng], Math.max(this.zoom, 8));
      return true;
    }

    const bounds = this.L.latLngBounds(this.markers.map((marker: MapMarker) => [marker.lat, marker.lng]));
    this.map.fitBounds(bounds, { padding: [32, 32], maxZoom: this.fitBoundsMaxZoom });
    return true;
  }

  private buildPopupContent(marker: MapMarker): string {
    const title: string = this.escapeHtml(marker.title ?? '');
    const subtitle: string = this.escapeHtml(this.resolveTranslatedPopupLine(marker.subtitleTranslationKey, marker.subtitle));
    const translatedDetails: string[] = (marker.detailTranslationKeys ?? [])
      .map((detailTranslationKey: string) => this.resolveTranslatedPopupLine(detailTranslationKey, null))
      .filter((detail: string) => detail.length > 0);
    const details: string[] = (marker.details ?? [])
      .map((detail: string) => detail.trim())
      .filter((detail: string) => detail.length > 0);

    const escapedTranslatedDetails: string[] = translatedDetails.map((detail: string) => this.escapeHtml(detail));
    const escapedDetails: string[] = details.map((detail: string) => this.escapeHtml(detail));
    const lines: string = [subtitle, ...escapedTranslatedDetails, ...escapedDetails]
      .filter((line: string) => line.length > 0)
      .map((line: string) => `<span>${line}</span>`)
      .join('');

    const actionLinks: string = this.buildPopupActions(marker);

    if (!lines && !actionLinks) {
      return `<strong>${title}</strong>`;
    }

    const linesBlock: string = lines.length > 0 ? `<div class="leaflet-map-popup__lines">${lines}</div>` : '';
    return `<strong>${title}</strong>${linesBlock}${actionLinks}`;
  }

  private resolveTranslatedPopupLine(translationKey: string | null | undefined, fallback: string | null | undefined): string {
    const normalizedTranslationKey: string = translationKey?.trim() ?? '';

    if (normalizedTranslationKey.length > 0) {
      const translatedValue: string = this.translateService.instant(normalizedTranslationKey)?.trim() ?? '';

      if (translatedValue.length > 0 && translatedValue !== normalizedTranslationKey) {
        return translatedValue;
      }
    }

    return fallback?.trim() ?? '';
  }

  private buildPopupActions(marker: MapMarker): string {
    const detailActionLink: string = this.buildPopupDetailAction(marker);
    const directionsActionLink: string = this.buildPopupDirectionsAction(marker);
    const actionLinks: string = [detailActionLink, directionsActionLink]
      .filter((actionLink: string) => actionLink.length > 0)
      .join('');

    return actionLinks.length > 0
      ? `<div class="leaflet-map-popup__actions">${actionLinks}</div>`
      : '';
  }

  private buildPopupDetailAction(marker: MapMarker): string {
    const actionUrl: string = this.resolveDetailActionUrl(marker);
    const actionLabel: string = this.resolvePopupActionLabel(marker.detailActionLabel, 'parks.map.openDetail', actionUrl);

    if (!actionUrl || !actionLabel) {
      return '';
    }

    return `<a class="leaflet-map-popup__action leaflet-map-popup__action--detail" href="${this.escapeHtml(actionUrl)}" data-app-map-popup-internal-link="true">${this.escapeHtml(actionLabel)}</a>`;
  }

  private buildPopupDirectionsAction(marker: MapMarker): string {
    const actionUrl: string = this.resolveDirectionsActionUrl(marker);
    const actionLabel: string = this.resolvePopupActionLabel(marker.actionLabel, 'parks.map.navigate', actionUrl);

    if (!actionUrl || !actionLabel) {
      return '';
    }

    return `<a class="leaflet-map-popup__action leaflet-map-popup__action--directions" href="${this.escapeHtml(actionUrl)}" target="_blank" rel="noopener noreferrer">${this.escapeHtml(actionLabel)}</a>`;
  }

  private resolvePopupActionLabel(label: string | null | undefined, fallbackTranslationKey: string, actionUrl: string): string {
    const explicitLabel: string = label?.trim() ?? '';

    if (explicitLabel.length > 0) {
      return explicitLabel;
    }

    if (!actionUrl) {
      return '';
    }

    const translatedLabel: string = this.translateService.instant(fallbackTranslationKey);
    return translatedLabel?.trim() ?? '';
  }

  private resolveDirectionsActionUrl(marker: MapMarker): string {
    const explicitUrl: string = marker.actionUrl?.trim() ?? '';

    if (explicitUrl.length > 0) {
      return explicitUrl;
    }

    if (marker.directionsActionEnabled !== true || !Number.isFinite(marker.lat) || !Number.isFinite(marker.lng)) {
      return '';
    }

    return this.mapDirectionsUrlService.buildDirectionsUrl({
      latitude: marker.lat,
      longitude: marker.lng,
      label: marker.title ?? marker.subtitle ?? null
    });
  }

  private resolveDetailActionUrl(marker: MapMarker): string {
    const explicitUrl: string = marker.detailActionUrl?.trim() ?? '';

    if (explicitUrl.length > 0) {
      return explicitUrl;
    }

    const routeCommands: string[] | null | undefined = marker.detailActionRouteCommands;

    if (!routeCommands || routeCommands.length === 0) {
      return '';
    }

    return this.router.serializeUrl(this.router.createUrlTree(routeCommands));
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

  private escapeHtml(value: string): string {
    return value
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  private handleMapClick(event: LeafletMouseEvent): void {
    if (!this.editable) {
      return;
    }

    if (this.markers.length === 0 || this.markers.length === 1) {
      const pos = event.latlng;
      this.positionChange.emit({ lat: pos.lat, lng: pos.lng });
    }
  }
}
