import {
  getParkItemCategoryTranslationKey,
  getParkItemTypeTranslationKey,
  resolveParkItemDescription
} from '@shared/utils/display/park-item-presentation.helpers';
import { buildPublicParkItemRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemCardViewModel } from '../models/park-item-card.model';

export function mapParkItemToCardViewModel(
  item: ParkItem,
  park: Park | null,
  currentLanguage: string,
  manufacturerName: string | null,
  zoneName: string | null
): ParkItemCardViewModel {
  const modelName: string | null = item.attractionDetails?.model?.trim() ?? null;
  const subtitleParts: string[] = [manufacturerName, modelName]
    .filter((value: string | null): value is string => !!value);

  return {
    id: item.id ?? null,
    name: item.name?.trim() ?? '',
    subtitle: subtitleParts.length > 0 ? subtitleParts.join(' · ') : null,
    description: resolveParkItemDescription(item, currentLanguage),
    categoryLabelKey: getParkItemCategoryTranslationKey(item.category),
    typeLabelKey: getParkItemTypeTranslationKey(item.type),
    typeIconClass: resolveParkItemTypeIconClass(item.type),
    zoneName,
    highlights: buildParkItemHighlights(item, manufacturerName, currentLanguage),
    itemLink: buildParkItemLink(park, item, currentLanguage)
  };
}

function buildParkItemHighlights(item: ParkItem, manufacturerName: string | null, currentLanguage: string): string[] {
  const values: string[] = [];

  if (manufacturerName) {
    values.push(manufacturerName);
  }

  if (item.attractionDetails?.model) {
    values.push(item.attractionDetails.model);
  }

  const statusLabel: string | null = resolveAttractionStatusDisplay(item.attractionDetails?.status, currentLanguage);
  if (statusLabel) {
    values.push(statusLabel);
  }

  if (item.attractionDetails?.heightInMeters != null) {
    values.push(`${item.attractionDetails.heightInMeters} m`);
  }

  if (item.attractionDetails?.speedInKmH != null) {
    values.push(`${item.attractionDetails.speedInKmH} km/h`);
  }

  if (item.attractionDetails?.inversionCount != null) {
    values.push(`${item.attractionDetails.inversionCount} inv.`);
  }

  return values.slice(0, 4);
}


function resolveAttractionStatusDisplay(status: string | null | undefined, currentLanguage: string): string | null {
  const normalized: string = status?.trim() ?? '';
  if (normalized.length === 0) {
    return null;
  }

  const normalizedKey: string = normalized.toLowerCase().replace(/[\s_-]+/g, '');
  const statusKey: string | undefined = {
    operating: 'operating',
    open: 'operating',
    opened: 'operating',
    enfonctionnement: 'operating',
    underconstruction: 'underConstruction',
    construction: 'underConstruction',
    temporarilyclosed: 'temporarilyClosed',
    temporaryclosed: 'temporarilyClosed',
    closedtemporarily: 'temporarilyClosed',
    closeddefinitively: 'closedDefinitively',
    permanentlyclosed: 'closedDefinitively',
    definitivelyclosed: 'closedDefinitively',
    fermedefinitivement: 'closedDefinitively',
    removed: 'removed',
    dismantled: 'removed',
    planned: 'planned',
    announced: 'planned',
    unknown: 'unknown'
  }[normalizedKey];

  if (!statusKey) {
    return normalized;
  }

  const labels: Record<string, Record<string, string>> = {
    fr: {
      operating: 'En fonctionnement',
      underConstruction: 'En construction',
      temporarilyClosed: 'Fermé temporairement',
      closedDefinitively: 'Fermé définitivement',
      removed: 'Supprimé / démonté',
      planned: 'Prévu',
      unknown: 'Inconnu'
    },
    en: {
      operating: 'Operating',
      underConstruction: 'Under construction',
      temporarilyClosed: 'Temporarily closed',
      closedDefinitively: 'Permanently closed',
      removed: 'Removed / dismantled',
      planned: 'Planned',
      unknown: 'Unknown'
    },
    es: {
      operating: 'En funcionamiento',
      underConstruction: 'En construcción',
      temporarilyClosed: 'Cerrado temporalmente',
      closedDefinitively: 'Cerrado definitivamente',
      removed: 'Retirado / desmontado',
      planned: 'Previsto',
      unknown: 'Desconocido'
    },
    de: {
      operating: 'In Betrieb',
      underConstruction: 'Im Bau',
      temporarilyClosed: 'Vorübergehend geschlossen',
      closedDefinitively: 'Dauerhaft geschlossen',
      removed: 'Entfernt / abgebaut',
      planned: 'Geplant',
      unknown: 'Unbekannt'
    },
    it: {
      operating: 'In funzione',
      underConstruction: 'In costruzione',
      temporarilyClosed: 'Chiuso temporaneamente',
      closedDefinitively: 'Chiuso definitivamente',
      removed: 'Rimosso / smontato',
      planned: 'Previsto',
      unknown: 'Sconosciuto'
    },
    pl: {
      operating: 'Działa',
      underConstruction: 'W budowie',
      temporarilyClosed: 'Tymczasowo zamknięte',
      closedDefinitively: 'Zamknięte na stałe',
      removed: 'Usunięte / zdemontowane',
      planned: 'Planowane',
      unknown: 'Nieznane'
    },
    nl: {
      operating: 'In werking',
      underConstruction: 'In aanbouw',
      temporarilyClosed: 'Tijdelijk gesloten',
      closedDefinitively: 'Definitief gesloten',
      removed: 'Verwijderd / afgebroken',
      planned: 'Gepland',
      unknown: 'Onbekend'
    },
    pt: {
      operating: 'Em funcionamento',
      underConstruction: 'Em construção',
      temporarilyClosed: 'Fechado temporariamente',
      closedDefinitively: 'Fechado definitivamente',
      removed: 'Removido / desmontado',
      planned: 'Planeado',
      unknown: 'Desconhecido'
    }
  };

  return labels[currentLanguage]?.[statusKey] ?? labels.en[statusKey] ?? normalized;
}

function buildParkItemLink(park: Park | null, item: ParkItem, currentLanguage: string): string[] | null {
  return buildPublicParkItemRouteCommands({
    language: currentLanguage,
    parkId: park?.id,
    parkName: park?.name,
    itemId: item.id,
    itemName: item.name
  });
}

function resolveParkItemTypeIconClass(type: string | null | undefined): string {
  switch (type) {
    case 'RollerCoaster':
      return 'pi pi-bolt';
    case 'WaterRide':
      return 'pi pi-compass';
    case 'FlatRide':
      return 'pi pi-sync';
    case 'DarkRide':
      return 'pi pi-moon';
    case 'FamilyRide':
    case 'MeetAndGreet':
      return 'pi pi-heart';
    case 'ThrillRide':
      return 'pi pi-send';
    case 'Restaurant':
    case 'Snack':
      return 'pi pi-shopping-bag';
    case 'Show':
      return 'pi pi-video';
    case 'Hotel':
      return 'pi pi-home';
    case 'Shop':
      return 'pi pi-shopping-cart';
    case 'Game':
    case 'InteractiveExperience':
      return 'pi pi-bullseye';
    case 'Transport':
    case 'TransportRide':
      return 'pi pi-car';
    case 'Station':
      return 'pi pi-directions';
    case 'Toilets':
      return 'pi pi-users';
    case 'FirstAid':
      return 'pi pi-plus-circle';
    case 'Information':
      return 'pi pi-info-circle';
    case 'Locker':
      return 'pi pi-lock';
    case 'Parking':
      return 'pi pi-car';
    case 'Service':
      return 'pi pi-wrench';
    default:
      return 'pi pi-star';
  }
}
