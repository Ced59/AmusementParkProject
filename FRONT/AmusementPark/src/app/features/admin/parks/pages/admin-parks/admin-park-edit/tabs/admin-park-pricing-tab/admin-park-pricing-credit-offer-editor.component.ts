import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { ParkCreditOffer } from '@app/models/parks/park-pricing';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { LocalizedTextInputComponent } from '@shared/components/localized-text-input/localized-text-input.component';
import { ButtonDirective } from '@shared/ui/primitives/button';

@Component({
  selector: 'app-admin-park-pricing-credit-offer-editor',
  templateUrl: './admin-park-pricing-credit-offer-editor.component.html',
  styleUrls: ['./admin-park-pricing-credit-offer-editor.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonDirective, FormsModule, LocalizedTextInputComponent, TranslateModule]
})
export class AdminParkPricingCreditOfferEditorComponent {
  @Input({ required: true }) offer!: ParkCreditOffer;
  @Input() disabled: boolean = false;

  @Output() readonly offerChange = new EventEmitter<ParkCreditOffer>();
  @Output() readonly remove = new EventEmitter<void>();

  protected updateUnitCode(value: string | null): void {
    this.emit({ unitCode: value ?? '' });
  }

  protected updateQuantity(value: string | number | null): void {
    this.emit({ quantity: Number(value) || 0 });
  }

  protected updateLabels(labels: LocalizedItem<string>[]): void {
    this.emit({ labels });
  }

  protected updatePrice(channel: 'onlinePrice' | 'gatePrice', value: string | number | null): void {
    const parsed: number | null = value === null || value === '' ? null : Number(value);
    this.emit({
      prices: {
        ...this.offer.prices,
        [channel]: parsed !== null && Number.isFinite(parsed) ? parsed : null
      }
    });
  }

  protected updateDate(field: 'validFrom' | 'validTo', value: string | null): void {
    this.emit({ [field]: value || null });
  }

  protected updatePurchaseUrl(value: string | null): void {
    this.emit({ purchaseUrl: value });
  }

  protected updateConditions(conditions: LocalizedItem<string>[]): void {
    this.emit({ conditions });
  }

  protected updateSortOrder(value: string | number | null): void {
    this.emit({ sortOrder: Number(value) || 0 });
  }

  private emit(changes: Partial<ParkCreditOffer>): void {
    this.offerChange.emit({ ...this.offer, ...changes });
  }
}
