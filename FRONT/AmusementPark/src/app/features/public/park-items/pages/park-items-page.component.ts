import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TranslationService } from '@app/services/translation.service';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { ParkItemsPageStateFacade } from '../state/park-items-page-state.facade';
import { ParkItemsListViewComponent } from '../ui/park-items-list-view.component';

@Component({
  selector: 'app-park-items-page',
  templateUrl: './park-items-page.component.html',
  styleUrls: ['./park-items-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkItemsPageStateFacade],
  imports: [ParkItemsListViewComponent]
})
export class ParkItemsPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly pageView = this.stateFacade.pageView;
  protected readonly zoneCards = this.stateFacade.zoneCards;
  protected readonly zoneFocus = this.stateFacade.zoneFocus;
  protected readonly pagedItems = this.stateFacade.pagedItems;
  protected readonly categoryOptions = this.stateFacade.categoryOptions;
  protected readonly typeOptions = this.stateFacade.typeOptions;
  protected readonly zoneOptions = this.stateFacade.zoneOptions;
  protected readonly totalResults = this.stateFacade.totalResults;
  protected readonly rangeStart = this.stateFacade.rangeStart;
  protected readonly rangeEnd = this.stateFacade.rangeEnd;
  protected readonly currentPage = this.stateFacade.currentPage;
  protected readonly pageSize = this.stateFacade.pageSize;
  protected readonly currentLanguage = signal<string>('en');
  protected readonly searchTerm = signal<string>('');
  protected readonly selectedCategory = signal<string | null>(null);
  protected readonly selectedType = signal<string | null>(null);
  protected readonly selectedZoneId = signal<string | null>(null);

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private currentParkId: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly stateFacade: ParkItemsPageStateFacade
  ) {
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');

    this.currentLanguage.set(initialLanguage);
    this.stateFacade.setCurrentLanguage(initialLanguage);

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string) => {
      this.currentLanguage.set(language);
      this.stateFacade.setCurrentLanguage(language);
    });

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap) => {
      const parkId: string | null = params.get('id');

      if (!parkId) {
        return;
      }

      if (parkId !== this.currentParkId) {
        this.currentParkId = parkId;
        this.stateFacade.loadData(parkId, this.currentLanguage());
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

  onBackClicked(): void {
    const backLink: string[] | null = this.pageView()?.backLink ?? null;

    if (backLink) {
      this.router.navigate(backLink);
      return;
    }

    this.router.navigate(['/', this.currentLanguage(), 'parks']);
  }

  onSearchChanged(value: string): void {
    this.searchTerm.set(value ?? '');
    this.updateQueryParams({
      search: this.searchTerm(),
      category: this.selectedCategory(),
      type: this.selectedType(),
      zone: this.selectedZoneId(),
      page: 1,
      size: this.pageSize()
    });
  }

  onCategoryChanged(value: string | null): void {
    this.selectedCategory.set(value);
    this.updateQueryParams({
      search: this.searchTerm(),
      category: this.selectedCategory(),
      type: this.selectedType(),
      zone: this.selectedZoneId(),
      page: 1,
      size: this.pageSize()
    });
  }

  onTypeChanged(value: string | null): void {
    this.selectedType.set(value);
    this.updateQueryParams({
      search: this.searchTerm(),
      category: this.selectedCategory(),
      type: this.selectedType(),
      zone: this.selectedZoneId(),
      page: 1,
      size: this.pageSize()
    });
  }

  onZoneChanged(value: string | null): void {
    this.selectedZoneId.set(value);
    this.updateQueryParams({
      search: this.searchTerm(),
      category: this.selectedCategory(),
      type: this.selectedType(),
      zone: this.selectedZoneId(),
      page: 1,
      size: this.pageSize()
    });
  }

  onZoneSelected(zoneId: string | null): void {
    this.selectedZoneId.set(zoneId);
    this.updateQueryParams({
      search: this.searchTerm(),
      category: this.selectedCategory(),
      type: this.selectedType(),
      zone: this.selectedZoneId(),
      page: 1,
      size: this.pageSize()
    });
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.selectedCategory.set(null);
    this.selectedType.set(null);
    this.selectedZoneId.set(null);

    this.updateQueryParams({
      search: null,
      category: null,
      type: null,
      zone: null,
      page: 1,
      size: 12
    });
  }

  onPageChanged(event: { page?: number; rows?: number }): void {
    const page: number = (event.page ?? 0) + 1;
    const rows: number = event.rows ?? this.pageSize();

    this.updateQueryParams({
      search: this.searchTerm(),
      category: this.selectedCategory(),
      type: this.selectedType(),
      zone: this.selectedZoneId(),
      page,
      size: rows
    });
  }

  private updateQueryParams(values: {
    search: string | null;
    category: string | null;
    type: string | null;
    zone: string | null;
    page: number;
    size: number;
  }): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: values.search && values.search.trim().length > 0 ? values.search : null,
        category: values.category,
        type: values.type,
        zone: values.zone,
        page: values.page > 1 ? values.page : null,
        size: values.size !== 12 ? values.size : null
      },
      queryParamsHandling: 'merge'
    });
  }
}
