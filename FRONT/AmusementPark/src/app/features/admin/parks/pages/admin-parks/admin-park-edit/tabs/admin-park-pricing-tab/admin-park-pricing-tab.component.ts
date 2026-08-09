import { ChangeDetectionStrategy, Component, Input, OnChanges, SimpleChanges, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ParkPricing } from '@app/models/parks/park-pricing';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { AdminParkEditStateFacade } from '@features/admin/parks/state/admin-park-edit-state.facade';

interface AdminPricingCopy {
  title: string;
  subtitle: string;
  reload: string;
  format: string;
  save: string;
  jsonLabel: string;
  hint: string;
  invalidJson: string;
  loadError: string;
  saveError: string;
  savedSummary: string;
  savedDetail: string;
}

const COPY: Record<string, AdminPricingCopy> = {
  fr: { title: 'Tarifs du parc', subtitle: 'Édition rapide du document tarifaire complet. Les règles métier sont validées par l’API lors de l’enregistrement.', reload: 'Recharger', format: 'Formater', save: 'Enregistrer les tarifs', jsonLabel: 'JSON des tarifs du parc', hint: 'Gère les billets, pass annuels, parking, périodes de validité, prix web/guichet, conditions et liens officiels.', invalidJson: 'Le JSON des tarifs est invalide.', loadError: 'Impossible de charger les tarifs.', saveError: 'Impossible d’enregistrer les tarifs.', savedSummary: 'Tarifs enregistrés', savedDetail: 'Le document tarifaire du parc a été mis à jour.' },
  en: { title: 'Park pricing', subtitle: 'Fast editing of the complete pricing document. Business rules are validated by the API when saving.', reload: 'Reload', format: 'Format', save: 'Save pricing', jsonLabel: 'Park pricing JSON', hint: 'Manage tickets, annual passes, parking, validity periods, online/gate prices, conditions and official links.', invalidJson: 'The pricing JSON is invalid.', loadError: 'Unable to load pricing.', saveError: 'Unable to save pricing.', savedSummary: 'Pricing saved', savedDetail: 'The park pricing document has been updated.' }
};

@Component({
  selector: 'app-admin-park-pricing-tab',
  templateUrl: './admin-park-pricing-tab.component.html',
  styleUrls: ['./admin-park-pricing-tab.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonDirective]
})
export class AdminParkPricingTabComponent implements OnChanges {
  @Input() parkId: string | null = null;
  @Input() currentLanguage: string = 'en';

  protected readonly pricingJson = signal<string>('');
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly loaded = signal<boolean>(false);

  private readonly editStateFacade: AdminParkEditStateFacade = inject(AdminParkEditStateFacade);
  private readonly toastMessageService: ToastMessageService = inject(ToastMessageService);

  protected get copy(): AdminPricingCopy {
    const language: string = this.currentLanguage?.toLowerCase().split('-')[0] ?? 'en';
    return COPY[language] ?? COPY['en'];
  }

  protected get loading(): boolean {
    return this.editStateFacade.pricingLoading();
  }

  protected get saving(): boolean {
    return this.editStateFacade.pricingSaving();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['parkId'] && this.parkId) {
      this.loaded.set(false);
      void this.loadPricing(false);
    }
  }

  protected onJsonChanged(value: string): void {
    this.pricingJson.set(value);
    this.errorMessage.set(null);
  }

  protected reload(): void {
    void this.loadPricing(true);
  }

  protected format(): void {
    try {
      const parsed: unknown = JSON.parse(this.pricingJson() || '{}');
      this.pricingJson.set(JSON.stringify(parsed, null, 2));
      this.errorMessage.set(null);
    } catch {
      this.errorMessage.set(this.copy.invalidJson);
    }
  }

  protected async save(): Promise<void> {
    const parkId: string | null = this.parkId;
    if (!parkId || this.saving) {
      return;
    }

    let payload: ParkPricing;
    try {
      payload = JSON.parse(this.pricingJson() || '{}') as ParkPricing;
    } catch {
      this.errorMessage.set(this.copy.invalidJson);
      return;
    }

    payload.parkId = parkId;
    payload.currencyCode = payload.currencyCode?.trim().toUpperCase() || 'EUR';
    payload.admissionOffers = Array.isArray(payload.admissionOffers) ? payload.admissionOffers : [];
    payload.annualPasses = Array.isArray(payload.annualPasses) ? payload.annualPasses : [];
    payload.parkingOffers = Array.isArray(payload.parkingOffers) ? payload.parkingOffers : [];

    try {
      const savedPricing: ParkPricing = await this.editStateFacade.savePricing(parkId, payload);
      this.pricingJson.set(JSON.stringify(savedPricing, null, 2));
      this.loaded.set(true);
      this.errorMessage.set(null);
      this.toastMessageService.add('success', this.copy.savedSummary, this.copy.savedDetail);
    } catch (error: unknown) {
      console.error('Error saving park pricing', error);
      this.errorMessage.set(this.copy.saveError);
    }
  }

  private async loadPricing(force: boolean): Promise<void> {
    const parkId: string | null = this.parkId;
    if (!parkId || (!force && this.loaded())) {
      return;
    }

    try {
      const pricing: ParkPricing = await this.editStateFacade.loadPricing(parkId);
      this.pricingJson.set(JSON.stringify(pricing, null, 2));
      this.loaded.set(true);
      this.errorMessage.set(null);
    } catch (error: unknown) {
      if (hasHttpStatus(error, 404)) {
        this.pricingJson.set(JSON.stringify(this.createTemplate(parkId), null, 2));
        this.loaded.set(true);
        this.errorMessage.set(null);
        return;
      }

      console.error('Error loading park pricing', error);
      this.errorMessage.set(this.copy.loadError);
    }
  }

  private createTemplate(parkId: string): ParkPricing {
    return {
      parkId,
      currencyCode: 'EUR',
      sourceUrl: '',
      purchaseUrl: '',
      notes: '',
      lastVerifiedAtUtc: new Date().toISOString(),
      admissionOffers: [],
      annualPasses: [],
      parkingOffers: []
    };
  }
}
