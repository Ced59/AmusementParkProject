import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, effect } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Card } from 'primeng/card';
import { Checkbox } from 'primeng/checkbox';
import { InputText } from 'primeng/inputtext';
import { PrimeTemplate } from 'primeng/api';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';

import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { AdminSeoSitemapsStateFacade } from '@features/admin/seo-sitemaps/state/admin-seo-sitemaps-state.facade';
import { ScrollAnchorService } from '@shared/services/scroll/scroll-anchor.service';
import {
  SeoSitemapGenerationHistory,
  SeoSitemapGenerationStatus,
  SeoSitemapSectionStats,
  UpdateSeoSitemapSettingsRequest
} from '@app/models/admin/seo/seo-sitemap.models';

interface SeoSitemapSettingsForm {
  isIndexNowEnabled: FormControl<boolean>;
  submitToIndexNowAfterManualGeneration: FormControl<boolean>;
  submitToIndexNowAfterAutomaticGeneration: FormControl<boolean>;
  indexNowKey: FormControl<string>;
  indexNowKeyLocation: FormControl<string>;
  indexNowEndpoints: FormControl<string>;
}

@Component({
  selector: 'app-admin-seo-sitemaps',
  templateUrl: './admin-seo-sitemaps.component.html',
  styleUrls: ['./admin-seo-sitemaps.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminSeoSitemapsStateFacade],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    ButtonDirective,
    Card,
    Checkbox,
    InputText,
    PrimeTemplate,
    TableModule,
    Tag,
    PageStateComponent
  ]
})
export class AdminSeoSitemapsComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly loading = this.stateFacade.loading;
  protected readonly overview = this.stateFacade.overview;
  protected readonly history = this.stateFacade.history;
  protected readonly totalRecords = this.stateFacade.totalRecords;
  protected readonly currentPage = this.stateFacade.currentPage;
  protected readonly pageSize = this.stateFacade.pageSize;
  protected readonly savingSettings = this.stateFacade.savingSettings;
  protected readonly generating = this.stateFacade.generating;
  protected readonly lastGeneration = this.stateFacade.lastGeneration;
  protected readonly canSubmitIndexNow = computed(() => !!this.overview()?.settings.isIndexNowEnabled);

  protected readonly settingsForm = new FormGroup<SeoSitemapSettingsForm>({
    isIndexNowEnabled: new FormControl<boolean>(false, { nonNullable: true }),
    submitToIndexNowAfterManualGeneration: new FormControl<boolean>(false, { nonNullable: true }),
    submitToIndexNowAfterAutomaticGeneration: new FormControl<boolean>(false, { nonNullable: true }),
    indexNowKey: new FormControl<string>('', { nonNullable: true }),
    indexNowKeyLocation: new FormControl<string>('', { nonNullable: true }),
    indexNowEndpoints: new FormControl<string>('https://api.indexnow.org/indexnow\nhttps://www.bing.com/indexnow', { nonNullable: true })
  });

  constructor(
    private readonly stateFacade: AdminSeoSitemapsStateFacade,
    private readonly scrollAnchorService: ScrollAnchorService
  ) {
    effect(() => {
      const settings = this.overview()?.settings;
      if (!settings) {
        return;
      }

      this.settingsForm.setValue({
        isIndexNowEnabled: settings.isIndexNowEnabled,
        submitToIndexNowAfterManualGeneration: settings.submitToIndexNowAfterManualGeneration,
        submitToIndexNowAfterAutomaticGeneration: settings.submitToIndexNowAfterAutomaticGeneration,
        indexNowKey: settings.indexNowKey ?? '',
        indexNowKeyLocation: settings.indexNowKeyLocation ?? '',
        indexNowEndpoints: (settings.indexNowEndpoints ?? []).join('\n')
      }, { emitEvent: false });
    });
  }

  ngOnInit(): void {
    this.stateFacade.load();
  }

  protected saveSettings(): void {
    const value = this.settingsForm.getRawValue();
    const request: UpdateSeoSitemapSettingsRequest = {
      isIndexNowEnabled: value.isIndexNowEnabled,
      submitToIndexNowAfterManualGeneration: value.submitToIndexNowAfterManualGeneration,
      submitToIndexNowAfterAutomaticGeneration: value.submitToIndexNowAfterAutomaticGeneration,
      indexNowKey: this.normalizeOptionalText(value.indexNowKey),
      indexNowKeyLocation: this.normalizeOptionalText(value.indexNowKeyLocation),
      indexNowEndpoints: value.indexNowEndpoints
        .split(/\r?\n/)
        .map((endpoint: string): string => endpoint.trim())
        .filter((endpoint: string): boolean => endpoint.length > 0)
    };

    this.stateFacade.saveSettings(request);
  }

  protected generate(submitToIndexNow: boolean): void {
    this.stateFacade.generate(submitToIndexNow);
  }

  protected refresh(): void {
    this.stateFacade.load(this.currentPage(), this.pageSize());
  }

  protected onHistoryPageChanged(event: TableLazyLoadEvent): void {
    const rows: number = event.rows ?? this.pageSize();
    const first: number = event.first ?? 0;
    const page: number = Math.floor(first / rows) + 1;
    const shouldScroll: boolean = page !== this.currentPage() || rows !== this.pageSize();
    this.stateFacade.load(page, rows);

    if (shouldScroll) {
      this.scrollAnchorService.scrollToSelector('[data-pagination-scroll-target="admin-seo-sitemap-history"]');
    }
  }

  protected sectionTrackBy(_: number, section: SeoSitemapSectionStats): string {
    return section.key;
  }

  protected historyTrackBy(_: number, item: SeoSitemapGenerationHistory): string {
    return item.id;
  }

  protected statusSeverity(status: SeoSitemapGenerationStatus | string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    switch (status) {
      case 'Succeeded':
        return 'success';
      case 'Running':
        return 'info';
      case 'Skipped':
        return 'warn';
      case 'Failed':
        return 'danger';
      default:
        return 'secondary';
    }
  }

  protected indexNowSeverity(isSuccess: boolean, wasRequested: boolean): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    if (!wasRequested) {
      return 'secondary';
    }

    return isSuccess ? 'success' : 'warn';
  }

  private normalizeOptionalText(value: string | null | undefined): string | null {
    if (!value || value.trim().length === 0) {
      return null;
    }

    return value.trim();
  }
}
