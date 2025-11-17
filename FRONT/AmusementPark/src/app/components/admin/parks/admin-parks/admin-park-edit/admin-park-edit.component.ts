import { Component, OnDestroy, OnInit } from '@angular/core';
import {FormBuilder, FormGroup, Validators} from "@angular/forms";
import {Subscription} from "rxjs";
import {ActivatedRoute, Router} from "@angular/router";
import {ApiService} from "../../../../../services/api.service";
import {Park} from "../../../../../models/parks/park";
import {CountryDto} from "../../../../../models/countries/country-dto";

interface MapMarker {
  id: string;
  lat: number;
  lng: number;
  draggable?: boolean;
}

@Component({
  selector: 'app-admin-park-edit',
  templateUrl: './admin-park-edit.component.html',
  styleUrls: ['./admin-park-edit.component.scss']
})
export class AdminParkEditComponent implements OnInit, OnDestroy {

  form!: FormGroup;

  isEditMode = false;
  parkId: string | null = null;

  countryOptions: { code: string; label: string }[] = [];
  countriesLoading = false;

  // Carte
  mapCenter: [number, number] = [48.8566, 2.3522]; // Paris par défaut
  mapZoom = 16;
  mapMarkers: MapMarker[] = [];

  private subscriptions = new Subscription();

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

    this.loadCountries();

    this.setupFormMapSync();

    if (this.isEditMode && this.parkId) {
      this.loadPark(this.parkId);
    } else {
      this.updateMarkerFromForm();
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  private buildForm(): void {
    const initial: Park = {
      id: undefined,
      name: '',
      countryCode: '',
      latitude: 48.8566,
      longitude: 2.3522,
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

    this.mapCenter = [initial.latitude, initial.longitude];
  }

  private loadCountries(): void {
    this.countriesLoading = true;
    this.form.get('countryCode')?.disable({ emitEvent: false });

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
        this.form.get('countryCode')?.enable({ emitEvent: false });
      },
      error: err => {
        console.error('Error loading countries', err);
        this.countriesLoading = false;
        this.form.get('countryCode')?.enable({ emitEvent: false });
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

        this.mapCenter = [park.latitude, park.longitude];
        this.updateMarkerFromForm();
      },
      error: err => {
        console.error('Error loading park', err);
        this.navigateToList();
      }
    });
  }

  private setupFormMapSync(): void {
    const subLat = this.form.get('latitude')!.valueChanges.subscribe(() => {
      this.updateMarkerFromForm();
    });
    const subLng = this.form.get('longitude')!.valueChanges.subscribe(() => {
      this.updateMarkerFromForm();
    });

    this.subscriptions.add(subLat);
    this.subscriptions.add(subLng);
  }

  private updateMarkerFromForm(): void {
    const lat = Number(this.form.get('latitude')!.value);
    const lng = Number(this.form.get('longitude')!.value);

    if (isNaN(lat) || isNaN(lng)) {
      return;
    }

    this.mapMarkers = [
      {
        id: 'park-marker',
        lat,
        lng,
        draggable: true
      }
    ];

    this.mapCenter = [lat, lng];
  }

  onMapPositionChange(pos: { lat: number; lng: number }): void {
    this.form.patchValue(
      {
        latitude: pos.lat,
        longitude: pos.lng
      },
      { emitEvent: true }
    );
  }

  // Getters form
  get nameControl() { return this.form.get('name')!; }
  get latitudeControl() { return this.form.get('latitude')!; }
  get longitudeControl() { return this.form.get('longitude')!; }
  get isVisibleControl() { return this.form.get('isVisible')!; }
  get countryCodeControl() { return this.form.get('countryCode')!; }

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
        error: err => console.error('Error updating park', err)
      });
    } else {
      this.apiService.createPark(payload).subscribe({
        next: created => {
          if (navigateBack) {
            this.navigateToList();
          } else if (created.id) {
            this.router.navigate(['../edit', created.id], { relativeTo: this.route });
          }
        },
        error: err => console.error('Error creating park', err)
      });
    }
  }

  onCancel(): void {
    this.navigateToList();
  }

  private navigateToList(): void {
    // On remonte jusqu'à la racine pour récupérer le :lang
    const lang =
      this.route.root.firstChild?.snapshot.params['lang'] ??
      this.route.snapshot.params['lang'] ??
      'en';

    this.router.navigate(['/', lang, 'admin', 'parks']);
  }
}
