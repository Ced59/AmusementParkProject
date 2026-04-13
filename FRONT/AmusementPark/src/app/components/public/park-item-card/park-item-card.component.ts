import { Component, Input } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';

import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { buildParkSlug } from '@app/commons/park-presentation.utils';
import {
  buildEntitySlug,
  getParkItemCategoryTranslationKey,
  getParkItemTypeTranslationKey,
  resolveParkItemDescription
} from '@app/commons/park-item-presentation.utils';

@Component({
  selector: 'app-park-item-card',
  templateUrl: './park-item-card.component.html',
  styleUrls: ['./park-item-card.component.scss'],
  imports: [NgFor, NgIf, RouterLink, TranslateModule, ButtonDirective]
})
export class ParkItemCardComponent {
  @Input() item: ParkItem | null = null;
  @Input() park: Park | null = null;
  @Input() currentLang: string = 'en';
  @Input() manufacturerName: string | null = null;
  @Input() zoneName: string | null = null;

  get itemLink(): string[] | null {
    if (!this.park?.id || !this.park?.name || !this.item?.id || !this.item?.name) {
      return null;
    }

    return [
      '/',
      this.currentLang,
      'park',
      this.park.id,
      buildParkSlug(this.park.name),
      'item',
      this.item.id,
      buildEntitySlug(this.item.name)
    ];
  }

  get description(): string | null {
    return resolveParkItemDescription(this.item, this.currentLang);
  }

  get categoryLabelKey(): string {
    return getParkItemCategoryTranslationKey(this.item?.category);
  }

  get typeLabelKey(): string {
    return getParkItemTypeTranslationKey(this.item?.type);
  }

  get typeIconClass(): string {
    switch (this.item?.type) {
      case 'RollerCoaster':
        return 'pi pi-bolt';
      case 'WaterRide':
        return 'pi pi-compass';
      case 'FlatRide':
        return 'pi pi-sync';
      case 'DarkRide':
        return 'pi pi-moon';
      default:
        return 'pi pi-star';
    }
  }

  get highlightValues(): string[] {
    if (!this.item) {
      return [];
    }

    const values: string[] = [];
    if (this.manufacturerName) {
      values.push(this.manufacturerName);
    }

    if (this.item.attractionDetails?.model) {
      values.push(this.item.attractionDetails.model);
    }

    if (this.item.attractionDetails?.status) {
      values.push(this.item.attractionDetails.status);
    }

    if (this.item.attractionDetails?.heightInMeters != null) {
      values.push(`${this.item.attractionDetails.heightInMeters} m`);
    }

    if (this.item.attractionDetails?.speedInKmH != null) {
      values.push(`${this.item.attractionDetails.speedInKmH} km/h`);
    }

    if (this.item.attractionDetails?.inversionCount != null) {
      values.push(`${this.item.attractionDetails.inversionCount} inv.`);
    }

    return values.slice(0, 4);
  }
}
