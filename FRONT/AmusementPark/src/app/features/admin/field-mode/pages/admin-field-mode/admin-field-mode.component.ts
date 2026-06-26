import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonDirective } from 'primeng/button';

import { ParkItem } from '@app/models/parks/park-item';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';

import { AdminFieldModeFilter, AdminFieldModeItemRow, AdminFieldModeLocationKey, AdminFieldModePhotoCategoryOption } from '../../models/admin-field-mode.model';
import { AdminFieldModePositionService } from '../../services/admin-field-mode-position.service';
import { AdminFieldModeActionsFacade } from '../../state/admin-field-mode-actions.facade';
import { ADMIN_FIELD_MODE_GEOLOCATION_PORT, ADMIN_FIELD_MODE_IMAGES_API_SERVICE_PORT, ADMIN_FIELD_MODE_PARK_ITEMS_API_SERVICE_PORT, ADMIN_FIELD_MODE_PARKS_API_SERVICE_PORT } from '../../state/admin-field-mode-data.ports';
import { AdminFieldModeFacade } from '../../state/admin-field-mode.facade';

@Component({
  selector: 'app-admin-field-mode',
  templateUrl: './admin-field-mode.component.html',
  styleUrls: ['./admin-field-mode.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    AdminFieldModeFacade,
    AdminFieldModeActionsFacade,
    { provide: ADMIN_FIELD_MODE_PARKS_API_SERVICE_PORT, useExisting: ParksApiService },
    { provide: ADMIN_FIELD_MODE_PARK_ITEMS_API_SERVICE_PORT, useExisting: ParkItemsApiService },
    { provide: ADMIN_FIELD_MODE_IMAGES_API_SERVICE_PORT, useExisting: ImagesApiService },
    { provide: ADMIN_FIELD_MODE_GEOLOCATION_PORT, useExisting: AdminFieldModePositionService }
  ],
  imports: [CommonModule, FormsModule, ButtonDirective]
})
export class AdminFieldModeComponent implements OnInit {
  protected readonly state = this.fieldModeFacade;
  protected readonly actions = this.actionsFacade;
  protected readonly selectedRow = computed(() => {
    const selectedItem: ParkItem | null = this.state.selectedItem();
    if (!selectedItem?.id) {
      return null;
    }
    return this.state.rows().find((row: AdminFieldModeItemRow) => row.item.id === selectedItem.id) ?? null;
  });

  constructor(
    private readonly fieldModeFacade: AdminFieldModeFacade,
    private readonly actionsFacade: AdminFieldModeActionsFacade,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {
  }

  ngOnInit(): void {
    this.fieldModeFacade.initialize(this.route.snapshot.paramMap.get('itemId'));
  }

  protected get currentLang(): string {
    return this.router.url.split('/')[1] || 'en';
  }

  protected setSelectedPark(parkId: string | null): void {
    this.fieldModeFacade.selectPark(parkId);
  }

  protected setParkSearch(value: string): void {
    this.fieldModeFacade.setParkSearch(value);
  }

  protected setSearch(value: string): void {
    this.fieldModeFacade.setSearch(value);
  }

  protected setFilter(value: AdminFieldModeFilter): void {
    this.fieldModeFacade.setFilter(value);
  }

  protected selectRow(row: AdminFieldModeItemRow): void {
    this.fieldModeFacade.selectItem(row.item.id ?? null);
  }

  protected async addPhoto(): Promise<void> {
    const item: ParkItem | null = this.state.selectedItem();
    if (!item) {
      return;
    }

    const row: AdminFieldModeItemRow | null = this.selectedRow();
    const added: boolean = await this.actionsFacade.addPhoto(item, (row?.photoCount ?? 0) === 0);
    if (added) {
      this.reloadSelectedPark();
    }
  }

  protected async saveLocation(): Promise<void> {
    const item: ParkItem | null = this.state.selectedItem();
    if (!item) {
      return;
    }

    const savedItem: ParkItem | null = await this.actionsFacade.saveLocation(item, this.actions.locationKey() as AdminFieldModeLocationKey);
    if (savedItem) {
      this.reloadSelectedPark();
    }
  }

  protected setLocationKey(value: string): void {
    this.actions.setLocationKey(value as AdminFieldModeLocationKey);
  }

  protected setPhotoCategorySlug(value: string): void {
    this.actions.setPhotoCategorySlug(value);
  }

  protected photoCategoryLabel(option: AdminFieldModePhotoCategoryOption): string {
    return this.currentLang === 'fr' ? option.labelFr : option.labelEn;
  }

  protected messageLabel(key: string): string {
    const messages: Record<string, { fr: string; en: string }> = {
      'admin.fieldMode.messages.invalidImage': { fr: 'Image invalide.', en: 'Invalid image.' },
      'admin.fieldMode.messages.photoRequired': { fr: 'Choisis une photo avant l’envoi.', en: 'Choose a photo before upload.' },
      'admin.fieldMode.messages.photoMissingGps': { fr: 'Photo refusée : aucune coordonnée GPS EXIF détectée.', en: 'Photo rejected: no EXIF GPS coordinates found.' },
      'admin.fieldMode.messages.photoGpsReady': { fr: 'Photo acceptée : coordonnées GPS détectées.', en: 'Photo accepted: GPS coordinates detected.' },
      'admin.fieldMode.messages.positionDenied': { fr: 'Localisation refusée. Autorise la position pour ce site dans Chrome puis réessaie.', en: 'Location denied. Allow location for this site in Chrome and try again.' },
      'admin.fieldMode.messages.positionTimeout': { fr: 'Position GPS trop longue à obtenir. Place-toi en extérieur ou réactive la localisation puis réessaie.', en: 'GPS position timed out. Move outside or re-enable location and try again.' },
      'admin.fieldMode.messages.positionUnavailable': { fr: 'Position indisponible. Active la localisation du téléphone et vérifie l’autorisation du navigateur.', en: 'Position unavailable. Enable phone location and check browser permission.' },
      'admin.fieldMode.messages.positionReady': { fr: 'Position GPS capturée.', en: 'GPS position captured.' },
      'admin.fieldMode.messages.photoAdded': { fr: 'Photo ajoutée avec sa position EXIF.', en: 'Photo added with its EXIF position.' },
      'admin.fieldMode.messages.photoFailed': { fr: 'Photo non envoyée.', en: 'Photo was not uploaded.' },
      'admin.fieldMode.messages.locationSaved': { fr: 'Localisation enregistrée.', en: 'Location saved.' },
      'admin.fieldMode.messages.locationFailed': { fr: 'Localisation non enregistrée.', en: 'Location was not saved.' }
    };
    const message = messages[key];
    return message ? this.text(message.fr, message.en) : key;
  }

  protected text(fr: string, en: string): string {
    return this.currentLang === 'fr' ? fr : en;
  }

  private reloadSelectedPark(): void {
    this.fieldModeFacade.selectPark(this.state.selectedParkId());
  }
}
