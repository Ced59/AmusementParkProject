import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from '@shared/primeless/button';
import { Card } from '@shared/primeless/card';
import { Tag } from '@shared/primeless/tag';

import { AdminReviewStatus, getAdminReviewStatusSeverity, getAdminReviewStatusTranslationKey } from '@app/models/admin/admin-review-status';
import { TechnicalPage, TechnicalPagesJsonUpsert } from '@app/models/technical-pages/technical-page';
import { TechnicalPagesApiService } from '@data-access/technical-pages/technical-pages-api.service';
import { resolveLocalizedText } from '@shared/utils/localization';

@Component({
  selector: 'app-admin-technical-pages',
  templateUrl: './admin-technical-pages.component.html',
  styleUrls: ['./admin-technical-pages.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, RouterLink, TranslateModule, ButtonDirective, Card, Tag]
})
export class AdminTechnicalPagesComponent implements OnInit {
  protected readonly pages = signal<TechnicalPage[]>([]);
  protected readonly loading = signal<boolean>(true);
  protected readonly importJson = signal<string>('');
  protected readonly importMessage = signal<string | null>(null);
  protected readonly importError = signal<string | null>(null);
  protected currentLang: string = 'en';

  constructor(
    private readonly technicalPagesApiService: TechnicalPagesApiService,
    private readonly route: ActivatedRoute,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.currentLang = this.route.root.firstChild?.snapshot.params['lang'] ?? this.route.snapshot.params['lang'] ?? 'en';
    this.loadPages();
  }

  protected title(page: TechnicalPage): string {
    return resolveLocalizedText(page.titles, this.currentLang, page.slug);
  }

  protected categoryName(page: TechnicalPage): string {
    return resolveLocalizedText(page.categoryNames, this.currentLang, page.categoryKey);
  }

  protected getStatusSeverity(status: AdminReviewStatus | null | undefined): 'success' | 'info' | 'warn' | 'danger' {
    return getAdminReviewStatusSeverity(status);
  }

  protected getStatusLabelKey(status: AdminReviewStatus | null | undefined): string {
    return getAdminReviewStatusTranslationKey(status);
  }

  protected onImportJsonChanged(value: string): void {
    this.importJson.set(value);
  }

  protected upsertJson(): void {
    this.importMessage.set(null);
    this.importError.set(null);

    const request: TechnicalPagesJsonUpsert | null = this.parseImportJson(this.importJson());
    if (request === null) {
      this.importError.set('admin.technicalPages.import.invalidJson');
      return;
    }

    this.technicalPagesApiService.upsertJson(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (): void => {
          this.importMessage.set('admin.technicalPages.import.success');
          this.importJson.set('');
          this.loadPages();
        },
        error: (): void => {
          this.importError.set('admin.technicalPages.import.failed');
        }
      });
  }

  private loadPages(): void {
    this.loading.set(true);
    this.technicalPagesApiService.getAllAdminPages()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (pages: TechnicalPage[]): void => {
          this.pages.set(pages);
          this.loading.set(false);
        },
        error: (): void => {
          this.pages.set([]);
          this.loading.set(false);
        }
      });
  }

  private parseImportJson(value: string): TechnicalPagesJsonUpsert | null {
    try {
      const parsed: unknown = JSON.parse(value);
      if (Array.isArray(parsed)) {
        return { pages: parsed as TechnicalPage[] };
      }

      if (this.hasPagesProperty(parsed)) {
        return parsed;
      }

      return null;
    } catch {
      return null;
    }
  }

  private hasPagesProperty(value: unknown): value is TechnicalPagesJsonUpsert {
    return typeof value === 'object'
      && value !== null
      && Array.isArray((value as { pages?: unknown }).pages);
  }
}
