import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { Card } from '@shared/ui/primitives/card';
import { InputText } from '@shared/ui/primitives/inputtext';

import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { TechnicalContentBlock, TechnicalPage, TechnicalPageAlias } from '@app/models/technical-pages/technical-page';
import { TechnicalPagesApiService } from '@data-access/technical-pages/technical-pages-api.service';
import { LocalizedTextInputComponent } from '@shared/components/localized-text-input/localized-text-input.component';

@Component({
  selector: 'app-admin-technical-page-edit',
  templateUrl: './admin-technical-page-edit.component.html',
  styleUrls: ['./admin-technical-page-edit.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TranslateModule,
    ButtonDirective,
    Card,
    InputText,
    LocalizedTextInputComponent
  ]
})
export class AdminTechnicalPageEditComponent implements OnInit {
  form!: FormGroup;
  protected currentLang: string = 'en';
  protected pageId: string | null = null;
  protected isEditMode: boolean = false;
  protected jsonError: string | null = null;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly technicalPagesApiService: TechnicalPagesApiService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.currentLang = this.route.root.firstChild?.snapshot.params['lang'] ?? this.route.snapshot.params['lang'] ?? 'en';
    this.pageId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.pageId;

    this.form = this.formBuilder.group({
      categoryKey: ['', Validators.required],
      categoryNames: [[]],
      slug: ['', Validators.required],
      titles: [[]],
      summaries: [[]],
      sortOrder: [0],
      isVisible: [true],
      adminReviewStatus: ['Validated' as AdminReviewStatus],
      aliasesJson: ['[]'],
      contentBlocksJson: ['[]']
    });

    if (this.pageId) {
      this.technicalPagesApiService.getById(this.pageId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (page: TechnicalPage): void => this.patchForm(page),
          error: (): void => this.navigateToList()
        });
    }
  }

  protected submit(): void {
    this.jsonError = null;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const aliases: TechnicalPageAlias[] | null = this.parseJsonArray<TechnicalPageAlias>(this.form.value.aliasesJson);
    const contentBlocks: TechnicalContentBlock[] | null = this.parseJsonArray<TechnicalContentBlock>(this.form.value.contentBlocksJson);
    if (aliases === null || contentBlocks === null) {
      this.jsonError = 'admin.technicalPages.edit.invalidJson';
      return;
    }

    const page: TechnicalPage = {
      id: this.pageId,
      categoryKey: this.form.value.categoryKey,
      categoryNames: this.form.value.categoryNames ?? [],
      slug: this.form.value.slug,
      titles: this.form.value.titles ?? [],
      summaries: this.form.value.summaries ?? [],
      aliases,
      contentBlocks,
      sortOrder: Number(this.form.value.sortOrder ?? 0),
      isVisible: !!this.form.value.isVisible,
      adminReviewStatus: this.form.value.adminReviewStatus ?? 'Validated'
    };

    if (this.isEditMode && this.pageId) {
      this.technicalPagesApiService.update(this.pageId, page)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (): void => this.navigateToList(),
          error: (): void => {
            this.jsonError = 'admin.technicalPages.edit.saveFailed';
          }
        });
      return;
    }

    this.technicalPagesApiService.create(page)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (): void => this.navigateToList(),
        error: (): void => {
          this.jsonError = 'admin.technicalPages.edit.saveFailed';
        }
      });
  }

  protected cancel(): void {
    this.navigateToList();
  }

  private patchForm(page: TechnicalPage): void {
    this.form.patchValue({
      categoryKey: page.categoryKey,
      categoryNames: page.categoryNames ?? [],
      slug: page.slug,
      titles: page.titles ?? [],
      summaries: page.summaries ?? [],
      sortOrder: page.sortOrder ?? 0,
      isVisible: page.isVisible,
      adminReviewStatus: page.adminReviewStatus ?? 'Validated',
      aliasesJson: JSON.stringify(page.aliases ?? [], null, 2),
      contentBlocksJson: JSON.stringify(page.contentBlocks ?? [], null, 2)
    });
  }

  private parseJsonArray<T>(value: unknown): T[] | null {
    try {
      const parsed: unknown = JSON.parse(typeof value === 'string' ? value : '[]');
      return Array.isArray(parsed) ? parsed as T[] : null;
    } catch {
      return null;
    }
  }

  private navigateToList(): void {
    this.router.navigate(['/', this.currentLang, 'admin', 'technical-pages']);
  }
}
