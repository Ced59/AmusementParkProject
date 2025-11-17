import { Component, OnInit } from '@angular/core';
import {FormBuilder, FormGroup, Validators} from "@angular/forms";
import {ActivatedRoute, Router} from "@angular/router";
import {ApiService} from "../../../../../services/api.service";
import {Park} from "../../../../../models/parks/park";


@Component({
  selector: 'app-admin-park-edit',
  templateUrl: './admin-park-edit.component.html',
  styleUrls: ['./admin-park-edit.component.scss']
})
export class AdminParkEditComponent implements OnInit {
  form!: FormGroup;

  isEditMode = false;
  parkId: string | null = null;

  countryOptions = [
    { code: 'FR', label: 'France' },
    { code: 'US', label: 'United States' },
    { code: 'JP', label: 'Japan' }
  ];

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly apiService: ApiService
  ) {}

  ngOnInit(): void {
    this.buildForm();

    this.parkId = this.route.snapshot.paramMap.get('idPark');
    this.isEditMode = !!this.parkId;

    if (this.isEditMode && this.parkId) {
      this.loadPark(this.parkId);
    }
  }

  private buildForm(): void {
    const initial: Park = {
      id: undefined,
      name: '',
      countryCode: '',
      latitude: 0,
      longitude: 0,
      isVisible: true
    };

    this.form = this.fb.group({
      id: [initial.id],
      name: [initial.name, Validators.required],
      countryCode: [initial.countryCode],
      latitude: [initial.latitude, Validators.required],
      longitude: [initial.longitude, Validators.required],
      isVisible: [initial.isVisible]
    });
  }

  private loadPark(id: string): void {
    this.apiService.getParkById(id).subscribe({
      next: (park: Park) => {
        // sécurité basique si l’API renvoie des null
        this.form.patchValue({
          id: park.id,
          name: park.name ?? '',
          countryCode: park.countryCode ?? '',
          latitude: park.latitude,
          longitude: park.longitude,
          isVisible: park.isVisible ?? true
        });
      },
      error: (err) => {
        console.error('Error loading park', err);
        this.navigateToList();
      }
    });
  }

  get nameControl() {
    return this.form.get('name')!;
  }
  get latitudeControl() {
    return this.form.get('latitude')!;
  }
  get longitudeControl() {
    return this.form.get('longitude')!;
  }
  get isVisibleControl() {
    return this.form.get('isVisible')!;
  }

  private buildPayload(): Park {
    const raw = this.form.value;

    return {
      id: raw.id,
      name: raw.name,
      countryCode: raw.countryCode || undefined,
      latitude: Number(raw.latitude),
      longitude: Number(raw.longitude),
      isVisible: raw.isVisible
    } as Park;
  }

  onSubmit(navigateBack: boolean = true): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = this.buildPayload();

    if (this.isEditMode && this.parkId) {
      this.apiService.updatePark(this.parkId, payload).subscribe({
        next: () => {
          if (navigateBack) {
            this.navigateToList();
          }
        },
        error: (err) => {
          console.error('Error updating park', err);
        }
      });
    } else {
      this.apiService.createPark(payload).subscribe({
        next: (created: Park) => {
          if (navigateBack) {
            this.navigateToList();
          } else if (created.id) {
            this.router.navigate(['../edit', created.id], { relativeTo: this.route });
          }
        },
        error: (err) => {
          console.error('Error creating park', err);
        }
      });
    }
  }

  onCancel(): void {
    this.navigateToList();
  }

  private navigateToList(): void {
    // retourne sur /:lang/admin/parks
    this.router.navigate(['../'], { relativeTo: this.route });
  }
}
