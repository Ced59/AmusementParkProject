import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LocalizedItem } from '../../../../../models/shared/localized-item';
import { ParkZone } from '../../../../../models/parks/park-zone';
import { ApiService } from '../../../../../services/api.service';

@Component({
    selector: 'app-admin-park-zone-edit',
    templateUrl: './admin-park-zone-edit.component.html',
    styleUrls: ['./admin-park-zone-edit.component.scss'],
    standalone: false
})
export class AdminParkZoneEditComponent implements OnInit {
  form!: FormGroup;
  parkId: string = '';
  zoneId: string | null = null;
  currentLang: string = 'en';

  get isEditMode(): boolean {
    return !!this.zoneId;
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
    this.zoneId = this.route.snapshot.paramMap.get('idZone');

    this.form = this.fb.group({
      parkId: [this.parkId, Validators.required],
      names: [[], Validators.required],
      descriptions: [[]],
      isVisible: [true],
      sortOrder: [0]
    });

    if (this.zoneId) {
      this.apiService.getParkZoneById(this.zoneId).subscribe((zone: ParkZone) => {
        this.form.patchValue({
          parkId: zone.parkId,
          names: this.getInitialNames(zone),
          descriptions: zone.descriptions ?? [],
          isVisible: zone.isVisible ?? true,
          sortOrder: zone.sortOrder ?? 0
        });
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload: ParkZone = this.form.value as ParkZone;

    if (this.zoneId) {
      this.apiService.updateParkZone(this.zoneId, payload).subscribe(() => this.goBack());
      return;
    }

    this.apiService.createParkZone(payload).subscribe(() => this.goBack());
  }

  goBack(): void {
    this.router.navigate(['/', this.currentLang, 'admin', 'parks', 'edit', this.parkId, 'zones']);
  }

  private getInitialNames(zone: ParkZone): LocalizedItem<string>[] {
    if (zone.names && zone.names.length > 0) {
      return zone.names;
    }

    if (zone.name) {
      return [{ languageCode: 'en', value: zone.name }];
    }

    return [];
  }
}
