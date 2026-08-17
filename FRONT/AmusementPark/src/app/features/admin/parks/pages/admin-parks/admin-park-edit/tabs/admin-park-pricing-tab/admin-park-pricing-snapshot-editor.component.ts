import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import {
  ParkAdmissionPriceOffer,
  ParkAnnualPassOffer,
  ParkCreditOffer,
  ParkParkingPriceOffer,
  ParkPricingSnapshot
} from '@app/models/parks/park-pricing';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { LocalizedTextInputComponent } from '@shared/components/localized-text-input/localized-text-input.component';
import { ButtonDirective } from '@shared/ui/primitives/button';
import {
  AdminParkPricingOffer,
  AdminParkPricingOfferEditorComponent
} from './admin-park-pricing-offer-editor.component';
import { AdminParkPricingCreditOfferEditorComponent } from './admin-park-pricing-credit-offer-editor.component';

type SnapshotPricingCollection = 'admissionOffers' | 'annualPasses' | 'parkingOffers';

@Component({
  selector: 'app-admin-park-pricing-snapshot-editor',
  templateUrl: './admin-park-pricing-snapshot-editor.component.html',
  styleUrls: ['./admin-park-pricing-snapshot-editor.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AdminParkPricingOfferEditorComponent,
    AdminParkPricingCreditOfferEditorComponent,
    ButtonDirective,
    FormsModule,
    LocalizedTextInputComponent,
    TranslateModule
  ]
})
export class AdminParkPricingSnapshotEditorComponent {
  @Input({ required: true }) snapshot!: ParkPricingSnapshot;
  @Input() disabled: boolean = false;

  @Output() readonly snapshotChange = new EventEmitter<ParkPricingSnapshot>();
  @Output() readonly remove = new EventEmitter<void>();

  protected updateYear(value: number | string | null): void {
    this.emit({ year: Number(value) || 0 });
  }

  protected updateCurrencyCode(value: string | null): void {
    this.emit({ currencyCode: value ?? '' });
  }

  protected updateSourceUrl(value: string | null): void {
    this.emit({ sourceUrl: value });
  }

  protected updateNotes(notes: LocalizedItem<string>[]): void {
    this.emit({ notes });
  }

  protected lastVerifiedInputValue(value: string | null | undefined): string {
    return value ? value.slice(0, 16) : '';
  }

  protected updateLastVerified(value: string): void {
    this.emit({
      lastVerifiedAtUtc: value ? new Date(`${value}:00Z`).toISOString() : null
    });
  }

  protected addCreditOffer(): void {
    const offers: ParkCreditOffer[] = this.snapshot.creditOffers ?? [];
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
    this.emit({ creditOffers: [...offers, offer] });
  }

  protected updateCreditOffer(index: number, offer: ParkCreditOffer): void {
    this.emit({
      creditOffers: (this.snapshot.creditOffers ?? []).map(
        (item: ParkCreditOffer, itemIndex: number): ParkCreditOffer => itemIndex === index ? offer : item)
    });
  }

  protected removeCreditOffer(index: number): void {
    this.emit({
      creditOffers: (this.snapshot.creditOffers ?? []).filter(
        (_item: ParkCreditOffer, itemIndex: number): boolean => itemIndex !== index)
    });
  }

  protected addOffer(collection: SnapshotPricingCollection): void {
    if (collection === 'admissionOffers') {
      const offer: ParkAdmissionPriceOffer = {
        code: this.nextCode('admission', this.snapshot.admissionOffers),
        audienceCategory: 'adult',
        labels: [],
        onlinePrice: { mode: 'Fixed', amount: null },
        gatePrice: null,
        validFrom: null,
        validTo: null,
        purchaseUrl: null,
        conditions: [],
        sortOrder: this.nextSortOrder(this.snapshot.admissionOffers)
      };
      this.emit({ admissionOffers: [...this.snapshot.admissionOffers, offer] });
      return;
    }

    if (collection === 'annualPasses') {
      const offer: ParkAnnualPassOffer = {
        code: this.nextCode('annual-pass', this.snapshot.annualPasses),
        names: [],
        onlinePrice: { mode: 'Fixed', amount: null },
        gatePrice: null,
        validFrom: null,
        validTo: null,
        purchaseUrl: null,
        conditions: [],
        sortOrder: this.nextSortOrder(this.snapshot.annualPasses)
      };
      this.emit({ annualPasses: [...this.snapshot.annualPasses, offer] });
      return;
    }

    const offer: ParkParkingPriceOffer = {
      code: this.nextCode('parking', this.snapshot.parkingOffers),
      labels: [],
      onlinePrice: null,
      gatePrice: { mode: 'Fixed', amount: null },
      validFrom: null,
      validTo: null,
      purchaseUrl: null,
      conditions: [],
      sortOrder: this.nextSortOrder(this.snapshot.parkingOffers)
    };
    this.emit({ parkingOffers: [...this.snapshot.parkingOffers, offer] });
  }

  protected updateOffer(collection: SnapshotPricingCollection, index: number, offer: AdminParkPricingOffer): void {
    if (collection === 'admissionOffers') {
      this.emit({
        admissionOffers: this.snapshot.admissionOffers.map(
          (item: ParkAdmissionPriceOffer, itemIndex: number): ParkAdmissionPriceOffer =>
            itemIndex === index ? offer as ParkAdmissionPriceOffer : item)
      });
      return;
    }

    if (collection === 'annualPasses') {
      this.emit({
        annualPasses: this.snapshot.annualPasses.map(
          (item: ParkAnnualPassOffer, itemIndex: number): ParkAnnualPassOffer =>
            itemIndex === index ? offer as ParkAnnualPassOffer : item)
      });
      return;
    }

    this.emit({
      parkingOffers: this.snapshot.parkingOffers.map(
        (item: ParkParkingPriceOffer, itemIndex: number): ParkParkingPriceOffer =>
          itemIndex === index ? offer as ParkParkingPriceOffer : item)
    });
  }

  protected removeOffer(collection: SnapshotPricingCollection, index: number): void {
    if (collection === 'admissionOffers') {
      this.emit({ admissionOffers: this.snapshot.admissionOffers.filter((_item, itemIndex): boolean => itemIndex !== index) });
      return;
    }

    if (collection === 'annualPasses') {
      this.emit({ annualPasses: this.snapshot.annualPasses.filter((_item, itemIndex): boolean => itemIndex !== index) });
      return;
    }

    this.emit({ parkingOffers: this.snapshot.parkingOffers.filter((_item, itemIndex): boolean => itemIndex !== index) });
  }

  private emit(changes: Partial<ParkPricingSnapshot>): void {
    this.snapshotChange.emit({ ...this.snapshot, ...changes });
  }

  private nextCode(prefix: string, offers: readonly { code: string }[]): string {
    const codes: string[] = offers.map((offer: { code: string }): string => offer.code);
    let sequence: number = codes.length + 1;
    let candidate: string = `${prefix}-${sequence}`;
    while (codes.includes(candidate)) {
      sequence += 1;
      candidate = `${prefix}-${sequence}`;
    }

    return candidate;
  }

  private nextSortOrder(offers: readonly { sortOrder: number }[]): number {
    return offers.reduce(
      (maximum: number, offer: { sortOrder: number }): number => Math.max(maximum, offer.sortOrder),
      0) + 1;
  }
}
