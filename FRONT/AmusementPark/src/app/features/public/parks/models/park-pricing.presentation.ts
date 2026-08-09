import { ParkPriceValue, ParkPricingMode } from '@app/models/parks/park-pricing';
import { LocalizedItem } from '@app/models/shared/localized-item';

export interface ParkPricingCopy {
  kicker: string;
  title: string;
  subtitle: (parkName: string) => string;
  admission: string;
  passes: string;
  parking: string;
  online: string;
  gate: string;
  from: string;
  to: string;
  dynamic: string;
  validFrom: string;
  validTo: string;
  conditions: string;
  buy: string;
  officialSource: string;
  verified: string;
  noPricing: string;
  back: string;
}

const COPY: Record<string, ParkPricingCopy> = {
  fr: { kicker: 'Préparer sa visite', title: 'Tarifs', subtitle: (name: string) => `Billets, pass et stationnement pour ${name}.`, admission: 'Billets', passes: 'Pass annuels', parking: 'Parking', online: 'En ligne', gate: 'Sur place', from: 'À partir de', to: 'à', dynamic: 'Tarif dynamique', validFrom: 'Valable à partir du', validTo: 'Valable jusqu’au', conditions: 'Conditions', buy: 'Acheter sur le site officiel', officialSource: 'Source officielle', verified: 'Vérifié le', noPricing: 'Aucun tarif public vérifié n’est disponible pour le moment.', back: 'Retour au parc' },
  en: { kicker: 'Plan your visit', title: 'Prices', subtitle: (name: string) => `Tickets, passes and parking prices for ${name}.`, admission: 'Tickets', passes: 'Annual passes', parking: 'Parking', online: 'Online', gate: 'At the gate', from: 'From', to: 'to', dynamic: 'Dynamic price', validFrom: 'Valid from', validTo: 'Valid until', conditions: 'Conditions', buy: 'Buy on the official website', officialSource: 'Official source', verified: 'Verified on', noPricing: 'No verified public pricing is available yet.', back: 'Back to park' },
  de: { kicker: 'Besuch planen', title: 'Preise', subtitle: (name: string) => `Tickets, Jahreskarten und Parkgebühren für ${name}.`, admission: 'Tickets', passes: 'Jahreskarten', parking: 'Parken', online: 'Online', gate: 'Vor Ort', from: 'Ab', to: 'bis', dynamic: 'Dynamischer Preis', validFrom: 'Gültig ab', validTo: 'Gültig bis', conditions: 'Bedingungen', buy: 'Auf der offiziellen Website kaufen', officialSource: 'Offizielle Quelle', verified: 'Geprüft am', noPricing: 'Derzeit sind keine verifizierten öffentlichen Preise verfügbar.', back: 'Zurück zum Park' },
  es: { kicker: 'Prepara tu visita', title: 'Precios', subtitle: (name: string) => `Entradas, pases y aparcamiento de ${name}.`, admission: 'Entradas', passes: 'Pases anuales', parking: 'Aparcamiento', online: 'En línea', gate: 'En taquilla', from: 'Desde', to: 'hasta', dynamic: 'Precio dinámico', validFrom: 'Válido desde', validTo: 'Válido hasta', conditions: 'Condiciones', buy: 'Comprar en la web oficial', officialSource: 'Fuente oficial', verified: 'Verificado el', noPricing: 'Todavía no hay precios públicos verificados disponibles.', back: 'Volver al parque' },
  it: { kicker: 'Prepara la visita', title: 'Prezzi', subtitle: (name: string) => `Biglietti, pass e parcheggio per ${name}.`, admission: 'Biglietti', passes: 'Pass annuali', parking: 'Parcheggio', online: 'Online', gate: 'In cassa', from: 'Da', to: 'a', dynamic: 'Prezzo dinamico', validFrom: 'Valido dal', validTo: 'Valido fino al', conditions: 'Condizioni', buy: 'Acquista sul sito ufficiale', officialSource: 'Fonte ufficiale', verified: 'Verificato il', noPricing: 'Al momento non sono disponibili prezzi pubblici verificati.', back: 'Torna al parco' },
  nl: { kicker: 'Plan je bezoek', title: 'Prijzen', subtitle: (name: string) => `Tickets, abonnementen en parkeertarieven voor ${name}.`, admission: 'Tickets', passes: 'Jaarpassen', parking: 'Parkeren', online: 'Online', gate: 'Aan de kassa', from: 'Vanaf', to: 'tot', dynamic: 'Dynamische prijs', validFrom: 'Geldig vanaf', validTo: 'Geldig tot', conditions: 'Voorwaarden', buy: 'Koop op de officiële website', officialSource: 'Officiële bron', verified: 'Gecontroleerd op', noPricing: 'Er zijn momenteel geen geverifieerde openbare prijzen beschikbaar.', back: 'Terug naar het park' },
  pl: { kicker: 'Zaplanuj wizytę', title: 'Ceny', subtitle: (name: string) => `Bilety, karnety i parking w ${name}.`, admission: 'Bilety', passes: 'Karnety roczne', parking: 'Parking', online: 'Online', gate: 'W kasie', from: 'Od', to: 'do', dynamic: 'Cena dynamiczna', validFrom: 'Ważne od', validTo: 'Ważne do', conditions: 'Warunki', buy: 'Kup na oficjalnej stronie', officialSource: 'Oficjalne źródło', verified: 'Zweryfikowano', noPricing: 'Brak obecnie zweryfikowanych publicznych cen.', back: 'Powrót do parku' },
  pt: { kicker: 'Planeia a visita', title: 'Preços', subtitle: (name: string) => `Bilhetes, passes e estacionamento para ${name}.`, admission: 'Bilhetes', passes: 'Passes anuais', parking: 'Estacionamento', online: 'Online', gate: 'Na bilheteira', from: 'Desde', to: 'até', dynamic: 'Preço dinâmico', validFrom: 'Válido desde', validTo: 'Válido até', conditions: 'Condições', buy: 'Comprar no site oficial', officialSource: 'Fonte oficial', verified: 'Verificado em', noPricing: 'Ainda não existem preços públicos verificados disponíveis.', back: 'Voltar ao parque' }
};

export function resolveParkPricingCopy(language: string | null | undefined): ParkPricingCopy {
  const normalizedLanguage: string = language?.trim().toLowerCase().split('-')[0] ?? 'en';
  return COPY[normalizedLanguage] ?? COPY['en'];
}

export function resolvePricingLocalizedText(items: readonly LocalizedItem<string>[] | null | undefined, language: string | null | undefined, fallback: string = ''): string {
  if (!items?.length) {
    return fallback;
  }

  const normalizedLanguage: string = language?.trim().toLowerCase().split('-')[0] ?? 'en';
  const exact: LocalizedItem<string> | undefined = items.find((item: LocalizedItem<string>): boolean => item.languageCode?.toLowerCase().split('-')[0] === normalizedLanguage && Boolean(item.value?.trim()));
  const english: LocalizedItem<string> | undefined = items.find((item: LocalizedItem<string>): boolean => item.languageCode?.toLowerCase().split('-')[0] === 'en' && Boolean(item.value?.trim()));
  return exact?.value?.trim() || english?.value?.trim() || items.find((item: LocalizedItem<string>): boolean => Boolean(item.value?.trim()))?.value?.trim() || fallback;
}

export function formatParkPrice(value: ParkPriceValue | null | undefined, currencyCode: string, language: string, copy: ParkPricingCopy): string | null {
  if (!value) {
    return null;
  }

  const formatter: Intl.NumberFormat = new Intl.NumberFormat(language || 'en', { style: 'currency', currency: currencyCode || 'EUR', maximumFractionDigits: 2 });
  const mode: ParkPricingMode = value.mode;
  if (mode === 'Fixed' && value.amount !== null && value.amount !== undefined) {
    return formatter.format(value.amount);
  }

  if (mode === 'Range') {
    if (value.minimumAmount !== null && value.minimumAmount !== undefined && value.maximumAmount !== null && value.maximumAmount !== undefined) {
      return `${formatter.format(value.minimumAmount)} ${copy.to} ${formatter.format(value.maximumAmount)}`;
    }
    if (value.minimumAmount !== null && value.minimumAmount !== undefined) {
      return `${copy.from} ${formatter.format(value.minimumAmount)}`;
    }
  }

  if (mode === 'Dynamic') {
    if (value.minimumAmount !== null && value.minimumAmount !== undefined && value.maximumAmount !== null && value.maximumAmount !== undefined) {
      return `${copy.dynamic} · ${formatter.format(value.minimumAmount)} ${copy.to} ${formatter.format(value.maximumAmount)}`;
    }
    if (value.minimumAmount !== null && value.minimumAmount !== undefined) {
      return `${copy.dynamic} · ${copy.from} ${formatter.format(value.minimumAmount)}`;
    }
    return copy.dynamic;
  }

  return null;
}
