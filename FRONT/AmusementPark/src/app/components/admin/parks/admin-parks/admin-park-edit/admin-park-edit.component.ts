import {Component, OnDestroy, OnInit} from '@angular/core';
import {FormBuilder, FormGroup, Validators} from "@angular/forms";
import {Subscription} from "rxjs";
import {ActivatedRoute, Router} from "@angular/router";
import {ApiService} from "../../../../../services/api.service";
import {Park} from "../../../../../models/parks/park";
import {CountryDto} from "../../../../../models/countries/country-dto";
import {ParkLogoViewModel} from "../../../../../models/parks/park-logo-viewmodel";
import {ParkLogoDto} from "../../../../../models/parks/park-logo";
import {UploadedImage} from "../../../../../models/images/uploaded-image";
import {ImageCategory} from "../../../../../models/images/image-category";

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

  // Logos
  parkLogos: ParkLogoViewModel[] = [];
  currentLogo: ParkLogoViewModel | null = null;

  logosLoading = false;
  logosUploading = false;
  logosPage = 0;
  logosPageSize = 8;

  selectedLogoFile: File | null = null;
  newLogoDescription: string = '';

  private subscriptions = new Subscription();

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    protected readonly apiService: ApiService
  ) {}

  ngOnInit(): void {
    this.buildForm();

    this.parkId = this.route.snapshot.paramMap.get('idPark');
    this.isEditMode = !!this.parkId;

    this.loadCountries();

    this.setupFormMapSync();

    if (this.isEditMode && this.parkId) {
      this.loadPark(this.parkId);
      this.loadLogos(this.parkId);
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
      isVisible: [initial.isVisible],
      websiteUrl: [initial.webSiteUrl ?? ''],
      street: [initial.street ?? ''],
      city: [initial.city ?? ''],
      postalCode: [initial.postalCode ?? '']
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
          isVisible: park.isVisible ?? true,
          websiteUrl: park.webSiteUrl ?? '',
          street: park.street ?? '',
          city: park.city ?? '',
          postalCode: park.postalCode ?? ''
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
      isVisible: raw.isVisible,
      webSiteUrl: raw.websiteUrl || undefined,
      street: raw.street || undefined,
      city: raw.city || undefined,
      postalCode: raw.postalCode || undefined
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

  onLogoFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      this.selectedLogoFile = null;
      return;
    }
    this.selectedLogoFile = input.files[0];
  }

  onUploadLogo(): void {
    if (!this.parkId || !this.selectedLogoFile) {
      return;
    }

    this.logosUploading = true;

    this.apiService
      .uploadImage(this.selectedLogoFile, ImageCategory.PARK_LOGO, false, `${this.form.get('name')?.value || ''}`)
      .subscribe({
      next: (uploaded: UploadedImage) => {
        const imageId = uploaded.id;

        this.apiService.createParkLogo(this.parkId!, imageId, this.newLogoDescription || undefined)
          .subscribe({
            next: (logoDto: ParkLogoDto) => {
              const vm = this.toLogoViewModel(logoDto);

              // Mise à jour locale : le nouveau logo est current
              this.parkLogos.forEach(l => l.isCurrent = l.id === vm.id ? vm.isCurrent : false);
              const existingIndex = this.parkLogos.findIndex(l => l.id === vm.id);
              if (existingIndex >= 0) {
                this.parkLogos[existingIndex] = vm;
              } else {
                // on insère en début pour voir le plus récent en premier
                this.parkLogos.unshift(vm);
              }

              this.currentLogo = vm;
              this.selectedLogoFile = null;
              this.newLogoDescription = '';
              this.logosUploading = false;
            },
            error: err => {
              console.error('Error creating park logo', err);
              this.logosUploading = false;
            }
          });
      },
      error: err => {
        console.error('Error uploading logo image', err);
        this.logosUploading = false;
      }
    });
  }

  onSetCurrentLogo(logo: ParkLogoViewModel): void {
    if (!this.parkId || logo.isCurrent) {
      return;
    }

    this.apiService.setCurrentParkLogo(this.parkId, logo.id).subscribe({
      next: (updatedDto: ParkLogoDto) => {
        const updatedVm = this.toLogoViewModel(updatedDto);

        this.parkLogos = this.parkLogos.map(l => ({
          ...l,
          isCurrent: l.id === updatedVm.id
        }));

        this.currentLogo = updatedVm;
      },
      error: err => {
        console.error('Error setting current park logo', err);
      }
    });
  }

  onDeleteLogo(logo: ParkLogoViewModel): void {
    if (!this.parkId) {
      return;
    }

    if (!confirm('Supprimer ce logo ?')) {
      return;
    }

    this.apiService.deleteParkLogo(this.parkId, logo.id).subscribe({
      next: () => {
        this.parkLogos = this.parkLogos.filter(l => l.id !== logo.id);
        if (this.currentLogo?.id === logo.id) {
          this.currentLogo = this.parkLogos.find(l => l.isCurrent) ?? null;
        }
      },
      error: err => {
        console.error('Error deleting park logo', err);
      }
    });
  }


  private navigateToList(): void {
    // On remonte jusqu'à la racine pour récupérer le :lang
    const lang =
      this.route.root.firstChild?.snapshot.params['lang'] ??
      this.route.snapshot.params['lang'] ??
      'en';

    this.router.navigate(['/', lang, 'admin', 'parks']);
  }

  private loadLogos(parkId: string): void {
    this.logosLoading = true;
    this.apiService.getParkLogos(parkId).subscribe({
      next: (dtos: ParkLogoDto[]) => {
        this.parkLogos = dtos.map(dto => this.toLogoViewModel(dto));
        this.currentLogo = this.parkLogos.find(l => l.isCurrent) ?? null;
        this.logosLoading = false;
      },
      error: err => {
        console.error('Error loading park logos', err);
        this.logosLoading = false;
      }
    });
  }

  private toLogoViewModel(dto: ParkLogoDto): ParkLogoViewModel {
    return {
      ...dto,
      imageUrl: this.apiService.buildLogoUrl(dto.imageId)
    };
  }

  get pagedLogos(): ParkLogoViewModel[] {
    const start = this.logosPage * this.logosPageSize;
    return this.parkLogos.slice(start, start + this.logosPageSize);
  }

  onLogosPageChange(event: any): void {
    this.logosPage = event.page;
    this.logosPageSize = event.rows;
  }
}
