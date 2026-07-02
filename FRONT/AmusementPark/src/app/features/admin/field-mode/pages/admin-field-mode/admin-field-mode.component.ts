import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ButtonDirective } from '@shared/primeless/button';

import { ParkItem } from '@app/models/parks/park-item';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';

import { AdminFieldModeFilter, AdminFieldModeItemRow, AdminFieldModeLocationKey, AdminFieldModePhotoCategoryOption, AdminFieldModeProcessedFilter } from '../../models/admin-field-mode.model';
import { AdminFieldModeProgressService } from '../../services/admin-field-mode-progress.service';
import { AdminFieldModePositionService } from '../../services/admin-field-mode-position.service';
import { AdminFieldModeActionsFacade } from '../../state/admin-field-mode-actions.facade';
import { ADMIN_FIELD_MODE_GEOLOCATION_PORT, ADMIN_FIELD_MODE_IMAGES_API_SERVICE_PORT, ADMIN_FIELD_MODE_PARK_ITEMS_API_SERVICE_PORT, ADMIN_FIELD_MODE_PARKS_API_SERVICE_PORT, ADMIN_FIELD_MODE_PROCESSED_STATUS_PORT } from '../../state/admin-field-mode-data.ports';
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
    { provide: ADMIN_FIELD_MODE_GEOLOCATION_PORT, useExisting: AdminFieldModePositionService },
    { provide: ADMIN_FIELD_MODE_PROCESSED_STATUS_PORT, useExisting: AdminFieldModeProgressService }
  ],
  imports: [CommonModule, FormsModule, ButtonDirective]
})
export class AdminFieldModeComponent implements OnInit {
  protected readonly state = this.fieldModeFacade;
  protected readonly actions = this.actionsFacade;
  protected readonly isDetailMode = signal(false);
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
    private readonly router: Router,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((paramMap) => {
      const itemId: string | null = paramMap.get('itemId');
      this.isDetailMode.set(!!itemId);
      this.fieldModeFacade.initialize(itemId);
    });
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

  protected setProcessedFilter(value: AdminFieldModeProcessedFilter): void {
    this.fieldModeFacade.setProcessedFilter(value);
  }

  protected async toggleProcessed(row: AdminFieldModeItemRow, event?: Event): Promise<void> {
    event?.stopPropagation();
    const wasProcessed: boolean = row.isProcessed;
    const saved: boolean = await this.fieldModeFacade.toggleProcessed(row);
    if (saved && this.isDetailMode() && !wasProcessed) {
      await this.goBackToList();
    }
  }

  protected selectRow(row: AdminFieldModeItemRow): void {
    if (!row.item.id) {
      this.fieldModeFacade.selectItem(null);
      return;
    }

    this.fieldModeFacade.selectItem(row.item.id);
    void this.router.navigate(['/', this.currentLang, 'admin', 'field-mode', 'item', row.item.id]);
  }

  protected async goBackToList(): Promise<void> {
    await this.router.navigate(['/', this.currentLang, 'admin', 'field-mode']);
  }

  protected async addPhoto(): Promise<void> {
    const item: ParkItem | null = this.state.selectedItem();
    if (!item?.id) {
      return;
    }

    const row: AdminFieldModeItemRow | null = this.selectedRow();
    const queuedCount: number = this.actions.selectedPhotos().length;
    const added: boolean = this.actionsFacade.addPhoto(item, (row?.photoCount ?? 0) === 0);
    if (added) {
      this.fieldModeFacade.incrementPhotoCount(item.id, queuedCount);
    }
  }

  protected async saveLocation(): Promise<void> {
    const item: ParkItem | null = this.state.selectedItem();
    if (!item) {
      return;
    }

    const savedItem: ParkItem | null = await this.actionsFacade.saveLocation(item, this.actions.locationKey() as AdminFieldModeLocationKey);
    if (savedItem) {
      this.fieldModeFacade.applySavedItem(savedItem);
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
      'admin.fieldMode.messages.photosPartiallyReady': { fr: 'Certaines photos ont été ignorées car elles sont invalides ou sans GPS EXIF.', en: 'Some photos were skipped because they are invalid or missing EXIF GPS.' },
      'admin.fieldMode.messages.photosQueued': { fr: 'Envoi photo lancé en arrière-plan.', en: 'Photo upload started in the background.' },
      'admin.fieldMode.messages.positionPrompt': { fr: 'Chrome doit afficher une demande d’autorisation de position. Autorise-la pour continuer.', en: 'Chrome should show a location permission prompt. Allow it to continue.' },
      'admin.fieldMode.messages.positionRetryLowAccuracy': { fr: 'GPS précis trop lent, nouvelle tentative avec une localisation approximative.', en: 'Precise GPS is too slow, retrying with approximate location.' },
      'admin.fieldMode.messages.positionBlockedByPolicy': { fr: 'Localisation bloquée par Permissions-Policy. Vérifie les headers servis par le proxy/Nginx.', en: 'Location is blocked by Permissions-Policy. Check headers served by the proxy/Nginx.' },
      'admin.fieldMode.messages.positionDenied': { fr: 'Localisation refusée. Dans Chrome, autorise la position pour ce site, puis vérifie aussi la permission Position de Chrome dans Android.', en: 'Location denied. In Chrome, allow location for this site, then also check Chrome Location permission in Android.' },
      'admin.fieldMode.messages.positionTimeout': { fr: 'Position GPS trop longue à obtenir. Place-toi en extérieur ou réactive la localisation puis réessaie.', en: 'GPS position timed out. Move outside or re-enable location and try again.' },
      'admin.fieldMode.messages.positionUnavailable': { fr: 'Position indisponible. Active la localisation du téléphone et vérifie l’autorisation du navigateur.', en: 'Position unavailable. Enable phone location and check browser permission.' },
      'admin.fieldMode.messages.positionInsecureContext': { fr: 'La géolocalisation exige HTTPS. Recharge la page en https:// puis réessaie.', en: 'Geolocation requires HTTPS. Reload the page with https:// and try again.' },
      'admin.fieldMode.messages.positionReady': { fr: 'Position GPS capturée.', en: 'GPS position captured.' },
      'admin.fieldMode.messages.positionBestEffort': { fr: 'Meilleure position GPS disponible capturée, mais la précision cible n’est pas atteinte.', en: 'Best available GPS position captured, but target accuracy was not reached.' },
      'admin.fieldMode.messages.photoAdded': { fr: 'Photo ajoutée avec sa position EXIF.', en: 'Photo added with its EXIF position.' },
      'admin.fieldMode.messages.photoFailed': { fr: 'Photo non envoyée.', en: 'Photo was not uploaded.' },
      'admin.fieldMode.messages.locationSaved': { fr: 'Localisation enregistrée.', en: 'Location saved.' },
      'admin.fieldMode.messages.locationFailed': { fr: 'Localisation non enregistrée.', en: 'Location was not saved.' },
      'admin.fieldMode.messages.itemProcessed': { fr: 'Item marqué traité.', en: 'Item marked as processed.' },
      'admin.fieldMode.messages.itemUnprocessed': { fr: 'Item remis à traiter.', en: 'Item moved back to todo.' },
      'admin.fieldMode.messages.itemProcessedFailed': { fr: 'Statut terrain non enregistré.', en: 'Field status was not saved.' },
      'admin.fieldMode.messages.itemLoadFailed': { fr: 'Item terrain introuvable.', en: 'Field item could not be loaded.' }
    };
    const message = messages[key];
    return message ? this.text(message.fr, message.en) : key;
  }

  protected text(fr: string, en: string): string {
    return this.currentLang === 'fr' ? fr : en;
  }

}
