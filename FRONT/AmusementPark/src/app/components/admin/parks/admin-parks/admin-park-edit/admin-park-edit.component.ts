import { Component, OnInit } from '@angular/core';
import {FormBuilder, FormGroup, Validators} from "@angular/forms";
import {ActivatedRoute, Router} from "@angular/router";
import {ApiService} from "../../../../../services/api.service";
import {Park} from "../../../../../models/parks/park";
import {CountryDto} from "../../../../../models/countries/country-dto";

@Component({
  selector: 'app-admin-park-edit',
  templateUrl: './admin-park-edit.component.html',
  styleUrls: ['./admin-park-edit.component.scss']
})
export class AdminParkEditComponent implements OnInit {
  form!: FormGroup;

  isEditMode = false;
  parkId: string | null = null;

  // 🔹 options pour le dropdown pays
  countryOptions: { code: string; label: string }[] = [];
  countriesLoading = false;

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

    // Charger la liste des pays (en langue courante)
    this.loadCountries();

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

  private loadCountries(): void {
    this.countriesLoading = true;

    // Récupère la langue depuis la route racine `:lang`
    // /:lang/admin/parks/...
    const root = this.route.root;
    const lang =
      root.firstChild?.snapshot.params['lang'] ??
      this.route.snapshot.params['lang'] ??
      'en';

    this.apiService.getCountries(lang).subscribe({
      next: (countries: CountryDto[]) => {
        this.countryOptions = countries.map(c => ({
          code: c.isoCode,
          label: c.name
        }));
        this.countriesLoading = false;
      },
      error: err => {
        console.error('Error loading countries', err);
        this.countryOptions = [];
        this.countriesLoading = false;
      }
    });
  }

  private loadPark(id: string): void {
    this.apiService.getParkById(id).subscribe({
      next: (park: Park) => {
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
  get countryCodeControl() {
    return this.form.get('countryCode')!;
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
    this.router.navigate(['../'], { relativeTo: this.route });
  }
}
