import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  ParkAdmissionPriceOffer,
  ParkAnnualPassOffer,
  ParkCreditOffer,
  ParkParkingPriceOffer,
  ParkPriceValue,
  ParkPricing
} from '@app/models/parks/park-pricing';
import { LocalizedItem } from '@app/models/shared/localized-item';
import {
  formatParkPrice,
  ParkPriceFormattingLabels
} from '@app/features/public/parks/models/park-pricing.presentation';
import { ScreenState } from '@shared/models/contracts';
import { SafeExternalUrlPipe } from '@shared/pipes';
import { resolveLocalizedText } from '@shared/utils/localization/localized-text.helpers';

interface StandaloneAttractionPriceSummary {
  key: string;
  label: string;
  price: string;
  purchaseUrl: string | null;
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

  constructor(private readonly ngxTranslateService: TranslateService) {}

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
              key: `admission:${offer.id || offer.code}`,
              label: this.localizedText(offer.labels, offer.code),
              price,
              purchaseUrl: offer.purchaseUrl ?? null
            };
      })
      .filter((summary: StandaloneAttractionPriceSummary | null): summary is StandaloneAttractionPriceSummary => summary !== null);
    const annualPassSummaries: StandaloneAttractionPriceSummary[] = this.pricing.annualPasses
      .map((offer: ParkAnnualPassOffer): StandaloneAttractionPriceSummary | null => {
        const price: string = this.formatPriceValue(
          offer.onlinePrice ?? offer.gatePrice ?? null,
          this.pricing!.currencyCode
        );
        return price.length === 0
          ? null
          : {
              key: `annual-pass:${offer.id || offer.code}`,
              label: this.localizedText(offer.names, offer.code),
              price,
              purchaseUrl: offer.purchaseUrl ?? null
            };
      })
      .filter((summary: StandaloneAttractionPriceSummary | null): summary is StandaloneAttractionPriceSummary => summary !== null);
    const parkingSummaries: StandaloneAttractionPriceSummary[] = this.pricing.parkingOffers
      .map((offer: ParkParkingPriceOffer): StandaloneAttractionPriceSummary | null => {
        const price: string = this.formatPriceValue(
          offer.onlinePrice ?? offer.gatePrice ?? null,
          this.pricing!.currencyCode
        );
        return price.length === 0
          ? null
          : {
              key: `parking:${offer.id || offer.code}`,
              label: this.localizedText(offer.labels, offer.code),
              price,
              purchaseUrl: offer.purchaseUrl ?? null
            };
      })
      .filter((summary: StandaloneAttractionPriceSummary | null): summary is StandaloneAttractionPriceSummary => summary !== null);
    const creditSummaries: StandaloneAttractionPriceSummary[] = (this.pricing.creditOffers ?? [])
      .map((offer: ParkCreditOffer): StandaloneAttractionPriceSummary | null => {
        const amount: number | null = offer.prices.onlinePrice ?? offer.prices.gatePrice ?? null;
        return amount === null
          ? null
          : {
              key: `credit:${offer.id || `${offer.unitCode}:${offer.quantity}`}`,
              label: this.localizedText(offer.labels, `${offer.quantity} ${offer.unitCode}`),
              price: this.formatAmount(amount, this.pricing!.currencyCode),
              purchaseUrl: offer.purchaseUrl ?? null
            };
      })
      .filter((summary: StandaloneAttractionPriceSummary | null): summary is StandaloneAttractionPriceSummary => summary !== null);

    return [
      ...admissionSummaries,
      ...annualPassSummaries,
      ...parkingSummaries,
      ...creditSummaries
    ];
  }

  private localizedText(values: LocalizedItem<string>[], fallback: string): string {
    return resolveLocalizedText(values, this.currentLanguage, fallback);
  }

  private formatPriceValue(value: ParkPriceValue | null, currencyCode: string): string {
    const labels: ParkPriceFormattingLabels = {
      from: this.ngxTranslateService.instant('parkPricing.price.from'),
      upTo: this.ngxTranslateService.instant('parkPricing.price.upTo'),
      dynamic: this.ngxTranslateService.instant('parkPricing.price.dynamic')
    };
    return formatParkPrice(value, currencyCode, this.currentLanguage, labels) ?? '';
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
