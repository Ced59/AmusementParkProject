import { ChangeDetectionStrategy, Component, Input, OnChanges, SimpleChanges, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  ParkAdmissionPriceOffer,
  ParkAnnualPassOffer,
  ParkCreditOffer,
  ParkParkingPriceOffer,
  ParkPricing,
  ParkPricingSnapshot,
} from '@app/models/parks/park-pricing';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { AdminParkEditStateFacade } from '@features/admin/parks/state/admin-park-edit-state.facade';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { LocalizedTextInputComponent } from '@shared/components/localized-text-input/localized-text-input.component';
import {
  AdminParkPricingOffer,
  AdminParkPricingOfferEditorComponent,
} from './admin-park-pricing-offer-editor.component';
import { AdminParkPricingSnapshotEditorComponent } from './admin-park-pricing-snapshot-editor.component';
import { AdminParkPricingCreditOfferEditorComponent } from './admin-park-pricing-credit-offer-editor.component';

type PricingCollection = 'admissionOffers' | 'annualPasses' | 'parkingOffers';

@Component({
  selector: 'app-admin-park-pricing-tab',
  templateUrl: './admin-park-pricing-tab.component.html',
  styleUrls: ['./admin-park-pricing-tab.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AdminParkPricingOfferEditorComponent,
    AdminParkPricingCreditOfferEditorComponent,
    AdminParkPricingSnapshotEditorComponent,
    ButtonDirective,
    FormsModule,
    LocalizedTextInputComponent,
    TranslateModule,
  ],
})
export class AdminParkPricingTabComponent implements OnChanges {
  @Input() parkId: string | null = null;

  protected readonly pricing = signal<ParkPricing | null>(null);
  protected readonly errorMessageKey = signal<string | null>(null);
  protected readonly loaded = signal<boolean>(false);

  private readonly editStateFacade: AdminParkEditStateFacade = inject(AdminParkEditStateFacade);
  private readonly toastMessageService: ToastMessageService = inject(ToastMessageService);
  private readonly translateService: TranslateService = inject(TranslateService);
  private readonly offerClientKeys: Record<PricingCollection, string[]> = {
    admissionOffers: [],
    annualPasses: [],
    parkingOffers: [],
  };
  private readonly creditOfferClientKeys: string[] = [];
  private offerClientKeySequence: number = 0;

  protected get loading(): boolean {
    return this.editStateFacade.pricingLoading();
  }

  protected get saving(): boolean {
    return this.editStateFacade.pricingSaving();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['parkId'] && this.parkId) {
      this.loaded.set(false);
      this.pricing.set(null);
      this.resetOfferClientKeys();
      void this.loadPricing(false);
    }
  }

  protected reload(): void {
    void this.loadPricing(true);
  }

  protected updateRootField(
    field: 'currencyCode' | 'sourceUrl' | 'purchaseUrl',
    value: string | null
  ): void {
    const current: ParkPricing | null = this.pricing();
    if (!current) {
      return;
    }

    if (field === 'currencyCode') {
      this.pricing.set({ ...current, currencyCode: value ?? '' });
    } else if (field === 'sourceUrl') {
      this.pricing.set({ ...current, sourceUrl: value });
    } else if (field === 'purchaseUrl') {
      this.pricing.set({ ...current, purchaseUrl: value });
    }
    this.errorMessageKey.set(null);
  }

  protected updateNotes(values: LocalizedItem<string>[]): void {
    const current: ParkPricing | null = this.pricing();
    if (!current) {
      return;
    }

    this.pricing.set({ ...current, notes: values });
    this.errorMessageKey.set(null);
  }

  protected lastVerifiedInputValue(value: string | null | undefined): string {
    return value ? value.slice(0, 16) : '';
  }

  protected updateLastVerified(value: string): void {
    const current: ParkPricing | null = this.pricing();
    if (!current) {
      return;
    }

    const normalizedValue: string | null = value
      ? new Date(`${value}:00Z`).toISOString()
      : null;
    this.pricing.set({ ...current, lastVerifiedAtUtc: normalizedValue });
  }

  protected addAdmissionOffer(): void {
    const current: ParkPricing | null = this.pricing();
    if (!current) {
      return;
    }

    const offer: ParkAdmissionPriceOffer = {
      code: this.nextCode('admission', current.admissionOffers.map((item: ParkAdmissionPriceOffer): string => item.code)),
      audienceCategory: 'adult',
      labels: [],
      onlinePrice: { mode: 'Fixed', amount: null },
      gatePrice: null,
      validFrom: null,
      validTo: null,
      purchaseUrl: null,
      conditions: [],
      sortOrder: this.nextSortOrder(current.admissionOffers),
    };
    this.offerClientKeys.admissionOffers.push(this.nextOfferClientKey('admissionOffers'));
    this.pricing.set({ ...current, admissionOffers: [...current.admissionOffers, offer] });
  }

  protected addCreditOffer(): void {
    const current: ParkPricing | null = this.pricing();
    if (!current) {
      return;
    }

    const offers: ParkCreditOffer[] = current.creditOffers ?? [];
    const offer: ParkCreditOffer = {
      unitCode: 'token',
      quantity: 1,
      labels: [],
      prices: { onlinePrice: null, gatePrice: null },
      validFrom: null,
      validTo: null,
      purchaseUrl: null,
      conditions: [],
      sortOrder: this.nextSortOrder(offers)
    };
    this.creditOfferClientKeys.push(this.nextOfferClientKey('creditOffers'));
    this.pricing.set({ ...current, creditOffers: [...offers, offer] });
  }

  protected creditOfferTrackKey(index: number): string {
    let key: string | undefined = this.creditOfferClientKeys[index];
    if (!key) {
      key = this.nextOfferClientKey('creditOffers');
      this.creditOfferClientKeys[index] = key;
    }

    return key;
  }

  protected updateCreditOffer(index: number, offer: ParkCreditOffer): void {
    const current: ParkPricing | null = this.pricing();
    if (!current) {
      return;
    }

    this.pricing.set({
      ...current,
      creditOffers: (current.creditOffers ?? []).map(
        (item: ParkCreditOffer, itemIndex: number): ParkCreditOffer => itemIndex === index ? offer : item)
    });
  }

  protected removeCreditOffer(index: number): void {
    const current: ParkPricing | null = this.pricing();
    if (!current) {
      return;
    }

    this.creditOfferClientKeys.splice(index, 1);
    this.pricing.set({
      ...current,
      creditOffers: (current.creditOffers ?? []).filter(
        (_item: ParkCreditOffer, itemIndex: number): boolean => itemIndex !== index)
    });
  }

  protected addAnnualPass(): void {
    const current: ParkPricing | null = this.pricing();
    if (!current) {
      return;
    }

    const offer: ParkAnnualPassOffer = {
      code: this.nextCode('annual-pass', current.annualPasses.map((item: ParkAnnualPassOffer): string => item.code)),
      names: [],
      onlinePrice: { mode: 'Fixed', amount: null },
      gatePrice: null,
      validFrom: null,
      validTo: null,
      purchaseUrl: null,
      conditions: [],
      sortOrder: this.nextSortOrder(current.annualPasses),
    };
    this.offerClientKeys.annualPasses.push(this.nextOfferClientKey('annualPasses'));
    this.pricing.set({ ...current, annualPasses: [...current.annualPasses, offer] });
  }

  protected addParkingOffer(): void {
    const current: ParkPricing | null = this.pricing();
    if (!current) {
      return;
    }

    const offer: ParkParkingPriceOffer = {
      code: this.nextCode('parking', current.parkingOffers.map((item: ParkParkingPriceOffer): string => item.code)),
      labels: [],
      onlinePrice: null,
      gatePrice: { mode: 'Fixed', amount: null },
      validFrom: null,
      validTo: null,
      purchaseUrl: null,
      conditions: [],
      sortOrder: this.nextSortOrder(current.parkingOffers),
    };
    this.offerClientKeys.parkingOffers.push(this.nextOfferClientKey('parkingOffers'));
    this.pricing.set({ ...current, parkingOffers: [...current.parkingOffers, offer] });
  }

  protected offerTrackKey(collection: PricingCollection, index: number): string {
    let key: string | undefined = this.offerClientKeys[collection][index];
    if (!key) {
      key = this.nextOfferClientKey(collection);
      this.offerClientKeys[collection][index] = key;
    }

    return key;
  }

  protected updateOffer(collection: PricingCollection, index: number, offer: AdminParkPricingOffer): void {
    const current: ParkPricing | null = this.pricing();
    if (!current) {
      return;
    }

    if (collection === 'admissionOffers') {
      const admissionOffer: ParkAdmissionPriceOffer = offer as ParkAdmissionPriceOffer;
      this.pricing.set({
        ...current,
        admissionOffers: current.admissionOffers.map((item: ParkAdmissionPriceOffer, itemIndex: number): ParkAdmissionPriceOffer =>
          itemIndex === index ? admissionOffer : item),
      });
      return;
    }

    if (collection === 'annualPasses') {
      const annualPass: ParkAnnualPassOffer = offer as ParkAnnualPassOffer;
      this.pricing.set({
        ...current,
        annualPasses: current.annualPasses.map((item: ParkAnnualPassOffer, itemIndex: number): ParkAnnualPassOffer =>
          itemIndex === index ? annualPass : item),
      });
      return;
    }

    const parkingOffer: ParkParkingPriceOffer = offer as ParkParkingPriceOffer;
    this.pricing.set({
      ...current,
      parkingOffers: current.parkingOffers.map((item: ParkParkingPriceOffer, itemIndex: number): ParkParkingPriceOffer =>
        itemIndex === index ? parkingOffer : item),
    });
  }

  protected removeOffer(collection: PricingCollection, index: number): void {
    const current: ParkPricing | null = this.pricing();
    if (!current) {
      return;
    }

    this.offerClientKeys[collection].splice(index, 1);
    if (collection === 'admissionOffers') {
      this.pricing.set({
        ...current,
        admissionOffers: current.admissionOffers.filter(
          (_item: ParkAdmissionPriceOffer, itemIndex: number): boolean => itemIndex !== index),
      });
    } else if (collection === 'annualPasses') {
      this.pricing.set({
        ...current,
        annualPasses: current.annualPasses.filter(
          (_item: ParkAnnualPassOffer, itemIndex: number): boolean => itemIndex !== index),
      });
    } else {
      this.pricing.set({
        ...current,
        parkingOffers: current.parkingOffers.filter(
          (_item: ParkParkingPriceOffer, itemIndex: number): boolean => itemIndex !== index),
      });
    }
  }

  protected addHistoricalSnapshot(): void {
    const current: ParkPricing | null = this.pricing();
    if (!current) {
      return;
    }

    const snapshots: ParkPricingSnapshot[] = current.historicalSnapshots ?? [];
    const usedYears = new Set(snapshots.map((snapshot: ParkPricingSnapshot): number => snapshot.year));
    let year: number = new Date().getUTCFullYear() - 1;
    while (year >= 1900 && usedYears.has(year)) {
      year -= 1;
    }

    const snapshot: ParkPricingSnapshot = {
      year,
      currencyCode: current.currencyCode,
      sourceUrl: null,
      notes: [],
      lastVerifiedAtUtc: null,
      admissionOffers: [],
      annualPasses: [],
      parkingOffers: [],
      creditOffers: []
    };

    this.pricing.set({ ...current, historicalSnapshots: [snapshot, ...snapshots] });
    this.errorMessageKey.set(null);
  }

  protected updateHistoricalSnapshot(index: number, snapshot: ParkPricingSnapshot): void {
    const current: ParkPricing | null = this.pricing();
    if (!current) {
      return;
    }

    this.pricing.set({
      ...current,
      historicalSnapshots: (current.historicalSnapshots ?? []).map(
        (item: ParkPricingSnapshot, itemIndex: number): ParkPricingSnapshot =>
          itemIndex === index ? snapshot : item)
    });
    this.errorMessageKey.set(null);
  }

  protected removeHistoricalSnapshot(index: number): void {
    const current: ParkPricing | null = this.pricing();
    if (!current) {
      return;
    }

    this.pricing.set({
      ...current,
      historicalSnapshots: (current.historicalSnapshots ?? []).filter(
        (_item: ParkPricingSnapshot, itemIndex: number): boolean => itemIndex !== index)
    });
    this.errorMessageKey.set(null);
  }

  protected async save(): Promise<void> {
    const parkId: string | null = this.parkId;
    const current: ParkPricing | null = this.pricing();
    if (!parkId || !current || this.saving) {
      return;
    }

    const payload: ParkPricing = {
      ...current,
      parkId,
      currencyCode: current.currencyCode.trim().toUpperCase(),
      sourceUrl: this.normalizeOptionalText(current.sourceUrl),
      purchaseUrl: this.normalizeOptionalText(current.purchaseUrl),
      notes: current.notes,
      historicalSnapshots: (current.historicalSnapshots ?? []).map(
        (snapshot: ParkPricingSnapshot): ParkPricingSnapshot => ({
          ...snapshot,
          currencyCode: snapshot.currencyCode.trim().toUpperCase(),
          sourceUrl: this.normalizeOptionalText(snapshot.sourceUrl),
          notes: snapshot.notes
        }))
    };

    try {
      const savedPricing: ParkPricing = await this.editStateFacade.savePricing(parkId, payload);
      this.resetOfferClientKeys(savedPricing);
      this.pricing.set(savedPricing);
      this.loaded.set(true);
      this.errorMessageKey.set(null);
      this.toastMessageService.add(
        'success',
        this.translateService.instant('adminParkPricing.messages.savedSummary'),
        this.translateService.instant('adminParkPricing.messages.savedDetail')
      );
    } catch (error: unknown) {
      console.error('Error saving park pricing', error);
      this.errorMessageKey.set('adminParkPricing.messages.saveError');
    }
  }

  private async loadPricing(force: boolean): Promise<void> {
    const parkId: string | null = this.parkId;
    if (!parkId || (!force && this.loaded())) {
      return;
    }

    try {
      const loadedPricing: ParkPricing = await this.editStateFacade.loadPricing(parkId);
      const pricing: ParkPricing = {
        ...loadedPricing,
        creditOffers: loadedPricing.creditOffers ?? [],
        historicalSnapshots: loadedPricing.historicalSnapshots ?? []
      };
      this.resetOfferClientKeys(pricing);
      this.pricing.set(pricing);
      this.loaded.set(true);
      this.errorMessageKey.set(null);
    } catch (error: unknown) {
      if (hasHttpStatus(error, 404)) {
        const pricing: ParkPricing = this.createTemplate(parkId);
        this.resetOfferClientKeys(pricing);
        this.pricing.set(pricing);
        this.loaded.set(true);
        this.errorMessageKey.set(null);
        return;
      }

      console.error('Error loading park pricing', error);
      this.errorMessageKey.set('adminParkPricing.messages.loadError');
    }
  }

  private createTemplate(parkId: string): ParkPricing {
    return {
      parkId,
      currencyCode: 'EUR',
      sourceUrl: null,
      purchaseUrl: null,
      notes: [],
      lastVerifiedAtUtc: null,
      admissionOffers: [],
      annualPasses: [],
      parkingOffers: [],
      creditOffers: [],
      historicalSnapshots: [],
    };
  }

  private nextCode(prefix: string, codes: readonly string[]): string {
    let sequence: number = codes.length + 1;
    let candidate: string = `${prefix}-${sequence}`;
    while (codes.includes(candidate)) {
      sequence += 1;
      candidate = `${prefix}-${sequence}`;
    }

    return candidate;
  }

  private nextSortOrder(offers: readonly { sortOrder: number }[]): number {
    const highestExplicitOrder: number = offers.reduce(
      (maximum: number, offer: { sortOrder: number }): number => Math.max(maximum, offer.sortOrder),
      0);
    return Math.max(highestExplicitOrder, offers.length) + 1;
  }

  private resetOfferClientKeys(pricing?: ParkPricing): void {
    this.offerClientKeys.admissionOffers = (pricing?.admissionOffers ?? [])
      .map((): string => this.nextOfferClientKey('admissionOffers'));
    this.offerClientKeys.annualPasses = (pricing?.annualPasses ?? [])
      .map((): string => this.nextOfferClientKey('annualPasses'));
    this.offerClientKeys.parkingOffers = (pricing?.parkingOffers ?? [])
      .map((): string => this.nextOfferClientKey('parkingOffers'));
    this.creditOfferClientKeys.splice(0, this.creditOfferClientKeys.length,
      ...(pricing?.creditOffers ?? []).map((): string => this.nextOfferClientKey('creditOffers')));
  }

  private nextOfferClientKey(collection: PricingCollection | 'creditOffers'): string {
    this.offerClientKeySequence += 1;
    return `${collection}-${this.offerClientKeySequence}`;
  }

  private normalizeOptionalText(value: string | null | undefined): string | null {
    const normalizedValue: string = value?.trim() ?? '';
    return normalizedValue || null;
  }
}
