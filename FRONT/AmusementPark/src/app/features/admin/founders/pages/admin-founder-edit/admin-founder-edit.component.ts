import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ParkFounder } from '@app/models/parks/park-founder';
import { ParkFoundersApiService } from '@data-access/parks/park-founders-api.service';
import { commitViewUpdate } from '@shared/utils/angular';
import { LocalizedRichTextEditorComponent } from '@shared/components/localized-rich-text-editor/localized-rich-text-editor.component';
import { AdminReferenceImagesComponent } from '@features/admin/shared/ui/admin-reference-images/admin-reference-images.component';
import { AdminButtonDirective } from '@features/admin/shared/ui/admin-button/admin-button.directive';
import { AdminCardComponent } from '@features/admin/shared/ui/admin-card/admin-card.component';

@Component({
  selector: 'app-admin-founder-edit',
  templateUrl: './admin-founder-edit.component.html',
  styleUrls: ['./admin-founder-edit.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    AdminCardComponent,
    FormsModule,
    ReactiveFormsModule,
    LocalizedRichTextEditorComponent,
    AdminButtonDirective,
    TranslateModule,
    AdminReferenceImagesComponent
  ]
})
export class AdminFounderEditComponent implements OnInit {
  form!: FormGroup;
  founderId: string | null = null;
  isEditMode: boolean = false;
  currentLang: string = 'en';

  protected readonly imageOwnerType: ImageOwnerType = ImageOwnerType.PARK_FOUNDER;
  protected readonly imageCategory: ImageCategory = ImageCategory.LOGO;
  protected readonly imageCategoryOptions: ImageCategory[] = [ImageCategory.LOGO, ImageCategory.FOUNDER];

  constructor(
    private readonly fb: FormBuilder,
    private readonly parkFoundersApiService: ParkFoundersApiService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.currentLang =
      this.route.root.firstChild?.snapshot.params['lang'] ??
      this.route.snapshot.params['lang'] ??
      'en';

    this.founderId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.founderId;

    this.form = this.fb.group({
      name: ['', Validators.required],
      occupation: [null],
      birthDate: [null],
      deathDate: [null],
      birthPlace: [null],
      nationalityCountryCode: [null],
      websiteUrl: [null],
      biography: [[]]
    });

    if (this.founderId) {
      this.parkFoundersApiService.getParkFounderById(this.founderId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (founder: ParkFounder) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.form.patchValue({
              name: founder.name,
              occupation: founder.occupation ?? null,
              birthDate: founder.birthDate ?? null,
              deathDate: founder.deathDate ?? null,
              birthPlace: founder.birthPlace ?? null,
              nationalityCountryCode: founder.nationalityCountryCode ?? null,
              websiteUrl: founder.websiteUrl ?? null,
              biography: founder.biography ?? []
            });
          });
        },
        error: (error: unknown) => {
          console.error('Error loading founder', error);
          this.navigateToFounders();
        }
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload: ParkFounder = {
      name: this.form.value.name,
      occupation: this.toOptionalText(this.form.value.occupation),
      birthDate: this.toOptionalText(this.form.value.birthDate),
      deathDate: this.toOptionalText(this.form.value.deathDate),
      birthPlace: this.toOptionalText(this.form.value.birthPlace),
      nationalityCountryCode: this.toOptionalText(this.form.value.nationalityCountryCode)?.toUpperCase() ?? null,
      websiteUrl: this.toOptionalText(this.form.value.websiteUrl),
      biography: this.form.value.biography ?? []
    };

    if (this.isEditMode && this.founderId) {
      this.parkFoundersApiService.updateParkFounder(this.founderId, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (updated: ParkFounder) => {
          this.navigateAfterSave(updated.id);
        },
        error: (error: unknown) => {
          console.error('Error updating founder', error);
        }
      });
      return;
    }

    this.parkFoundersApiService.createParkFounder(payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (created: ParkFounder) => {
        this.navigateAfterSave(created.id);
      },
      error: (error: unknown) => {
        console.error('Error creating founder', error);
      }
    });
  }

  onCancel(): void {
    this.navigateBackToOrigin();
  }

  private toOptionalText(value: unknown): string | null {
    const normalizedValue: string = typeof value === 'string' ? value.trim() : '';
    return normalizedValue.length > 0 ? normalizedValue : null;
  }

  private navigateAfterSave(createdId: string | undefined): void {
    const returnUrl: string | null = this.route.snapshot.queryParamMap.get('returnUrl');
    const returnTab: string | null = this.route.snapshot.queryParamMap.get('returnTab');

    if (returnUrl) {
      const separator: string = returnUrl.includes('?') ? '&' : '?';
      const tabQuery: string = returnTab ? `&tab=${returnTab}` : '';
      this.router.navigateByUrl(`${returnUrl}${separator}founderId=${createdId ?? ''}${tabQuery}`);
      return;
    }

    this.navigateToFounders();
  }

  private navigateBackToOrigin(): void {
    const returnUrl: string | null = this.route.snapshot.queryParamMap.get('returnUrl');
    const returnTab: string | null = this.route.snapshot.queryParamMap.get('returnTab');

    if (returnUrl) {
      const separator: string = returnUrl.includes('?') ? '&' : '?';
      const tabQuery: string = returnTab ? `${separator}tab=${returnTab}` : '';
      this.router.navigateByUrl(`${returnUrl}${tabQuery}`);
      return;
    }

    this.navigateToFounders();
  }

  private navigateToFounders(): void {
    this.router.navigate(['/', this.currentLang, 'admin', 'founders']);
  }
}
