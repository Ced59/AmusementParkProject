import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ParkFoundersApiService } from '@data-access/parks/park-founders-api.service';
import { ParkFounder } from '@app/models/parks/park-founder';
import { commitViewUpdate } from '@app/utils/change-detection.utils';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { InputText } from 'primeng/inputtext';
import { LocalizedRichTextEditorComponent } from '../../../shared/localized-rich-text-editor/localized-rich-text-editor.component';
import { ButtonDirective } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-admin-founder-edit',
    templateUrl: './admin-founder-edit.component.html',
    styleUrls: ['./admin-founder-edit.component.scss'],
    imports: [Bind, Card, FormsModule, ReactiveFormsModule, InputText, LocalizedRichTextEditorComponent, ButtonDirective, TranslateModule]
})
export class AdminFounderEditComponent implements OnInit {
  form!: FormGroup;
  founderId: string | null = null;
  isEditMode: boolean = false;
  currentLang: string = 'en';

  constructor(
    private readonly fb: FormBuilder,
    private readonly parkFoundersApiService: ParkFoundersApiService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef
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
      biography: [[]]
    });

    if (this.founderId) {
      this.parkFoundersApiService.getParkFounderById(this.founderId).subscribe({
        next: (founder: ParkFounder) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.form.patchValue({
              name: founder.name,
              biography: founder.biography ?? []
            });
          });
        },
        error: (error: unknown) => {
          console.error('Error loading founder', error);
          this.navigateToParks();
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
      biography: this.form.value.biography ?? []
    };

    if (this.isEditMode && this.founderId) {
      this.parkFoundersApiService.updateParkFounder(this.founderId, payload).subscribe({
        next: (updated: ParkFounder) => {
          this.navigateAfterSave(updated.id);
        },
        error: (error: unknown) => {
          console.error('Error updating founder', error);
        }
      });
      return;
    }

    this.parkFoundersApiService.createParkFounder(payload).subscribe({
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

  private navigateAfterSave(createdId: string | undefined): void {
    const returnUrl: string | null = this.route.snapshot.queryParamMap.get('returnUrl');
    const returnTab: string | null = this.route.snapshot.queryParamMap.get('returnTab');

    if (returnUrl) {
      const separator: string = returnUrl.includes('?') ? '&' : '?';
      const tabQuery: string = returnTab ? `&tab=${returnTab}` : '';
      this.router.navigateByUrl(`${returnUrl}${separator}founderId=${createdId ?? ''}${tabQuery}`);
      return;
    }

    this.navigateToParks();
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

    this.navigateToParks();
  }

  private navigateToParks(): void {
    this.router.navigate(['/', this.currentLang, 'admin', 'parks']);
  }
}
