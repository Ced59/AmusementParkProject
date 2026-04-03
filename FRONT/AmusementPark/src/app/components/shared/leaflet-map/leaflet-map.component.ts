import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  Inject,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  PLATFORM_ID,
  SimpleChanges,
  ViewChild
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import {MapMarker} from "../../../models/map/map-marker";

@Component({
    selector: 'app-leaflet-map',
    templateUrl: './leaflet-map.component.html',
    styleUrls: ['./leaflet-map.component.scss'],
    standalone: false
})
export class LeafletMapComponent implements AfterViewInit, OnChanges, OnDestroy {

  private resizeObserver: ResizeObserver | null = null;

  @ViewChild('mapContainer', { static: true }) mapContainer!: ElementRef<HTMLDivElement>;

  /** Centre initial [lat, lng] */
  @Input() center: [number, number] = [0, 0];

  /** Zoom initial */
  @Input() zoom = 2;

  /** Marqueurs à afficher */
  @Input() markers: MapMarker[] = [];

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
  private L: typeof import('leaflet') | null = null;
  private map: import('leaflet').Map | null = null;
  private markerLayer: import('leaflet').LayerGroup | null = null;
  private leafletMarkers: Map<string, import('leaflet').Marker> = new Map();

  private readonly isBrowser: boolean;

  constructor(@Inject(PLATFORM_ID) platformId: Object) {
    this.isBrowser = isPlatformBrowser(platformId);
  }

  async ngAfterViewInit(): Promise<void> {
    if (!this.isBrowser) {
      // SSR : on ne fait rien
      return;
    }

    const leaflet = await import('leaflet');
    this.L = leaflet;

    // Fix des icônes
    const iconRetinaUrl = 'assets/leaflet/marker-icon-2x.png';
    const iconUrl = 'assets/leaflet/marker-icon.png';
    const shadowUrl = 'assets/leaflet/marker-shadow.png';

    const iconDefault = this.L.icon({
      iconRetinaUrl,
      iconUrl,
      shadowUrl,
      iconSize: [25, 41],
      iconAnchor: [12, 41],
      shadowSize: [41, 41]
    });

    // @ts-ignore
    this.L.Marker.prototype.options.icon = iconDefault;

    this.initMap();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.isBrowser || !this.L || !this.map) {
      return;
    }

    if (changes['markers']) {
      this.refreshMarkers();
      this.refreshMapSize();
    }

    if (changes['center'] && !changes['center'].firstChange) {
      this.map.setView(this.center, this.zoom);
      this.refreshMapSize();
    }

    if (changes['zoom'] && !changes['zoom'].firstChange) {
      this.map.setZoom(this.zoom);
      this.refreshMapSize();
    }
  }

  ngOnDestroy(): void {
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
    this.refreshMapSize();

    if (typeof ResizeObserver !== 'undefined') {
      this.resizeObserver = new ResizeObserver((): void => {
        this.refreshMapSize();
      });
      this.resizeObserver.observe(this.mapContainer.nativeElement);
    }

    if (this.editable) {
      this.map.on('click', (e: any) => this.handleMapClick(e));
    }
  }

  private refreshMarkers(): void {
    if (!this.L || !this.markerLayer) {
      return;
    }

    this.markerLayer.clearLayers();
    this.leafletMarkers.clear();

    for (const m of this.markers) {
      const marker = this.L.marker([m.lat, m.lng], {
        draggable: this.editable && (this.markers.length <= 1)
      });

      marker.addTo(this.markerLayer);

      marker.on('click', () => {
        this.markerClick.emit(m);
      });

      if (this.editable && this.markers.length <= 1) {
        marker.on('dragend', (evt: any) => {
          const target = evt.target as import('leaflet').Marker;
          const pos = target.getLatLng();
          this.positionChange.emit({ lat: pos.lat, lng: pos.lng });
        });
      }

      this.leafletMarkers.set(m.id, marker);
    }
  }

  private handleMapClick(e: any): void {
    if (!this.editable) {
      return;
    }

    if (this.markers.length === 0 || this.markers.length === 1) {
      const pos = e.latlng;
      this.positionChange.emit({ lat: pos.lat, lng: pos.lng });
    }
  }
}
