import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AttractionManufacturer } from '../../../../models/parks/attraction-manufacturer';
import { ApiService } from '../../../../services/api.service';
import { commitViewUpdate } from '../../../../utils/change-detection.utils';

@Component({
  selector: 'app-admin-manufacturer-edit',
  templateUrl: './admin-manufacturer-edit.component.html',
  styleUrls: ['./admin-manufacturer-edit.component.scss'],
  standalone: false
})
export class AdminManufacturerEditComponent implements OnInit {
  form!: FormGroup;
  manufacturerId: string | null = null;
  isEditMode: boolean = false;
  currentLang: string = 'en';

  constructor(
    private readonly fb: FormBuilder,
    private readonly apiService: ApiService,
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

    this.manufacturerId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.manufacturerId;

    this.form = this.fb.group({
      name: ['', Validators.required],
      biography: [[]]
    });

    if (this.manufacturerId) {
      this.apiService.getAttractionManufacturerById(this.manufacturerId).subscribe({
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
      this.apiService.updateAttractionManufacturer(this.manufacturerId, payload).subscribe({
        next: (updated: AttractionManufacturer) => {
          this.navigateAfterSave(updated.id);
        },
        error: (error: unknown) => {
          console.error('Error updating manufacturer', error);
        }
      });
      return;
    }

    this.apiService.createAttractionManufacturer(payload).subscribe({
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
