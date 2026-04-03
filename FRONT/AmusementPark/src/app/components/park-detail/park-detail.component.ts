import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { Park } from '../../models/parks/park';
import { ViewState } from '../../models/shared/view-state';

@Component({
  selector: 'app-park-detail',
  templateUrl: './park-detail.component.html',
  styleUrls: ['./park-detail.component.scss']
})
export class ParkDetailComponent implements OnInit {
  readonly park = signal<Park | null>(null);
  readonly pageState = signal<ViewState>(ViewState.Loading);
  readonly nearbyParks = signal<Park[]>([]);
  readonly nearbyState = signal<ViewState>(ViewState.Loading);

  currentLang = 'en';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly apiService: ApiService
  ) {
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    const lang = this.route.snapshot.paramMap.get('lang') || this.route.parent?.snapshot.paramMap.get('lang');

    if (lang) {
      this.currentLang = lang;
    }

    if (!id) {
      this.pageState.set(ViewState.Error);
      return;
    }

    this.pageState.set(ViewState.Loading);

    this.apiService.getParkById(id).subscribe({
      next: (park: Park) => {
        this.park.set(park);
        this.pageState.set(ViewState.Ready);
        this.loadNearbyParks(park);
      },
      error: (error: unknown) => {
        console.error('Error loading park details', error);
        this.pageState.set(ViewState.Error);
      }
    });
  }

  goBack(): void {
    this.router.navigate([`/${this.currentLang}/parks`]);
  }

  hasPracticalInfo(park: Park | null): boolean {
    if (!park) {
      return false;
    }

    return !!park.countryCode || !!park.city || !!park.street || !!park.postalCode || !!park.webSiteUrl;
  }

  hasLocationInfo(park: Park | null): boolean {
    if (!park) {
      return false;
    }

    return Number.isFinite(park.latitude) && Number.isFinite(park.longitude);
  }

  private loadNearbyParks(park: Park): void {
    if (!this.hasValidCoordinates(park)) {
      this.nearbyState.set(ViewState.Empty);
      this.nearbyParks.set([]);
      return;
    }

    this.nearbyState.set(ViewState.Loading);

    this.apiService.getParksByLocation(park.latitude, park.longitude, 150).subscribe({
      next: (parks: Park[]) => {
        const nearby = parks
          .filter((candidate: Park) => candidate.id !== park.id)
          .slice(0, 4);

        this.nearbyParks.set(nearby);
        this.nearbyState.set(nearby.length > 0 ? ViewState.Ready : ViewState.Empty);
      },
      error: (error: unknown) => {
        const errorStatus = typeof error === 'object' && error !== null && 'status' in error
          ? Number((error as { status?: number }).status)
          : 0;

        if (errorStatus === 404) {
          this.nearbyParks.set([]);
          this.nearbyState.set(ViewState.Empty);
          return;
        }

        console.error('Error loading nearby parks', error);
        this.nearbyState.set(ViewState.Error);
      }
    });
  }

  private hasValidCoordinates(park: Park): boolean {
    return Number.isFinite(park.latitude) && Number.isFinite(park.longitude);
  }
}
