import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import {
  ParkAdmissionPriceOffer,
  ParkCreditOffer,
  ParkPriceValue,
  ParkPricing
} from '@app/models/parks/park-pricing';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { ScreenState } from '@shared/models/contracts';
import { SafeExternalUrlPipe } from '@shared/pipes';
import { resolveLocalizedText } from '@shared/utils/localization/localized-text.helpers';

interface StandaloneAttractionPriceSummary {
  key: string;
  label: string;
  price: string;
}

@Component({
  selector: 'app-standalone-attraction-pricing-card',
  standalone: true,
  imports: [TranslateModule, SafeExternalUrlPipe],
  templateUrl: './standalone-attraction-pricing-card.component.html',
  styleUrl: './standalone-attraction-pricing-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StandaloneAttractionPricingCardComponent {
  @Input() pricing: ParkPricing | null = null;
  @Input() state: ScreenState<unknown, string> | null = null;
  @Input() currentLanguage: string = 'en';

  get isLoading(): boolean {
    return this.state?.kind === 'loading';
  }

  get summaries(): StandaloneAttractionPriceSummary[] {
    if (!this.pricing) {
      return [];
    }

    const admissionSummaries: StandaloneAttractionPriceSummary[] = this.pricing.admissionOffers
      .map((offer: ParkAdmissionPriceOffer): StandaloneAttractionPriceSummary | null => {
        const price: string = this.formatPriceValue(
          offer.onlinePrice ?? offer.gatePrice ?? null,
          this.pricing!.currencyCode
        );
        return price.length === 0
          ? null
          : {
              key: offer.id || offer.code,
              label: this.localizedText(offer.labels, offer.code),
              price
            };
      })
      .filter((summary: StandaloneAttractionPriceSummary | null): summary is StandaloneAttractionPriceSummary => summary !== null);
    const creditSummaries: StandaloneAttractionPriceSummary[] = (this.pricing.creditOffers ?? [])
      .map((offer: ParkCreditOffer): StandaloneAttractionPriceSummary | null => {
        const amount: number | null = offer.prices.onlinePrice ?? offer.prices.gatePrice ?? null;
        return amount === null
          ? null
          : {
              key: offer.id || `${offer.unitCode}:${offer.quantity}`,
              label: this.localizedText(offer.labels, `${offer.quantity} ${offer.unitCode}`),
              price: this.formatAmount(amount, this.pricing!.currencyCode)
            };
      })
      .filter((summary: StandaloneAttractionPriceSummary | null): summary is StandaloneAttractionPriceSummary => summary !== null);

    return [...admissionSummaries, ...creditSummaries].slice(0, 4);
  }

  private localizedText(values: LocalizedItem<string>[], fallback: string): string {
    return resolveLocalizedText(values, this.currentLanguage, fallback);
  }

  private formatPriceValue(value: ParkPriceValue | null, currencyCode: string): string {
    if (!value) {
      return '';
    }

    if (Number.isFinite(value.amount)) {
      return this.formatAmount(value.amount!, currencyCode);
    }

    const minimum: string | null = Number.isFinite(value.minimumAmount)
      ? this.formatAmount(value.minimumAmount!, currencyCode)
      : null;
    const maximum: string | null = Number.isFinite(value.maximumAmount)
      ? this.formatAmount(value.maximumAmount!, currencyCode)
      : null;
    if (minimum && maximum) {
      return `${minimum} – ${maximum}`;
    }

    return minimum ? `≥ ${minimum}` : maximum ? `≤ ${maximum}` : '';
  }

  private formatAmount(amount: number, currencyCode: string): string {
    try {
      return new Intl.NumberFormat(this.currentLanguage, {
        style: 'currency',
        currency: currencyCode,
        maximumFractionDigits: 2
      }).format(amount);
    } catch {
      return `${new Intl.NumberFormat(this.currentLanguage).format(amount)} ${currencyCode}`;
    }
  }
}
