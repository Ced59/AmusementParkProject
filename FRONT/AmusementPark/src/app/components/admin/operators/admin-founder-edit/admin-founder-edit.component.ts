import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {ApiService} from "../../../../services/api.service";
import {ParkFounder} from "../../../../models/parks/park-founder";

@Component({
    selector: 'app-admin-founder-edit',
    templateUrl: './admin-founder-edit.component.html',
    styleUrls: ['./admin-founder-edit.component.scss'],
    standalone: false
})
export class AdminFounderEditComponent implements OnInit {
  form!: FormGroup;
  founderId: string | null = null;
  isEditMode: boolean = false;
  currentLang: string = 'en';

  constructor(
    private readonly fb: FormBuilder,
    private readonly apiService: ApiService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
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
      this.apiService.getParkFounderById(this.founderId).subscribe({
        next: (founder: ParkFounder) => {
          this.form.patchValue({
            name: founder.name,
            biography: founder.biography ?? []
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
      this.apiService.updateParkFounder(this.founderId, payload).subscribe({
        next: (updated: ParkFounder) => {
          this.navigateAfterSave(updated.id);
        },
        error: (error: unknown) => {
          console.error('Error updating founder', error);
        }
      });
      return;
    }

    this.apiService.createParkFounder(payload).subscribe({
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
