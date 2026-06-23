import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Card } from 'primeng/card';
import { InputText } from 'primeng/inputtext';

import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { ParkReferenceContactDetails } from '@app/models/parks/park-reference-contact-details';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';
import { commitViewUpdate } from '@shared/utils/angular';
import { Bind } from 'primeng/bind';
import { LocalizedRichTextEditorComponent } from '@shared/components/localized-rich-text-editor/localized-rich-text-editor.component';
import { AdminReferenceImagesComponent } from '@features/admin/shared/ui/admin-reference-images/admin-reference-images.component';

@Component({
  selector: 'app-admin-manufacturer-edit',
  templateUrl: './admin-manufacturer-edit.component.html',
  styleUrls: ['./admin-manufacturer-edit.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    Bind,
    Card,
    FormsModule,
    ReactiveFormsModule,
    InputText,
    LocalizedRichTextEditorComponent,
    ButtonDirective,
    TranslateModule,
    AdminReferenceImagesComponent
  ]
})
export class AdminManufacturerEditComponent implements OnInit {
  form!: FormGroup;
  manufacturerId: string | null = null;
  isEditMode: boolean = false;
  currentLang: string = 'en';

  protected readonly imageOwnerType: ImageOwnerType = ImageOwnerType.ATTRACTION_MANUFACTURER;
  protected readonly imageCategory: ImageCategory = ImageCategory.LOGO;
  protected readonly imageCategoryOptions: ImageCategory[] = [ImageCategory.LOGO, ImageCategory.MANUFACTURER];

  constructor(
    private readonly fb: FormBuilder,
    private readonly manufacturersApiService: ManufacturersApiService,
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

    this.manufacturerId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.manufacturerId;

    this.form = this.fb.group({
      name: ['', Validators.required],
      legalName: [null],
      foundedYear: [null],
      closedYear: [null],
      contactDetails: this.fb.group({
        websiteUrl: [null],
        email: [null],
        phoneNumber: [null],
        street: [null],
        city: [null],
        postalCode: [null],
        countryCode: [null],
        latitude: [null],
        longitude: [null]
      }),
      biography: [[]],
      isVisible: [true],
      adminReviewStatus: ['Validated' as AdminReviewStatus]
    });

    if (this.manufacturerId) {
      this.manufacturersApiService.getAttractionManufacturerById(this.manufacturerId, true).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (manufacturer: AttractionManufacturer) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.form.patchValue({
              name: manufacturer.name,
              legalName: manufacturer.legalName ?? null,
              foundedYear: manufacturer.foundedYear ?? null,
              closedYear: manufacturer.closedYear ?? null,
              contactDetails: manufacturer.contactDetails ?? {},
              biography: manufacturer.biography ?? [],
              isVisible: manufacturer.isVisible ?? true,
              adminReviewStatus: manufacturer.adminReviewStatus ?? 'ToReview'
            });
          });
        },
        error: (error: unknown) => {
          console.error('Error loading manufacturer', error);
          this.navigateToList();
        }
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload: AttractionManufacturer = {
      name: this.form.value.name,
      legalName: this.toOptionalText(this.form.value.legalName),
      foundedYear: this.toOptionalNumber(this.form.value.foundedYear),
      closedYear: this.toOptionalNumber(this.form.value.closedYear),
      contactDetails: this.buildContactDetails(),
      biography: this.form.value.biography ?? [],
      isVisible: this.form.value.isVisible !== false,
      adminReviewStatus: this.form.value.adminReviewStatus ?? 'Validated'
    };

    if (this.isEditMode && this.manufacturerId) {
      this.manufacturersApiService.updateAttractionManufacturer(this.manufacturerId, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (updated: AttractionManufacturer) => {
          this.navigateAfterSave(updated.id);
        },
        error: (error: unknown) => {
          console.error('Error updating manufacturer', error);
        }
      });
      return;
    }

    this.manufacturersApiService.createAttractionManufacturer(payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (created: AttractionManufacturer) => {
        this.navigateAfterSave(created.id);
      },
      error: (error: unknown) => {
        console.error('Error creating manufacturer', error);
      }
    });
  }

  onCancel(): void {
    this.navigateBackToOrigin();
  }

  private buildContactDetails(): ParkReferenceContactDetails | null {
    const raw: ParkReferenceContactDetails = this.form.value.contactDetails ?? {};
    const contactDetails: ParkReferenceContactDetails = {
      websiteUrl: this.toOptionalText(raw.websiteUrl),
      email: this.toOptionalText(raw.email),
      phoneNumber: this.toOptionalText(raw.phoneNumber),
      street: this.toOptionalText(raw.street),
      city: this.toOptionalText(raw.city),
      postalCode: this.toOptionalText(raw.postalCode),
      countryCode: this.toOptionalText(raw.countryCode)?.toUpperCase() ?? null,
      latitude: this.toOptionalNumber(raw.latitude),
      longitude: this.toOptionalNumber(raw.longitude)
    };

    const hasValue: boolean = Object.values(contactDetails).some((value: string | number | null | undefined): boolean => value !== null && value !== undefined && value !== '');
    return hasValue ? contactDetails : null;
  }

  private toOptionalText(value: unknown): string | null {
    const normalizedValue: string = typeof value === 'string' ? value.trim() : '';
    return normalizedValue.length > 0 ? normalizedValue : null;
  }

  private toOptionalNumber(value: unknown): number | null {
    if (value === null || value === undefined || value === '') {
      return null;
    }

    const numericValue: number = Number(value);
    return Number.isFinite(numericValue) ? numericValue : null;
  }

  private navigateAfterSave(createdId: string | undefined): void {
    const returnUrl: string | null = this.route.snapshot.queryParamMap.get('returnUrl');
    const returnTab: string | null = this.route.snapshot.queryParamMap.get('returnTab');

    if (returnUrl) {
      const separator: string = returnUrl.includes('?') ? '&' : '?';
      const tabQuery: string = returnTab ? `&tab=${returnTab}` : '';
      this.router.navigateByUrl(`${returnUrl}${separator}manufacturerId=${createdId ?? ''}${tabQuery}`);
      return;
    }

    this.navigateToList();
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

    this.navigateToList();
  }

  private navigateToList(): void {
    this.router.navigate(['/', this.currentLang, 'admin', 'manufacturers']);
  }
}
