import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import {
  ParkAdmissionPriceOffer,
  ParkAnnualPassOffer,
  ParkParkingPriceOffer,
  ParkPriceValue,
  ParkPricingMode,
} from '@app/models/parks/park-pricing';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { LocalizedTextInputComponent } from '@shared/components/localized-text-input/localized-text-input.component';
import { ButtonDirective } from '@shared/ui/primitives/button';

export type AdminParkPricingOfferKind = 'admission' | 'annualPass' | 'parking';
export type AdminParkPricingOffer = ParkAdmissionPriceOffer | ParkAnnualPassOffer | ParkParkingPriceOffer;
type PriceChannel = 'onlinePrice' | 'gatePrice';
type PriceAmountField = 'amount' | 'minimumAmount' | 'maximumAmount';

@Component({
  selector: 'app-admin-park-pricing-offer-editor',
  templateUrl: './admin-park-pricing-offer-editor.component.html',
  styleUrls: ['./admin-park-pricing-offer-editor.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonDirective, FormsModule, LocalizedTextInputComponent, TranslateModule],
})
export class AdminParkPricingOfferEditorComponent {
  @Input({ required: true }) offer!: AdminParkPricingOffer;
  @Input({ required: true }) kind!: AdminParkPricingOfferKind;
  @Input() disabled: boolean = false;
  @Output() readonly offerChange = new EventEmitter<AdminParkPricingOffer>();
  @Output() readonly remove = new EventEmitter<void>();
  protected readonly priceChannels: readonly PriceChannel[] = ['onlinePrice', 'gatePrice'];

  protected audienceCategory(): string {
    return this.kind === 'admission'
      ? (this.offer as ParkAdmissionPriceOffer).audienceCategory
      : '';
  }

  protected titleValues(): LocalizedItem<string>[] {
    return this.kind === 'annualPass'
      ? (this.offer as ParkAnnualPassOffer).names
      : (this.offer as ParkAdmissionPriceOffer | ParkParkingPriceOffer).labels;
  }

  protected updateTitleValues(values: LocalizedItem<string>[]): void {
    this.emitPatch(this.kind === 'annualPass' ? { names: values } : { labels: values });
  }

  protected updateConditions(values: LocalizedItem<string>[]): void {
    this.emitPatch({ conditions: values });
  }

  protected updateText(
    field: 'code' | 'audienceCategory' | 'validFrom' | 'validTo' | 'purchaseUrl',
    value: string | null
  ): void {
    if (field === 'code') {
      this.emitPatch({ code: value ?? '' });
      return;
    }

    if (field === 'audienceCategory') {
      this.emitPatch({ audienceCategory: value ?? '' });
      return;
    }

    this.emitPatch({ [field]: value || null });
  }

  protected updateSortOrder(value: number | null): void {
    this.emitPatch({ sortOrder: this.toNumber(value) ?? 0 });
  }

  protected price(channel: PriceChannel): ParkPriceValue | null {
    return this.offer[channel] ?? null;
  }

  protected priceMode(channel: PriceChannel): ParkPricingMode | '' {
    return this.price(channel)?.mode ?? '';
  }

  protected updatePriceMode(channel: PriceChannel, mode: ParkPricingMode | ''): void {
    if (!mode) {
      this.emitPatch({ [channel]: null });
      return;
    }

    const current: ParkPriceValue | null = this.price(channel);
    let value: ParkPriceValue;
    if (mode === 'Fixed') {
      value = {
        mode,
        amount: current?.amount ?? current?.minimumAmount ?? null,
        minimumAmount: null,
        maximumAmount: null,
      };
    } else {
      value = {
        mode,
        amount: null,
        minimumAmount: current?.minimumAmount ?? current?.amount ?? null,
        maximumAmount: current?.maximumAmount ?? null,
      };
    }

    this.emitPatch({ [channel]: value });
  }

  protected updatePriceAmount(channel: PriceChannel, field: PriceAmountField, value: number | null): void {
    const current: ParkPriceValue | null = this.price(channel);
    if (!current) {
      return;
    }

    this.emitPatch({
      [channel]: {
        ...current,
        [field]: this.toNumber(value),
      },
    });
  }

  private emitPatch(patch: Record<string, unknown>): void {
    this.offerChange.emit({ ...this.offer, ...patch } as AdminParkPricingOffer);
  }

  private toNumber(value: number | null): number | null {
    return value === null || !Number.isFinite(value) ? null : value;
  }
}
