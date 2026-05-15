import { ChangeDetectorRef, Component, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';
import { commitViewUpdate } from '@shared/utils/angular';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { InputText } from 'primeng/inputtext';
import { LocalizedRichTextEditorComponent } from '../../../shared/localized-rich-text-editor/localized-rich-text-editor.component';
import { ButtonDirective } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-admin-manufacturer-edit',
    templateUrl: './admin-manufacturer-edit.component.html',
    styleUrls: ['./admin-manufacturer-edit.component.scss'],
    imports: [Bind, Card, FormsModule, ReactiveFormsModule, InputText, LocalizedRichTextEditorComponent, ButtonDirective, TranslateModule]
})
export class AdminManufacturerEditComponent implements OnInit {
  form!: FormGroup;
  manufacturerId: string | null = null;
  isEditMode: boolean = false;
  currentLang: string = 'en';

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
      biography: [[]]
    });

    if (this.manufacturerId) {
      this.manufacturersApiService.getAttractionManufacturerById(this.manufacturerId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (manufacturer: AttractionManufacturer) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.form.patchValue({
              name: manufacturer.name,
              biography: manufacturer.biography ?? []
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
      biography: this.form.value.biography ?? []
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
