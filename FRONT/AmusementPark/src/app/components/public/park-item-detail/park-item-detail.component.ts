import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';

import { PageStateComponent } from '../../shared/page-state/page-state.component';
import { Park } from '../../../models/parks/park';
import { ParkItem } from '../../../models/parks/park-item';
import { ViewState } from '../../../models/shared/view-state';
import { ApiService } from '../../../services/api.service';
import { TranslationService } from '../../../services/translation.service';
import { commitViewUpdate } from '../../../utils/change-detection.utils';
import { buildParkSlug } from '../../../commons/park-presentation.utils';
import {
  buildEntitySlug,
  getParkItemCategoryTranslationKey,
  getParkItemTypeTranslationKey,
  resolveParkItemDescription
} from '../../../commons/park-item-presentation.utils';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';

@Component({
  selector: 'app-park-item-detail',
  templateUrl: './park-item-detail.component.html',
  styleUrls: ['./park-item-detail.component.scss'],
  imports: [NgFor, NgIf, RouterLink, PageStateComponent, TranslateModule, ButtonDirective]
})
export class ParkItemDetailComponent implements OnInit, OnDestroy {
  pageState: ViewState = ViewState.Loading;
  currentLang: string = 'en';

  item: ParkItem | null = null;
  park: Park | null = null;
  manufacturerName: string | null = null;
  zoneName: string | null = null;

  private readonly subscriptions: Subscription = new Subscription();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly apiService: ApiService,
    private readonly translationService: TranslationService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    if (this.route.parent) {
      this.subscriptions.add(this.route.parent.paramMap.subscribe((params: ParamMap) => {
        commitViewUpdate(this.changeDetectorRef, () => {
          this.currentLang = params.get('lang') ?? 'en';
        });
      }));
    }

    this.subscriptions.add(this.translationService.languageChanged.subscribe((lang: string) => {
      commitViewUpdate(this.changeDetectorRef, () => {
        this.currentLang = lang;
      });
    }));

    this.subscriptions.add(this.route.paramMap.subscribe((params: ParamMap) => {
      const itemId: string | null = params.get('itemId');
      if (!itemId) {
        commitViewUpdate(this.changeDetectorRef, () => {
          this.pageState = ViewState.Error;
        });
        return;
      }

      this.loadItem(itemId);
    }));
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  get categoryLabelKey(): string {
    return getParkItemCategoryTranslationKey(this.item?.category);
  }

  get typeLabelKey(): string {
    return getParkItemTypeTranslationKey(this.item?.type);
  }

  get description(): string | null {
    return resolveParkItemDescription(this.item, this.currentLang);
  }

  get parkLink(): string[] | null {
    if (!this.park?.id || !this.park?.name) {
      return null;
    }

    return ['/', this.currentLang, 'park', this.park.id, buildParkSlug(this.park.name)];
  }

  get itemsLink(): string[] | null {
    if (!this.park?.id || !this.park?.name) {
      return null;
    }

    return ['/', this.currentLang, 'park', this.park.id, buildParkSlug(this.park.name), 'items'];
  }

  get itemLink(): string[] | null {
    if (!this.park?.id || !this.park?.name || !this.item?.id || !this.item?.name) {
      return null;
    }

    return ['/', this.currentLang, 'park', this.park.id, buildParkSlug(this.park.name), 'item', this.item.id, buildEntitySlug(this.item.name)];
  }

  get hasAttractionDetails(): boolean {
    return !!this.item?.attractionDetails;
  }

  get specRows(): Array<{ labelKey: string; value: string }> {
    if (!this.item?.attractionDetails) {
      return [];
    }

    const details = this.item.attractionDetails;
    const rows: Array<{ labelKey: string; value: string }> = [];

    this.pushRow(rows, 'parkItems.fields.manufacturer', this.manufacturerName);
    this.pushRow(rows, 'parkItems.fields.model', details.model);
    this.pushRow(rows, 'parkItems.fields.status', details.status);
    this.pushRow(rows, 'parkItems.fields.materialType', details.materialType);
    this.pushRow(rows, 'parkItems.fields.seatingType', details.seatingType);
    this.pushRow(rows, 'parkItems.fields.launchType', details.launchType);
    this.pushRow(rows, 'parkItems.fields.restraintType', details.restraintType);
    this.pushRow(rows, 'parkItems.fields.openingDate', this.formatDate(details.openingDate));
    this.pushRow(rows, 'parkItems.fields.closingDate', this.formatDate(details.closingDate));
    this.pushRow(rows, 'parkItems.fields.heightInMeters', this.formatNumberWithUnit(details.heightInMeters, 'm'));
    this.pushRow(rows, 'parkItems.fields.heightInFeet', this.formatNumberWithUnit(details.heightInFeet, 'ft'));
    this.pushRow(rows, 'parkItems.fields.lengthInMeters', this.formatNumberWithUnit(details.lengthInMeters, 'm'));
    this.pushRow(rows, 'parkItems.fields.lengthInFeet', this.formatNumberWithUnit(details.lengthInFeet, 'ft'));
    this.pushRow(rows, 'parkItems.fields.speedInKmH', this.formatNumberWithUnit(details.speedInKmH, 'km/h'));
    this.pushRow(rows, 'parkItems.fields.speedInMph', this.formatNumberWithUnit(details.speedInMph, 'mph'));
    this.pushRow(rows, 'parkItems.fields.dropInMeters', this.formatNumberWithUnit(details.dropInMeters, 'm'));
    this.pushRow(rows, 'parkItems.fields.inversionCount', this.formatInteger(details.inversionCount));
    this.pushRow(rows, 'parkItems.fields.capacityPerHour', this.formatInteger(details.capacityPerHour));
    this.pushRow(rows, 'parkItems.fields.durationInSeconds', this.formatInteger(details.durationInSeconds));
    this.pushRow(rows, 'parkItems.fields.trainCount', this.formatInteger(details.trainCount));
    this.pushRow(rows, 'parkItems.fields.carsPerTrain', this.formatInteger(details.carsPerTrain));
    this.pushRow(rows, 'parkItems.fields.ridersPerVehicle', this.formatInteger(details.ridersPerVehicle));
    this.pushRow(rows, 'parkItems.fields.hasSingleRider', this.formatBoolean(details.hasSingleRider));
    this.pushRow(rows, 'parkItems.fields.hasFastPass', this.formatBoolean(details.hasFastPass));
    this.pushRow(rows, 'parkItems.fields.isAccessibleForReducedMobility', this.formatBoolean(details.isAccessibleForReducedMobility));
    this.pushRow(rows, 'parkItems.fields.isIndoor', this.formatBoolean(details.isIndoor));
    this.pushRow(rows, 'parkItems.fields.isLaunched', this.formatBoolean(details.isLaunched));
    this.pushRow(rows, 'parkItems.fields.waterExposureLevel', details.waterExposureLevel ?? null);

    return rows;
  }

  goBackToItems(): void {
    if (this.itemsLink) {
      this.router.navigate(this.itemsLink);
      return;
    }

    if (this.parkLink) {
      this.router.navigate(this.parkLink);
      return;
    }

    this.router.navigate(['/', this.currentLang, 'parks']);
  }

  private loadItem(itemId: string): void {
    this.pageState = ViewState.Loading;
    this.item = null;
    this.park = null;
    this.manufacturerName = null;
    this.zoneName = null;

    this.subscriptions.add(this.apiService.getParkItemById(itemId).subscribe({
      next: (item: ParkItem) => {
        commitViewUpdate(this.changeDetectorRef, () => {
          this.item = item;
        });

        this.loadRelatedData(item);
      },
      error: (error: unknown) => {
        console.error('Error loading park item', error);
        commitViewUpdate(this.changeDetectorRef, () => {
          this.pageState = ViewState.Error;
        });
      }
    }));
  }

  private loadRelatedData(item: ParkItem): void {
    this.subscriptions.add(this.apiService.getParkById(item.parkId).subscribe({
      next: (park: Park) => {
        commitViewUpdate(this.changeDetectorRef, () => {
          this.park = park;
          this.pageState = ViewState.Ready;
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park for item', error);
        commitViewUpdate(this.changeDetectorRef, () => {
          this.pageState = ViewState.Error;
        });
      }
    }));

    if (item.attractionDetails?.manufacturerId) {
      this.subscriptions.add(this.apiService.getAttractionManufacturerById(item.attractionDetails.manufacturerId).subscribe({
        next: (manufacturer) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.manufacturerName = manufacturer.name ?? null;
          });
        },
        error: () => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.manufacturerName = null;
          });
        }
      }));
    }

    if (item.zoneId) {
      this.subscriptions.add(this.apiService.getParkZoneById(item.zoneId).subscribe({
        next: (zone) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.zoneName = zone.name ?? null;
          });
        },
        error: () => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.zoneName = null;
          });
        }
      }));
    }
  }

  private pushRow(
    rows: Array<{ labelKey: string; value: string }>,
    labelKey: string,
    value: string | null | undefined
  ): void {
    if (value == null || value.trim() === '') {
      return;
    }

    rows.push({ labelKey, value });
  }

  private formatDate(value: string | null | undefined): string | null {
    if (!value) {
      return null;
    }

    return value.slice(0, 10);
  }

  private formatNumberWithUnit(value: number | null | undefined, unit: string): string | null {
    if (value == null) {
      return null;
    }

    return `${value} ${unit}`;
  }

  private formatInteger(value: number | null | undefined): string | null {
    return value == null ? null : `${value}`;
  }

  private formatBoolean(value: boolean | null | undefined): string | null {
    if (value == null) {
      return null;
    }

    if (this.currentLang === 'fr') {
      return value ? 'Oui' : 'Non';
    }

    return value ? 'Yes' : 'No';
  }
}
