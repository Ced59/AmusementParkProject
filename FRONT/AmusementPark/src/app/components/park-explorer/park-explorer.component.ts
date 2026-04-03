import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { resolveLocalizedValue } from '../../commons/localized-item.utils';
import { Park } from '../../models/parks/park';
import { ParkExplorer, ParkExplorerBucket } from '../../models/parks/park-explorer';
import { ApiService } from '../../services/api.service';
import { TranslationService } from '../../services/translation.service';
import { commitViewUpdate } from '../../utils/change-detection.utils';

@Component({
  selector: 'app-park-explorer',
  templateUrl: './park-explorer.component.html',
  styleUrls: ['./park-explorer.component.scss'],
  standalone: false
})
export class ParkExplorerComponent implements OnInit, OnDestroy {
  park: Park | null = null;
  explorer: ParkExplorer | null = null;
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
    this.subscriptions.add(this.route.paramMap.subscribe((params: ParamMap) => {
      const parkId: string | null = params.get('id');
      if (parkId) {
        this.loadData(parkId);
      }
    }));

    if (this.route.parent) {
      this.subscriptions.add(this.route.parent.paramMap.subscribe((params: ParamMap) => {
        commitViewUpdate(this.changeDetectorRef, () => {
          this.currentLang = params.get('lang') ?? 'en';
        });
      }));
    }

    this.subscriptions.add(this.translationService.languageChanged.subscribe((lang: string) => {
      commitViewUpdate(this.changeDetectorRef, () => {
        this.currentLang = lang;
      });
    }));
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  get buckets(): ParkExplorerBucket[] {
    if (!this.explorer) {
      return [];
    }

    const buckets: ParkExplorerBucket[] = [...this.explorer.zones];

    if (this.explorer.unassigned && (this.explorer.hasZones || this.explorer.unassigned.totalItems > 0)) {
      buckets.push(this.explorer.unassigned);
    }

    if (!this.explorer.hasZones) {
      return [this.explorer.overview];
    }

    return buckets;
  }

  goBack(): void {
    if (this.park?.id && this.park.name) {
      this.router.navigate(['/', this.currentLang, 'park', this.park.id, this.slugify(this.park.name)]);
      return;
    }

    this.router.navigate(['/', this.currentLang, 'parks']);
  }

  getCategoryKey(category: string): string {
    return `parkExplorer.categories.${this.toCamelCase(category)}`;
  }

  getTypeKey(type: string): string {
    return `parkExplorer.types.${this.toCamelCase(type)}`;
  }

  getBucketLabel(bucket: ParkExplorerBucket): string {
    if (bucket.isVirtual && bucket.name === 'overview') {
      return 'parkExplorer.overviewTitle';
    }

    if (bucket.isVirtual && bucket.name === 'unassigned') {
      return 'parkExplorer.unassignedTitle';
    }

    return resolveLocalizedValue(bucket.names, this.currentLang) ?? bucket.name;
  }

  private loadData(parkId: string): void {
    this.apiService.getParkById(parkId).subscribe((park: Park) => {
      commitViewUpdate(this.changeDetectorRef, () => {
        this.park = park;
      });
    });

    this.apiService.getParkExplorer(parkId).subscribe((explorer: ParkExplorer) => {
      commitViewUpdate(this.changeDetectorRef, () => {
        this.explorer = explorer;
      });
    });
  }

  private slugify(value: string): string {
    return value
      .toLowerCase()
      .normalize('NFD')
      .replace(/[̀-ͯ]/g, '')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
  }

  private toCamelCase(value: string): string {
    return value.charAt(0).toLowerCase() + value.slice(1);
  }
}
