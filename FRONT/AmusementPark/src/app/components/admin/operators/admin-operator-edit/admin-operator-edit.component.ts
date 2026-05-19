import { ChangeDetectorRef, Component, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ParkOperatorsApiService } from '@data-access/parks/park-operators-api.service';
import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkOperator } from '@app/models/parks/park-operator';
import { commitViewUpdate } from '@shared/utils/angular';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { InputText } from 'primeng/inputtext';
import { LocalizedRichTextEditorComponent } from '../../../shared/localized-rich-text-editor/localized-rich-text-editor.component';
import { ButtonDirective } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-admin-operator-edit',
    templateUrl: './admin-operator-edit.component.html',
    styleUrls: ['./admin-operator-edit.component.scss'],
    imports: [Bind, Card, FormsModule, ReactiveFormsModule, InputText, LocalizedRichTextEditorComponent, ButtonDirective, TranslateModule]
})
export class AdminOperatorEditComponent implements OnInit {
  form!: FormGroup;
  operatorId: string | null = null;
  isEditMode: boolean = false;
  currentLang: string = 'en';

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
      description: [[]],
      adminReviewStatus: ['Validated' as AdminReviewStatus]
    });

    if (this.operatorId) {
      this.parkOperatorsApiService.getParkOperatorById(this.operatorId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (parkOperator: ParkOperator) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.form.patchValue({
              name: parkOperator.name,
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
