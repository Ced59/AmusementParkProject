import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { Subscription } from 'rxjs';

import { Park } from '../../models/parks/park';
import { ViewState } from '../../models/shared/view-state';
import { ApiService } from '../../services/api.service';
import { TranslationService } from '../../services/translation.service';
import { buildParkSlug } from '../../commons/park-presentation.utils';
import { commitViewUpdate } from '../../utils/change-detection.utils';
import { PageStateComponent } from '../shared/page-state/page-state.component';
import { NgIf } from '@angular/common';
import { Bind } from 'primeng/bind';
import { ButtonDirective } from 'primeng/button';
import { ParkHeroSectionComponent } from '../public/park-hero-section/park-hero-section.component';
import { ParkPracticalInfoSectionComponent } from '../public/park-practical-info-section/park-practical-info-section.component';
import { ParkLocationSectionComponent } from '../public/park-location-section/park-location-section.component';
import { ParkNearbySectionComponent } from '../public/park-nearby-section/park-nearby-section.component';
import { TranslateModule } from '@ngx-translate/core';
import { ParkExplorer, ParkExplorerCount } from '../../models/parks/park-explorer';
import { ParkContentSummaryComponent } from '../public/park-content-summary/park-content-summary.component';
import { ParkItem } from '../../models/parks/park-item';

@Component({
    selector: 'app-park-detail',
    templateUrl: './park-detail.component.html',
    styleUrls: ['./park-detail.component.scss'],
    imports: [PageStateComponent, NgIf, Bind, ButtonDirective, ParkHeroSectionComponent, ParkPracticalInfoSectionComponent, ParkLocationSectionComponent, ParkNearbySectionComponent, ParkContentSummaryComponent, TranslateModule]
})
export class ParkDetailComponent implements OnInit, OnDestroy {
  park: Park | null = null;
  nearbyParks: Park[] = [];
  explorer: ParkExplorer | null = null;
  pageState: ViewState = ViewState.Loading;
  nearbyState: ViewState = ViewState.Empty;
  currentLang: string = 'en';

  private readonly subscriptions: Subscription = new Subscription();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly apiService: ApiService,
    private readonly translationService: TranslationService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    this.subscriptions.add(
      this.route.paramMap.subscribe((params: ParamMap) => {
        const id: string | null = params.get('id');

        if (!id) {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.pageState = ViewState.Error;
          });
          return;
        }

        if (this.park?.id !== id) {
          this.loadPark(id);
        }
      })
    );

    if (this.route.parent) {
      this.subscriptions.add(
        this.route.parent.paramMap.subscribe((params: ParamMap) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.currentLang = params.get('lang') ?? 'en';
          });
        })
      );
    }

    this.subscriptions.add(
      this.translationService.languageChanged.subscribe((lang: string) => {
        commitViewUpdate(this.changeDetectorRef, () => {
          this.currentLang = lang;
        });
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  goBack(): void {
    this.router.navigate([`/${this.currentLang}/parks`]);
  }

  goToExplore(): void {
    if (!this.park?.id || !this.park?.name) {
      return;
    }

    this.router.navigate(['/', this.currentLang, 'park', this.park.id, buildParkSlug(this.park.name), 'items']);
  }

  hasPracticalInfo(park: Park | null): boolean {
    return !!park?.countryCode || !!park?.city || !!park?.street || !!park?.postalCode || !!park?.webSiteUrl;
  }

  hasLocationInfo(park: Park | null): boolean {
    return !!park && Number.isFinite(park.latitude) && Number.isFinite(park.longitude);
  }

  private loadPark(id: string): void {
    this.pageState = ViewState.Loading;
    this.park = null;
    this.nearbyParks = [];
    this.explorer = null;
    this.nearbyState = ViewState.Empty;

    this.subscriptions.add(
      this.apiService.getParkById(id).subscribe({
        next: (park: Park) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.park = park;
            this.pageState = ViewState.Ready;
          });
          this.loadNearbyParks(park);
          this.loadExplorerSummary(park.id ?? id);
        },
        error: (error: unknown) => {
          console.error('Error loading park details', error);
          commitViewUpdate(this.changeDetectorRef, () => {
            this.pageState = ViewState.Error;
          });
        }
      })
    );
  }


  private loadExplorerSummary(parkId: string): void {
    this.subscriptions.add(
      this.apiService.getParkExplorer(parkId).subscribe({
        next: (explorer: ParkExplorer) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.explorer = explorer;
          });
        },
        error: (error: unknown) => {
          console.error('Error loading park explorer summary', error);
          this.loadFallbackExplorerSummary(parkId);
        }
      })
    );
  }

  private loadFallbackExplorerSummary(parkId: string): void {
    this.subscriptions.add(
      this.apiService.getParkItemsByParkId(parkId).subscribe({
        next: (items: ParkItem[]) => {
          const countsByCategory: ParkExplorerCount[] = this.buildCounts(items.map((item: ParkItem) => item.category));
          const countsByType: ParkExplorerCount[] = this.buildCounts(items.map((item: ParkItem) => item.type));

          commitViewUpdate(this.changeDetectorRef, () => {
            this.explorer = {
              parkId,
              hasZones: false,
              overview: {
                name: 'overview',
                isVirtual: true,
                totalItems: items.length,
                countsByCategory,
                countsByType
              },
              zones: [],
              unassigned: null
            };
          });
        },
        error: () => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.explorer = null;
          });
        }
      })
    );
  }

  private buildCounts(values: string[]): ParkExplorerCount[] {
    const counts: Map<string, number> = new Map<string, number>();
    values.filter((value: string) => !!value).forEach((value: string) => {
      counts.set(value, (counts.get(value) ?? 0) + 1);
    });

    return Array.from(counts.entries())
      .map(([key, count]: [string, number]) => ({ key, count }))
      .sort((left: ParkExplorerCount, right: ParkExplorerCount) => right.count - left.count || left.key.localeCompare(right.key));
  }

  private loadNearbyParks(park: Park): void {
    if (!this.hasLocationInfo(park)) {
      commitViewUpdate(this.changeDetectorRef, () => {
        this.nearbyState = ViewState.Empty;
        this.nearbyParks = [];
      });
      return;
    }

    this.nearbyState = ViewState.Loading;

    this.subscriptions.add(
      this.apiService.getParksByLocation(park.latitude, park.longitude, 150).subscribe({
        next: (parks: Park[]) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.nearbyParks = parks.filter((candidate: Park) => candidate.id !== park.id).slice(0, 4);
            this.nearbyState = this.nearbyParks.length > 0 ? ViewState.Ready : ViewState.Empty;
          });
        },
        error: (error: unknown) => {
          const status: number = typeof error === 'object' && error !== null && 'status' in error
            ? Number((error as { status?: number }).status)
            : 0;

          if (status === 404) {
            commitViewUpdate(this.changeDetectorRef, () => {
              this.nearbyParks = [];
              this.nearbyState = ViewState.Empty;
            });
            return;
          }

          console.error('Error loading nearby parks', error);
          commitViewUpdate(this.changeDetectorRef, () => {
            this.nearbyState = ViewState.Error;
          });
        }
      })
    );
  }
}
