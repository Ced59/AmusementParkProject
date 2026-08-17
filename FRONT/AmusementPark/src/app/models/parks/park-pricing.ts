import { LocalizedItem } from '@app/models/shared/localized-item';

export type ParkPricingMode = 'Fixed' | 'Range' | 'Dynamic';

export interface ParkPriceValue {
  mode: ParkPricingMode;
  amount?: number | null;
  minimumAmount?: number | null;
  maximumAmount?: number | null;
}

export interface ParkAdmissionPriceOffer {
  id?: string | null;
  code: string;
  audienceCategory: string;
  labels: LocalizedItem<string>[];
  onlinePrice?: ParkPriceValue | null;
  gatePrice?: ParkPriceValue | null;
  validFrom?: string | null;
  validTo?: string | null;
  purchaseUrl?: string | null;
  conditions: LocalizedItem<string>[];
  sortOrder: number;
}

export interface ParkAnnualPassOffer {
  id?: string | null;
  code: string;
  names: LocalizedItem<string>[];
  onlinePrice?: ParkPriceValue | null;
  gatePrice?: ParkPriceValue | null;
  validFrom?: string | null;
  validTo?: string | null;
  purchaseUrl?: string | null;
  conditions: LocalizedItem<string>[];
  sortOrder: number;
}

export interface ParkParkingPriceOffer {
  id?: string | null;
  code: string;
  labels: LocalizedItem<string>[];
  onlinePrice?: ParkPriceValue | null;
  gatePrice?: ParkPriceValue | null;
  validFrom?: string | null;
  validTo?: string | null;
  purchaseUrl?: string | null;
  conditions: LocalizedItem<string>[];
  sortOrder: number;
}

export interface ParkCreditOfferPrices {
  onlinePrice?: number | null;
  gatePrice?: number | null;
}

export interface ParkCreditOffer {
  id?: string | null;
  unitCode: string;
  quantity: number;
  labels: LocalizedItem<string>[];
  prices: ParkCreditOfferPrices;
  validFrom?: string | null;
  validTo?: string | null;
  purchaseUrl?: string | null;
  conditions: LocalizedItem<string>[];
  sortOrder: number;
}

export interface ParkPricing {
  parkId: string;
  currencyCode: string;
  sourceUrl?: string | null;
  purchaseUrl?: string | null;
  notes: LocalizedItem<string>[];
  lastVerifiedAtUtc?: string | null;
  createdAtUtc?: string | null;
  updatedAtUtc?: string | null;
  admissionOffers: ParkAdmissionPriceOffer[];
  annualPasses: ParkAnnualPassOffer[];
  parkingOffers: ParkParkingPriceOffer[];
  creditOffers?: ParkCreditOffer[];
  historicalSnapshots?: ParkPricingSnapshot[];
}

export interface ParkPricingSnapshot {
  id?: string | null;
  year: number;
  currencyCode: string;
  sourceUrl?: string | null;
  notes: LocalizedItem<string>[];
  lastVerifiedAtUtc?: string | null;
  admissionOffers: ParkAdmissionPriceOffer[];
  annualPasses: ParkAnnualPassOffer[];
  parkingOffers: ParkParkingPriceOffer[];
  creditOffers?: ParkCreditOffer[];
}
