import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { PageStateComponent } from '../../shared/page-state/page-state.component';
import { Park } from '../../../models/parks/park';
import { ParkItem } from '../../../models/parks/park-item';
import { TranslationService } from '../../../services/translation.service';
import { buildParkSlug } from '../../../commons/park-presentation.utils';
import {
  buildEntitySlug,
  getParkItemCategoryTranslationKey,
  getParkItemTypeTranslationKey,
  resolveParkItemDescription
} from '../../../commons/park-item-presentation.utils';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { ParkItemDetailStateFacade } from '@features/public/park-items/state/park-item-detail-state.facade';

interface ParkItemDetailRow {
  labelKey: string;
  value: string;
}

@Component({
  selector: 'app-park-item-detail',
  templateUrl: './park-item-detail.component.html',
  styleUrls: ['./park-item-detail.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkItemDetailStateFacade],
  imports: [NgFor, NgIf, RouterLink, PageStateComponent, TranslateModule, ButtonDirective]
})
export class ParkItemDetailComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly item = this.stateFacade.item;
  protected readonly park = this.stateFacade.park;
  protected readonly manufacturerName = this.stateFacade.manufacturerName;
  protected readonly zoneName = this.stateFacade.zoneName;
  protected readonly currentLang = signal<string>('en');

  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly stateFacade: ParkItemDetailStateFacade
  ) {
  }

  ngOnInit(): void {
    if (this.route.parent) {
      this.route.parent.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap) => {
        this.currentLang.set(params.get('lang') ?? 'en');
      });
    }

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((lang: string) => {
      this.currentLang.set(lang);
    });

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap) => {
      const itemId: string | null = params.get('itemId');

      if (!itemId) {
        return;
      }

      this.stateFacade.loadItem(itemId);
    });
  }

  get categoryLabelKey(): string {
    return getParkItemCategoryTranslationKey(this.item()?.category);
  }

  get typeLabelKey(): string {
    return getParkItemTypeTranslationKey(this.item()?.type);
  }

  get description(): string | null {
    return resolveParkItemDescription(this.item(), this.currentLang());
  }

  get parkLink(): string[] | null {
    const currentPark: Park | null = this.park();

    if (!currentPark?.id || !currentPark?.name) {
      return null;
    }

    return ['/', this.currentLang(), 'park', currentPark.id, buildParkSlug(currentPark.name)];
  }

  get itemsLink(): string[] | null {
    const currentPark: Park | null = this.park();

    if (!currentPark?.id || !currentPark?.name) {
      return null;
    }

    return ['/', this.currentLang(), 'park', currentPark.id, buildParkSlug(currentPark.name), 'items'];
  }

  get itemLink(): string[] | null {
    const currentPark: Park | null = this.park();
    const currentItem: ParkItem | null = this.item();

    if (!currentPark?.id || !currentPark?.name || !currentItem?.id || !currentItem?.name) {
      return null;
    }

    return ['/', this.currentLang(), 'park', currentPark.id, buildParkSlug(currentPark.name), 'item', currentItem.id, buildEntitySlug(currentItem.name)];
  }

  get specRows(): ParkItemDetailRow[] {
    const currentItem: ParkItem | null = this.item();

    if (!currentItem?.attractionDetails) {
      return [];
    }

    const details = currentItem.attractionDetails;
    const rows: ParkItemDetailRow[] = [];

    this.pushRow(rows, 'parkItems.fields.manufacturer', this.manufacturerName());
    this.pushRow(rows, 'parkItems.fields.model', details.model);
    this.pushRow(rows, 'parkItems.fields.status', details.status);
    this.pushRow(rows, 'parkItems.fields.materialType', details.materialType);
    this.pushRow(rows, 'parkItems.fields.seatingType', details.seatingType);
    this.pushRow(rows, 'parkItems.fields.launchType', details.launchType);
    this.pushRow(rows, 'parkItems.fields.restraintType', details.restraintType);
    this.pushRow(rows, 'parkItems.fields.openingDate', this.formatDate(details.openingDate ?? details.openingDateText));
    this.pushRow(rows, 'parkItems.fields.closingDate', this.formatDate(details.closingDate ?? details.closingDateText));
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

  get spotlightRows(): ParkItemDetailRow[] {
    const priorityKeys: string[] = [
      'parkItems.fields.status',
      'parkItems.fields.heightInMeters',
      'parkItems.fields.speedInKmH',
      'parkItems.fields.inversionCount'
    ];

    return this.specRows.filter((row: ParkItemDetailRow) => priorityKeys.includes(row.labelKey)).slice(0, 4);
  }

  get secondaryRows(): ParkItemDetailRow[] {
    const spotlightKeys: Set<string> = new Set(this.spotlightRows.map((row: ParkItemDetailRow) => row.labelKey));
    return this.specRows.filter((row: ParkItemDetailRow) => !spotlightKeys.has(row.labelKey));
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

    this.router.navigate(['/', this.currentLang(), 'parks']);
  }

  private pushRow(rows: ParkItemDetailRow[], labelKey: string, value: string | null | undefined): void {
    if (value == null || value.trim() === '') {
      return;
    }

    rows.push({ labelKey, value });
  }

  private formatDate(value: string | null | undefined): string | null {
    if (!value) {
      return null;
    }

    return value.length >= 10 ? value.slice(0, 10) : value;
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

    if (this.currentLang() === 'fr') {
      return value ? 'Oui' : 'Non';
    }

    return value ? 'Yes' : 'No';
  }
}
