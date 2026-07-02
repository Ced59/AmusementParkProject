import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ParkOperator } from '@app/models/parks/park-operator';
import { ParkReferenceContactDetails } from '@app/models/parks/park-reference-contact-details';
import { ParkOperatorsApiService } from '@data-access/parks/park-operators-api.service';
import { commitViewUpdate } from '@shared/utils/angular';
import { LocalizedRichTextEditorComponent } from '@shared/components/localized-rich-text-editor/localized-rich-text-editor.component';
import { AdminReferenceImagesComponent } from '@features/admin/shared/ui/admin-reference-images/admin-reference-images.component';
import { AdminButtonDirective } from '@features/admin/shared/ui/admin-button/admin-button.directive';
import { AdminCardComponent } from '@features/admin/shared/ui/admin-card/admin-card.component';

@Component({
  selector: 'app-admin-operator-edit',
  templateUrl: './admin-operator-edit.component.html',
  styleUrls: ['./admin-operator-edit.component.scss'],
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
export class AdminOperatorEditComponent implements OnInit {
  form!: FormGroup;
  operatorId: string | null = null;
  isEditMode: boolean = false;
  currentLang: string = 'en';

  protected readonly imageOwnerType: ImageOwnerType = ImageOwnerType.PARK_OPERATOR;
  protected readonly imageCategory: ImageCategory = ImageCategory.LOGO;
  protected readonly imageCategoryOptions: ImageCategory[] = [ImageCategory.LOGO, ImageCategory.OPERATOR];

  constructor(
    private readonly fb: FormBuilder,
    private readonly parkOperatorsApiService: ParkOperatorsApiService,
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

    this.operatorId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.operatorId;

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
      description: [[]],
      adminReviewStatus: ['Validated' as AdminReviewStatus]
    });

    if (this.operatorId) {
      this.parkOperatorsApiService.getParkOperatorById(this.operatorId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (parkOperator: ParkOperator) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.form.patchValue({
              name: parkOperator.name,
              legalName: parkOperator.legalName ?? null,
              foundedYear: parkOperator.foundedYear ?? null,
              closedYear: parkOperator.closedYear ?? null,
              contactDetails: parkOperator.contactDetails ?? {},
              description: parkOperator.description ?? [],
              adminReviewStatus: parkOperator.adminReviewStatus ?? 'ToReview'
            });
          });
        },
        error: (error: unknown) => {
          console.error('Error loading operator', error);
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

    const payload: ParkOperator = {
      name: this.form.value.name,
      legalName: this.toOptionalText(this.form.value.legalName),
      foundedYear: this.toOptionalNumber(this.form.value.foundedYear),
      closedYear: this.toOptionalNumber(this.form.value.closedYear),
      contactDetails: this.buildContactDetails(),
      description: this.form.value.description ?? [],
      adminReviewStatus: this.form.value.adminReviewStatus ?? 'Validated'
    };

    if (this.isEditMode && this.operatorId) {
      this.parkOperatorsApiService.updateParkOperator(this.operatorId, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (updated: ParkOperator) => {
          this.navigateAfterSave(updated.id);
        },
        error: (error: unknown) => {
          console.error('Error updating operator', error);
        }
      });
      return;
    }

    this.parkOperatorsApiService.createParkOperator(payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (created: ParkOperator) => {
        this.navigateAfterSave(created.id);
      },
      error: (error: unknown) => {
        console.error('Error creating operator', error);
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
      this.router.navigateByUrl(`${returnUrl}${separator}operatorId=${createdId ?? ''}${tabQuery}`);
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
    this.router.navigate(['/', this.currentLang, 'admin', 'operators']);
  }
}
