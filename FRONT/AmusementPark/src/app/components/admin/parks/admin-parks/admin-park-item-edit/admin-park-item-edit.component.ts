import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ParkItem } from '../../../../../models/parks/park-item';
import { ParkItemCategory } from '../../../../../models/parks/park-item-category';
import { ParkItemType } from '../../../../../models/parks/park-item-type';
import { ParkZone } from '../../../../../models/parks/park-zone';
import { ApiService } from '../../../../../services/api.service';

interface Option<T> {
  labelKey: string;
  value: T;
}

@Component({
  selector: 'app-admin-park-item-edit',
  templateUrl: './admin-park-item-edit.component.html',
  styleUrls: ['./admin-park-item-edit.component.scss']
})
export class AdminParkItemEditComponent implements OnInit {
  form!: FormGroup;
  parkId: string = '';
  itemId: string | null = null;
  currentLang: string = 'en';
  zones: { id: string; label: string }[] = [];

  readonly categoryOptions: Option<ParkItemCategory>[] = [
    { labelKey: 'parkExplorer.categories.attraction', value: 'Attraction' },
    { labelKey: 'parkExplorer.categories.restaurant', value: 'Restaurant' },
    { labelKey: 'parkExplorer.categories.hotel', value: 'Hotel' },
    { labelKey: 'parkExplorer.categories.animal', value: 'Animal' },
    { labelKey: 'parkExplorer.categories.show', value: 'Show' },
    { labelKey: 'parkExplorer.categories.shop', value: 'Shop' },
    { labelKey: 'parkExplorer.categories.service', value: 'Service' },
    { labelKey: 'parkExplorer.categories.transport', value: 'Transport' },
    { labelKey: 'parkExplorer.categories.other', value: 'Other' }
  ];

  readonly typeOptions: Option<ParkItemType>[] = [
    { labelKey: 'parkExplorer.types.rollerCoaster', value: 'RollerCoaster' },
    { labelKey: 'parkExplorer.types.waterRide', value: 'WaterRide' },
    { labelKey: 'parkExplorer.types.flatRide', value: 'FlatRide' },
    { labelKey: 'parkExplorer.types.darkRide', value: 'DarkRide' },
    { labelKey: 'parkExplorer.types.familyRide', value: 'FamilyRide' },
    { labelKey: 'parkExplorer.types.thrillRide', value: 'ThrillRide' },
    { labelKey: 'parkExplorer.types.animalExhibit', value: 'AnimalExhibit' },
    { labelKey: 'parkExplorer.types.restaurant', value: 'Restaurant' },
    { labelKey: 'parkExplorer.types.snack', value: 'Snack' },
    { labelKey: 'parkExplorer.types.hotel', value: 'Hotel' },
    { labelKey: 'parkExplorer.types.show', value: 'Show' },
    { labelKey: 'parkExplorer.types.shop', value: 'Shop' },
    { labelKey: 'parkExplorer.types.service', value: 'Service' },
    { labelKey: 'parkExplorer.types.transport', value: 'Transport' },
    { labelKey: 'parkExplorer.types.other', value: 'Other' }
  ];

  get isEditMode(): boolean {
    return !!this.itemId;
  }

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly apiService: ApiService
  ) {
  }

  ngOnInit(): void {
    this.currentLang = this.route.root.firstChild?.snapshot.params['lang'] ?? 'en';
    this.parkId = this.route.snapshot.paramMap.get('idPark') ?? '';
    this.itemId = this.route.snapshot.paramMap.get('idItem');

    this.form = this.fb.group({
      parkId: [this.parkId, Validators.required],
      zoneId: [null],
      name: ['', Validators.required],
      category: ['Attraction', Validators.required],
      type: ['RollerCoaster', Validators.required],
      subtype: [''],
      latitude: [0, Validators.required],
      longitude: [0, Validators.required],
      descriptions: [[]],
      isVisible: [true]
    });

    this.apiService.getParkZonesByParkId(this.parkId).subscribe((zones: ParkZone[]) => {
      this.zones = zones
        .filter((zone: ParkZone) => !!zone.id)
        .map((zone: ParkZone) => ({ id: zone.id!, label: zone.name }));
    });

    if (this.itemId) {
      this.apiService.getParkItemById(this.itemId).subscribe((item: ParkItem) => {
        this.form.patchValue({
          parkId: item.parkId,
          zoneId: item.zoneId ?? null,
          name: item.name,
          category: item.category,
          type: item.type,
          subtype: item.subtype ?? '',
          latitude: item.latitude,
          longitude: item.longitude,
          descriptions: item.descriptions ?? [],
          isVisible: item.isVisible ?? true
        });
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload: ParkItem = {
      ...this.form.value,
      zoneId: this.form.value.zoneId || null,
      subtype: this.form.value.subtype || null
    };

    if (this.itemId) {
      this.apiService.updateParkItem(this.itemId, payload).subscribe(() => this.goBack());
      return;
    }

    this.apiService.createParkItem(payload).subscribe(() => this.goBack());
  }

  goBack(): void {
    this.router.navigate(['/', this.currentLang, 'admin', 'parks', 'edit', this.parkId, 'items']);
  }
}
