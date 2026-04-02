import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {ApiService} from "../../../../services/api.service";
import {ParkOperator} from "../../../../models/parks/park-operator";


@Component({
    selector: 'app-admin-operator-edit',
    templateUrl: './admin-operator-edit.component.html',
    styleUrls: ['./admin-operator-edit.component.scss'],
    standalone: false
})
export class AdminOperatorEditComponent implements OnInit {
  form!: FormGroup;
  operatorId: string | null = null;
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

    this.operatorId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.operatorId;

    this.form = this.fb.group({
      name: ['', Validators.required],
      description: [[]]
    });

    if (this.operatorId) {
      this.apiService.getParkOperatorById(this.operatorId).subscribe({
        next: (parkOperator: ParkOperator) => {
          this.form.patchValue({
            name: parkOperator.name,
            description: parkOperator.description ?? []
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
      description: this.form.value.description ?? []
    };

    if (this.isEditMode && this.operatorId) {
      this.apiService.updateParkOperator(this.operatorId, payload).subscribe({
        next: (updated: ParkOperator) => {
          this.navigateAfterSave(updated.id);
        },
        error: (error: unknown) => {
          console.error('Error updating operator', error);
        }
      });
      return;
    }

    this.apiService.createParkOperator(payload).subscribe({
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
