import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ParkExplorerBucket } from '../../../models/parks/park-explorer';
import { ParkItem } from '../../../models/parks/park-item';
import { TranslationService } from '../../../services/translation.service';
import { buildParkSlug } from '../../../commons/park-presentation.utils';
import { PageStateComponent } from '../../shared/page-state/page-state.component';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ParkItemCardComponent } from '../park-item-card/park-item-card.component';
import { ParkItemsPageStateFacade, SelectOption } from '@features/public/park-items/state/park-items-page-state.facade';

@Component({
  selector: 'app-park-items-page',
  templateUrl: './park-items-page.component.html',
  styleUrls: ['./park-items-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkItemsPageStateFacade],
  imports: [
    NgFor,
    NgIf,
    FormsModule,
    InputText,
    ButtonDirective,
    TranslateModule,
    PageStateComponent,
    PaginationComponent,
    EmptyStateComponent,
    ParkItemCardComponent
  ]
})
export class ParkItemsPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly park = this.stateFacade.park;
  protected readonly explorer = this.stateFacade.explorer;
  protected readonly bucketCards = this.stateFacade.bucketCards;
  protected readonly hasZones = this.stateFacade.hasZones;
  protected readonly activeZoneLabel = this.stateFacade.activeZoneLabel;
  protected readonly categoryOptions = this.stateFacade.categoryOptions;
  protected readonly typeOptions = this.stateFacade.typeOptions;
  protected readonly zoneOptions = this.stateFacade.zoneOptions;
  protected readonly topTypeHighlights = this.stateFacade.topTypeHighlights;
  protected readonly pagedItems = this.stateFacade.pagedItems;
  protected readonly totalResults = this.stateFacade.totalResults;
  protected readonly rangeStart = this.stateFacade.rangeStart;
  protected readonly rangeEnd = this.stateFacade.rangeEnd;
  protected readonly currentLang = signal<string>('en');
  protected readonly searchTerm = signal<string>('');
  protected readonly selectedCategory = signal<string | null>(null);
  protected readonly selectedType = signal<string | null>(null);
  protected readonly selectedZoneId = signal<string | null>(null);
  protected readonly pageSize = signal<number>(12);
  protected readonly currentPage = signal<number>(1);

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private currentParkId: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    protected readonly stateFacade: ParkItemsPageStateFacade
  ) {
  }

  ngOnInit(): void {
    if (this.route.parent) {
      this.route.parent.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap) => {
        const currentLang: string = params.get('lang') ?? 'en';
        this.currentLang.set(currentLang);
        this.stateFacade.setCurrentLanguage(currentLang);
      });
    }

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((lang: string) => {
      this.currentLang.set(lang);
      this.stateFacade.setCurrentLanguage(lang);
    });

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap) => {
      const parkId: string | null = params.get('id');

      if (!parkId) {
        return;
      }

      if (parkId !== this.currentParkId) {
        this.currentParkId = parkId;
        this.stateFacade.loadData(parkId, this.currentLang());
      }
    });

    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((queryParams) => {
      const searchTerm: string = queryParams.get('search') ?? '';
      const selectedCategory: string | null = queryParams.get('category');
      const selectedType: string | null = queryParams.get('type');
      const selectedZoneId: string | null = queryParams.get('zone');
      const currentPage: number = Math.max(Number(queryParams.get('page') ?? '1') || 1, 1);
      const pageSize: number = Math.max(Number(queryParams.get('size') ?? '12') || 12, 1);

      this.searchTerm.set(searchTerm);
      this.selectedCategory.set(selectedCategory);
      this.selectedType.set(selectedType);
      this.selectedZoneId.set(selectedZoneId);
      this.currentPage.set(currentPage);
      this.pageSize.set(pageSize);

      this.stateFacade.setFilters({
        searchTerm,
        selectedCategory,
        selectedType,
        selectedZoneId,
        currentPage,
        pageSize
      });
    });
  }

  get parkLink(): string[] | null {
    const currentPark = this.park();

    if (!currentPark?.id || !currentPark?.name) {
      return null;
    }

    return ['/', this.currentLang(), 'park', currentPark.id, buildParkSlug(currentPark.name)];
  }

  getParkZoneDisplayName(bucket: ParkExplorerBucket): string {
    return this.stateFacade.getParkZoneDisplayName(bucket);
  }

  getZoneCardTypeHighlights(bucket: ParkExplorerBucket): Array<{ key: string; count: number }> {
    return this.stateFacade.getZoneCardTypeHighlights(bucket);
  }

  getTypeKey(type: string): string {
    return this.stateFacade.getTypeKey(type);
  }

  getManufacturerName(item: ParkItem): string | null {
    return this.stateFacade.getManufacturerName(item);
  }

  getZoneName(item: ParkItem): string | null {
    return this.stateFacade.getZoneName(item);
  }

  isZoneSelected(bucket: ParkExplorerBucket): boolean {
    return this.stateFacade.isZoneSelected(bucket);
  }

  selectZone(bucket: ParkExplorerBucket): void {
    this.selectedZoneId.set(bucket.id ?? null);
    this.currentPage.set(1);
    this.updateQueryParams();
  }

  goBack(): void {
    if (this.parkLink) {
      this.router.navigate(this.parkLink);
      return;
    }

    this.router.navigate(['/', this.currentLang(), 'parks']);
  }

  onSearchChanged(value: string): void {
    this.searchTerm.set(value ?? '');
    this.currentPage.set(1);
    this.updateQueryParams();
  }

  onCategoryChanged(value: string | null): void {
    this.selectedCategory.set(value);
    this.currentPage.set(1);
    this.updateQueryParams();
  }

  onTypeChanged(value: string | null): void {
    this.selectedType.set(value);
    this.currentPage.set(1);
    this.updateQueryParams();
  }

  onZoneChanged(value: string | null): void {
    this.selectedZoneId.set(value);
    this.currentPage.set(1);
    this.updateQueryParams();
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.selectedCategory.set(null);
    this.selectedType.set(null);
    this.selectedZoneId.set(null);
    this.currentPage.set(1);
    this.pageSize.set(12);
    this.updateQueryParams();
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    this.currentPage.set((event.page ?? 0) + 1);
    this.pageSize.set(event.rows ?? this.pageSize());
    this.updateQueryParams();
  }

  resolveOptionLabel(option: SelectOption | null | undefined, fallbackKey: string): string {
    if (!option) {
      return fallbackKey;
    }

    if (option.labelKey) {
      return option.labelKey;
    }

    return option.label ?? fallbackKey;
  }

  private updateQueryParams(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: this.searchTerm() || null,
        category: this.selectedCategory(),
        type: this.selectedType(),
        zone: this.selectedZoneId(),
        page: this.currentPage() > 1 ? this.currentPage() : null,
        size: this.pageSize() !== 12 ? this.pageSize() : null
      },
      queryParamsHandling: 'merge'
    });
  }
}
