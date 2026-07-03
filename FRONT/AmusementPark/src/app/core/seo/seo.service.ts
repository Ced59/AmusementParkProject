import { DOCUMENT } from '@angular/common';
import { Inject, Injectable } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { TechnicalContentBlock, TechnicalPage } from '@app/models/technical-pages/technical-page';
import { VideoDto } from '@app/models/videos/video-dto';
import { ParkDetailViewModel } from '@features/public/parks/models/park-detail-view.model';
import { ParkReferenceDetailViewModel, ParkReferenceKind } from '@features/public/parks/models/park-reference-detail-view.model';
import { ParkItemDetailViewModel } from '@features/public/park-items/models/park-item-detail-view.model';
import { HistoryArticlePageViewModel, HistoryTimelinePageViewModel } from '@features/public/history/models/history-view.model';
import { environment } from '../../../environments/environment';
import { resolveLocalizedText, stripHtml } from '@shared/utils/localization/localized-text.helpers';
import { CanonicalUrlService } from './canonical-url.service';
import { HreflangService } from './hreflang.service';
import { JsonLdService } from './json-ld.service';
import { buildCanonicalVideoRouteRedirectPath } from './legacy-video-route.helpers';
import { SeoAlternateLink, SeoRouteData } from './models/seo-route-data.model';
import { SEO_DEFAULT_LANGUAGE } from './seo-languages';
import { normalizeSeoText, truncateSeoText } from './seo-text.utils';

interface StaticSeoCopy {
  title: string;
  description: string;
}

interface SocialImageMetadata {
  url: string;
  width: number | null;
  height: number | null;
}

interface ParkImagesSeoCopy {
  titlePrefix: string;
  parkFallback: string;
  description: (parkName: string, locationLabel: string, totalImages: number) => string;
}

interface ParkItemImagesSeoCopy {
  titlePrefix: string;
  itemFallback: string;
  description: (itemName: string, parkName: string, locationLabel: string, totalImages: number) => string;
}

interface ParkVideosSeoCopy {
  titlePrefix: string;
  parkFallback: string;
  description: (parkName: string, locationLabel: string, totalVideos: number) => string;
}

interface ParkItemVideosSeoCopy {
  titlePrefix: string;
  itemFallback: string;
  description: (itemName: string, parkName: string, locationLabel: string, totalVideos: number) => string;
}

interface ParkMapSeoCopy {
  titlePrefix: string;
  parkFallback: string;
  description: (parkName: string, locationLabel: string) => string;
}

interface ParkItemsSeoCopy {
  parkFallback: string;
  breadcrumbLabel: string;
  title: (parkName: string) => string;
  description: (parkName: string) => string;
}

interface ParkWeatherSeoCopy {
  parkFallback: string;
  breadcrumbLabel: string;
  title: (parkName: string) => string;
  description: (parkName: string, totalDays: number) => string;
}

interface ParkOpeningHoursSeoCopy {
  parkFallback: string;
  breadcrumbLabel: string;
  title: (parkName: string) => string;
  description: (parkName: string, totalDays: number) => string;
}

interface HistorySeoCopy {
  breadcrumbLabel: (ownerName: string) => string;
  timelineDescription: (ownerName: string) => string;
  articleDescription: (ownerName: string, dateLabel: string) => string;
}

interface ParkZonesSeoCopy {
  parkFallback: string;
  breadcrumbLabel: string;
  title: (parkName: string) => string;
  description: (parkName: string, zoneCount: number, totalItems: number) => string;
}

interface ParkZoneSeoCopy {
  parkFallback: string;
  zoneFallback: string;
  title: (zoneName: string, parkName: string) => string;
  description: (zoneName: string, parkName: string, totalItems: number) => string;
}

interface ParkReferenceSeoCopy {
  title: (referenceName: string, kindLabel: string) => string;
  description: (referenceName: string, kindLabel: string, legalName: string | null, attractionCount: number) => string;
  breadcrumbLabel: string;
  kindLabels: Record<ParkReferenceKind, string>;
}

interface ParkDetailSeoCopy {
  description: (parkName: string, locationLabel: string) => string;
}

interface ParkItemDetailSeoCopy {
  parkContextPrefix: string;
  description: (itemName: string, parkLabel: string, specSummary: string) => string;
}

interface VideoDetailSeoCopy {
  parkFallback: string;
  itemFallback: string;
  videoFallback: string;
  parkDescription: (videoTitle: string, parkName: string) => string;
  itemDescription: (videoTitle: string, itemName: string, parkName: string) => string;
}

interface RegionDisplayNames {
  of(code: string): string | undefined;
}

interface IntlWithDisplayNames {
  DisplayNames?: new (locales: string[], options: { type: 'region' }) => RegionDisplayNames;
}

const SITE_NAME: string = 'Amusement Parks';
const DEFAULT_DESCRIPTION: string = 'Explore amusement parks, attractions, restaurants, hotels and park references around the world.';
const DEFAULT_SOCIAL_IMAGE_PATH: string = '/assets/general-icon/logo-amusementpark.png';
const SOCIAL_IMAGE_WIDTH: number = 1200;
const DEFAULT_SOCIAL_IMAGE_WIDTH: number = 1024;
const DEFAULT_SOCIAL_IMAGE_HEIGHT: number = 1024;
const RESPONSIVE_IMAGE_VERSION: string = '2';

const HISTORY_SEO_COPY: Record<string, HistorySeoCopy> = {
  fr: {
    breadcrumbLabel: (ownerName: string): string => `Histoire de ${ownerName}`,
    timelineDescription: (ownerName: string): string => `Explore les dates clés, changements, incidents et grands jalons de ${ownerName}.`,
    articleDescription: (ownerName: string, dateLabel: string): string => `Article historique sur ${ownerName}, autour de ${dateLabel}.`
  },
  en: {
    breadcrumbLabel: (ownerName: string): string => `${ownerName} history`,
    timelineDescription: (ownerName: string): string => `Explore key dates, changes, incidents and major milestones for ${ownerName}.`,
    articleDescription: (ownerName: string, dateLabel: string): string => `History article about ${ownerName}, around ${dateLabel}.`
  },
  de: {
    breadcrumbLabel: (ownerName: string): string => `Geschichte von ${ownerName}`,
    timelineDescription: (ownerName: string): string => `Entdecke die Schlüsseldaten, Änderungen, Vorfälle und großen Meilensteine von ${ownerName}.`,
    articleDescription: (ownerName: string, dateLabel: string): string => `Historischer Artikel über ${ownerName}, rund um ${dateLabel}.`
  },
  nl: {
    breadcrumbLabel: (ownerName: string): string => `Geschiedenis van ${ownerName}`,
    timelineDescription: (ownerName: string): string => `Ontdek de sleuteldatums, veranderingen, incidenten en grote mijlpalen van ${ownerName}.`,
    articleDescription: (ownerName: string, dateLabel: string): string => `Historisch artikel over ${ownerName}, rond ${dateLabel}.`
  },
  it: {
    breadcrumbLabel: (ownerName: string): string => `Storia di ${ownerName}`,
    timelineDescription: (ownerName: string): string => `Esplora le date chiave, i cambiamenti, gli incidenti e le grandi tappe di ${ownerName}.`,
    articleDescription: (ownerName: string, dateLabel: string): string => `Articolo storico su ${ownerName}, intorno al ${dateLabel}.`
  },
  es: {
    breadcrumbLabel: (ownerName: string): string => `Historia de ${ownerName}`,
    timelineDescription: (ownerName: string): string => `Explora las fechas clave, cambios, incidentes y grandes hitos de ${ownerName}.`,
    articleDescription: (ownerName: string, dateLabel: string): string => `Artículo histórico sobre ${ownerName}, alrededor de ${dateLabel}.`
  },
  pl: {
    breadcrumbLabel: (ownerName: string): string => `Historia ${ownerName}`,
    timelineDescription: (ownerName: string): string => `Poznaj kluczowe daty, zmiany, incydenty i najważniejsze punkty historii ${ownerName}.`,
    articleDescription: (ownerName: string, dateLabel: string): string => `Artykuł historyczny o ${ownerName}, wokół daty ${dateLabel}.`
  },
  pt: {
    breadcrumbLabel: (ownerName: string): string => `História de ${ownerName}`,
    timelineDescription: (ownerName: string): string => `Explora as datas-chave, mudanças, incidentes e grandes marcos de ${ownerName}.`,
    articleDescription: (ownerName: string, dateLabel: string): string => `Artigo histórico sobre ${ownerName}, por volta de ${dateLabel}.`
  }
};

const PARK_IMAGES_SEO_COPY: Record<string, ParkImagesSeoCopy> = {
  en: {
    titlePrefix: 'Images of',
    parkFallback: 'this park',
    description: (parkName: string, locationLabel: string, totalImages: number): string => {
      const countLabel: string = totalImages > 0 ? `${totalImages} published photos` : 'published photos';
      return `Browse ${countLabel} of ${parkName}${locationLabel ? ` in ${locationLabel}` : ''}.`;
    }
  },
  fr: {
    titlePrefix: 'Images de',
    parkFallback: 'ce parc',
    description: (parkName: string, locationLabel: string, totalImages: number): string => {
      const countLabel: string = totalImages > 0 ? `${totalImages} photos publiées` : 'les photos publiées';
      return `Découvre ${countLabel} de ${parkName}${locationLabel ? ` à ${locationLabel}` : ''}.`;
    }
  },
  es: {
    titlePrefix: 'Imágenes de',
    parkFallback: 'este parque',
    description: (parkName: string, locationLabel: string, totalImages: number): string => {
      const countLabel: string = totalImages > 0 ? `${totalImages} fotos publicadas` : 'las fotos publicadas';
      return `Consulta ${countLabel} de ${parkName}${locationLabel ? ` en ${locationLabel}` : ''}.`;
    }
  },
  de: {
    titlePrefix: 'Bilder von',
    parkFallback: 'diesem Park',
    description: (parkName: string, locationLabel: string, totalImages: number): string => {
      const countLabel: string = totalImages > 0 ? `${totalImages} veröffentlichte Fotos` : 'die veröffentlichten Fotos';
      return `Entdecke ${countLabel} von ${parkName}${locationLabel ? ` in ${locationLabel}` : ''}.`;
    }
  },
  it: {
    titlePrefix: 'Immagini di',
    parkFallback: 'questo parco',
    description: (parkName: string, locationLabel: string, totalImages: number): string => {
      const countLabel: string = totalImages > 0 ? `${totalImages} foto pubblicate` : 'le foto pubblicate';
      return `Scopri ${countLabel} di ${parkName}${locationLabel ? ` a ${locationLabel}` : ''}.`;
    }
  },
  nl: {
    titlePrefix: 'Afbeeldingen van',
    parkFallback: 'dit park',
    description: (parkName: string, locationLabel: string, totalImages: number): string => {
      const countLabel: string = totalImages > 0 ? `${totalImages} gepubliceerde foto's` : `de gepubliceerde foto's`;
      return `Bekijk ${countLabel} van ${parkName}${locationLabel ? ` in ${locationLabel}` : ''}.`;
    }
  },
  pl: {
    titlePrefix: 'Zdjęcia',
    parkFallback: 'tego parku',
    description: (parkName: string, locationLabel: string, totalImages: number): string => {
      const countLabel: string = totalImages > 0 ? `${totalImages} opublikowanych zdjęć` : 'opublikowane zdjęcia';
      return `Zobacz ${countLabel} parku ${parkName}${locationLabel ? ` w ${locationLabel}` : ''}.`;
    }
  },
  pt: {
    titlePrefix: 'Imagens de',
    parkFallback: 'este parque',
    description: (parkName: string, locationLabel: string, totalImages: number): string => {
      const countLabel: string = totalImages > 0 ? `${totalImages} fotos publicadas` : 'as fotos publicadas';
      return `Explore ${countLabel} de ${parkName}${locationLabel ? ` em ${locationLabel}` : ''}.`;
    }
  }
};

const PARK_ITEM_IMAGES_SEO_COPY: Record<string, ParkItemImagesSeoCopy> = {
  en: {
    titlePrefix: 'Images of',
    itemFallback: 'this item',
    description: (itemName: string, parkName: string, locationLabel: string, totalImages: number): string => {
      const countLabel: string = totalImages > 0 ? `${totalImages} published photos` : 'published photos';
      return `Browse ${countLabel} of ${itemName}${parkName ? ` at ${parkName}` : ''}${locationLabel ? ` in ${locationLabel}` : ''}.`;
    }
  },
  fr: {
    titlePrefix: 'Images de',
    itemFallback: 'cet élément',
    description: (itemName: string, parkName: string, locationLabel: string, totalImages: number): string => {
      const countLabel: string = totalImages > 0 ? `${totalImages} photos publiées` : 'les photos publiées';
      return `Découvre ${countLabel} de ${itemName}${parkName ? ` à ${parkName}` : ''}${locationLabel ? ` (${locationLabel})` : ''}.`;
    }
  },
  es: {
    titlePrefix: 'Imágenes de',
    itemFallback: 'este elemento',
    description: (itemName: string, parkName: string, locationLabel: string, totalImages: number): string => {
      const countLabel: string = totalImages > 0 ? `${totalImages} fotos publicadas` : 'las fotos publicadas';
      return `Consulta ${countLabel} de ${itemName}${parkName ? ` en ${parkName}` : ''}${locationLabel ? ` (${locationLabel})` : ''}.`;
    }
  },
  de: {
    titlePrefix: 'Bilder von',
    itemFallback: 'diesem Element',
    description: (itemName: string, parkName: string, locationLabel: string, totalImages: number): string => {
      const countLabel: string = totalImages > 0 ? `${totalImages} veröffentlichte Fotos` : 'die veröffentlichten Fotos';
      return `Entdecke ${countLabel} von ${itemName}${parkName ? ` in ${parkName}` : ''}${locationLabel ? ` (${locationLabel})` : ''}.`;
    }
  },
  it: {
    titlePrefix: 'Immagini di',
    itemFallback: 'questo elemento',
    description: (itemName: string, parkName: string, locationLabel: string, totalImages: number): string => {
      const countLabel: string = totalImages > 0 ? `${totalImages} foto pubblicate` : 'le foto pubblicate';
      return `Scopri ${countLabel} di ${itemName}${parkName ? ` a ${parkName}` : ''}${locationLabel ? ` (${locationLabel})` : ''}.`;
    }
  },
  nl: {
    titlePrefix: 'Afbeeldingen van',
    itemFallback: 'dit onderdeel',
    description: (itemName: string, parkName: string, locationLabel: string, totalImages: number): string => {
      const countLabel: string = totalImages > 0 ? `${totalImages} gepubliceerde foto's` : `de gepubliceerde foto's`;
      return `Bekijk ${countLabel} van ${itemName}${parkName ? ` in ${parkName}` : ''}${locationLabel ? ` (${locationLabel})` : ''}.`;
    }
  },
  pl: {
    titlePrefix: 'Zdjęcia',
    itemFallback: 'tego elementu',
    description: (itemName: string, parkName: string, locationLabel: string, totalImages: number): string => {
      const countLabel: string = totalImages > 0 ? `${totalImages} opublikowanych zdjęć` : 'opublikowane zdjęcia';
      return `Zobacz ${countLabel} elementu ${itemName}${parkName ? ` w ${parkName}` : ''}${locationLabel ? ` (${locationLabel})` : ''}.`;
    }
  },
  pt: {
    titlePrefix: 'Imagens de',
    itemFallback: 'este elemento',
    description: (itemName: string, parkName: string, locationLabel: string, totalImages: number): string => {
      const countLabel: string = totalImages > 0 ? `${totalImages} fotos publicadas` : 'as fotos publicadas';
      return `Explore ${countLabel} de ${itemName}${parkName ? ` em ${parkName}` : ''}${locationLabel ? ` (${locationLabel})` : ''}.`;
    }
  }
};

const PARK_VIDEOS_SEO_COPY: Record<string, ParkVideosSeoCopy> = {
  en: {
    titlePrefix: 'Videos of',
    parkFallback: 'this park',
    description: (parkName: string, locationLabel: string, totalVideos: number): string => {
      const countLabel: string = totalVideos > 0 ? `${totalVideos} published videos` : 'published videos';
      return `Watch ${countLabel} of ${parkName}${locationLabel ? ` in ${locationLabel}` : ''}: onrides, offrides, official videos and creator content.`;
    }
  },
  fr: {
    titlePrefix: 'Vidéos de',
    parkFallback: 'ce parc',
    description: (parkName: string, locationLabel: string, totalVideos: number): string => {
      const countLabel: string = totalVideos > 0 ? `${totalVideos} vidéos publiées` : 'les vidéos publiées';
      return `Regarde ${countLabel} de ${parkName}${locationLabel ? ` à ${locationLabel}` : ''} : onrides, offrides, vidéos officielles et contenus créateurs.`;
    }
  }
};

const PARK_ITEM_VIDEOS_SEO_COPY: Record<string, ParkItemVideosSeoCopy> = {
  en: {
    titlePrefix: 'Videos of',
    itemFallback: 'this item',
    description: (itemName: string, parkName: string, locationLabel: string, totalVideos: number): string => {
      const countLabel: string = totalVideos > 0 ? `${totalVideos} published videos` : 'published videos';
      return `Watch ${countLabel} of ${itemName}${parkName ? ` at ${parkName}` : ''}${locationLabel ? ` in ${locationLabel}` : ''}.`;
    }
  },
  fr: {
    titlePrefix: 'Vidéos de',
    itemFallback: 'cet élément',
    description: (itemName: string, parkName: string, locationLabel: string, totalVideos: number): string => {
      const countLabel: string = totalVideos > 0 ? `${totalVideos} vidéos publiées` : 'les vidéos publiées';
      return `Regarde ${countLabel} de ${itemName}${parkName ? ` à ${parkName}` : ''}${locationLabel ? ` (${locationLabel})` : ''}.`;
    }
  }
};


const PARK_MAP_SEO_COPY: Record<string, ParkMapSeoCopy> = {
  en: {
    titlePrefix: 'Interactive map of',
    parkFallback: 'this park',
    description: (parkName: string, locationLabel: string): string => `Interactive map of ${parkName}${locationLabel ? ` in ${locationLabel}` : ''}.`
  },
  fr: {
    titlePrefix: 'Carte interactive de',
    parkFallback: 'ce parc',
    description: (parkName: string, locationLabel: string): string => `Carte interactive de ${parkName}${locationLabel ? ` à ${locationLabel}` : ''}.`
  },
  es: {
    titlePrefix: 'Mapa interactivo de',
    parkFallback: 'este parque',
    description: (parkName: string, locationLabel: string): string => `Mapa interactivo de ${parkName}${locationLabel ? ` en ${locationLabel}` : ''}.`
  },
  de: {
    titlePrefix: 'Interaktive Karte von',
    parkFallback: 'diesem Park',
    description: (parkName: string, locationLabel: string): string => `Interaktive Karte von ${parkName}${locationLabel ? ` in ${locationLabel}` : ''}.`
  },
  it: {
    titlePrefix: 'Mappa interattiva di',
    parkFallback: 'questo parco',
    description: (parkName: string, locationLabel: string): string => `Mappa interattiva di ${parkName}${locationLabel ? ` a ${locationLabel}` : ''}.`
  },
  nl: {
    titlePrefix: 'Interactieve kaart van',
    parkFallback: 'dit park',
    description: (parkName: string, locationLabel: string): string => `Interactieve kaart van ${parkName}${locationLabel ? ` in ${locationLabel}` : ''}.`
  },
  pl: {
    titlePrefix: 'Interaktywna mapa',
    parkFallback: 'tego parku',
    description: (parkName: string, locationLabel: string): string => `Interaktywna mapa parku ${parkName}${locationLabel ? ` w ${locationLabel}` : ''}.`
  },
  pt: {
    titlePrefix: 'Mapa interativo de',
    parkFallback: 'este parque',
    description: (parkName: string, locationLabel: string): string => `Mapa interativo de ${parkName}${locationLabel ? ` em ${locationLabel}` : ''}.`
  }
};

const PARK_ITEMS_SEO_COPY: Record<string, ParkItemsSeoCopy> = {
  en: {
    parkFallback: 'this park',
    breadcrumbLabel: 'To discover',
    title: (parkName: string): string => `${parkName} attractions, shows, restaurants and shops`,
    description: (parkName: string): string => `Browse attractions, shows, restaurants, shops and practical places at ${parkName}.`
  },
  fr: {
    parkFallback: 'ce parc',
    breadcrumbLabel: 'À découvrir',
    title: (parkName: string): string => `Attractions, spectacles, restaurants et boutiques à ${parkName}`,
    description: (parkName: string): string => `Découvre les attractions, spectacles, restaurants, boutiques et lieux pratiques de ${parkName}.`
  },
  es: {
    parkFallback: 'este parque',
    breadcrumbLabel: 'Por descubrir',
    title: (parkName: string): string => `Atracciones, espectáculos, restaurantes y tiendas de ${parkName}`,
    description: (parkName: string): string => `Explora atracciones, espectáculos, restaurantes, tiendas y lugares prácticos de ${parkName}.`
  },
  de: {
    parkFallback: 'diesem Park',
    breadcrumbLabel: 'Entdecken',
    title: (parkName: string): string => `Attraktionen, Shows, Restaurants und Shops in ${parkName}`,
    description: (parkName: string): string => `Entdecke Attraktionen, Shows, Restaurants, Shops und praktische Orte in ${parkName}.`
  },
  it: {
    parkFallback: 'questo parco',
    breadcrumbLabel: 'Da scoprire',
    title: (parkName: string): string => `Attrazioni, spettacoli, ristoranti e negozi di ${parkName}`,
    description: (parkName: string): string => `Esplora attrazioni, spettacoli, ristoranti, negozi e luoghi utili di ${parkName}.`
  },
  nl: {
    parkFallback: 'dit park',
    breadcrumbLabel: 'Te ontdekken',
    title: (parkName: string): string => `Attracties, shows, restaurants en winkels in ${parkName}`,
    description: (parkName: string): string => `Ontdek attracties, shows, restaurants, winkels en praktische plekken in ${parkName}.`
  },
  pl: {
    parkFallback: 'tym parku',
    breadcrumbLabel: 'Do odkrycia',
    title: (parkName: string): string => `Atrakcje, pokazy, restauracje i sklepy w ${parkName}`,
    description: (parkName: string): string => `Przeglądaj atrakcje, pokazy, restauracje, sklepy i praktyczne miejsca w ${parkName}.`
  },
  pt: {
    parkFallback: 'este parque',
    breadcrumbLabel: 'A descobrir',
    title: (parkName: string): string => `Atrações, espetáculos, restaurantes e lojas de ${parkName}`,
    description: (parkName: string): string => `Explora atrações, espetáculos, restaurantes, lojas e locais práticos de ${parkName}.`
  }
};

const PARK_WEATHER_SEO_COPY: Record<string, ParkWeatherSeoCopy> = {
  en: {
    parkFallback: 'this park',
    breadcrumbLabel: '7-day weather',
    title: (parkName: string): string => `7-day weather for ${parkName}`,
    description: (parkName: string, totalDays: number): string => `Check the ${totalDays || 7}-day weather forecast for ${parkName}: daily conditions, temperatures, rain risk and wind.`
  },
  fr: {
    parkFallback: 'ce parc',
    breadcrumbLabel: 'M\u00e9t\u00e9o 7 jours',
    title: (parkName: string): string => `M\u00e9t\u00e9o \u00e0 7 jours de ${parkName}`,
    description: (parkName: string, totalDays: number): string => `V\u00e9rifie la m\u00e9t\u00e9o de ${parkName} pour ta visite : pr\u00e9visions \u00e0 ${totalDays || 7} jours, temp\u00e9ratures, pluie et vent.`
  },
  es: {
    parkFallback: 'este parque',
    breadcrumbLabel: 'Tiempo 7 días',
    title: (parkName: string): string => `Tiempo a 7 días de ${parkName}`,
    description: (parkName: string, totalDays: number): string => `Consulta el pronóstico del tiempo a ${totalDays || 7} días para ${parkName}: condiciones, temperaturas, lluvia y viento.`
  },
  de: {
    parkFallback: 'diesem Park',
    breadcrumbLabel: '7-Tage-Wetter',
    title: (parkName: string): string => `7-Tage-Wetter für ${parkName}`,
    description: (parkName: string, totalDays: number): string => `Prüfe die ${totalDays || 7}-Tage-Wettervorhersage für ${parkName}: Wetterlage, Temperaturen, Regenrisiko und Wind.`
  },
  it: {
    parkFallback: 'questo parco',
    breadcrumbLabel: 'Meteo 7 giorni',
    title: (parkName: string): string => `Meteo a 7 giorni di ${parkName}`,
    description: (parkName: string, totalDays: number): string => `Consulta le previsioni meteo a ${totalDays || 7} giorni per ${parkName}: condizioni, temperature, pioggia e vento.`
  },
  nl: {
    parkFallback: 'dit park',
    breadcrumbLabel: '7-daags weer',
    title: (parkName: string): string => `7-daags weer voor ${parkName}`,
    description: (parkName: string, totalDays: number): string => `Bekijk de ${totalDays || 7}-daagse weersverwachting voor ${parkName}: weerbeeld, temperaturen, regen en wind.`
  },
  pl: {
    parkFallback: 'tym parku',
    breadcrumbLabel: 'Pogoda 7 dni',
    title: (parkName: string): string => `Pogoda na 7 dni dla ${parkName}`,
    description: (parkName: string, totalDays: number): string => `Sprawdź prognozę pogody na ${totalDays || 7} dni dla ${parkName}: warunki, temperatury, ryzyko deszczu i wiatr.`
  },
  pt: {
    parkFallback: 'este parque',
    breadcrumbLabel: 'Meteorologia 7 dias',
    title: (parkName: string): string => `Meteorologia a 7 dias de ${parkName}`,
    description: (parkName: string, totalDays: number): string => `Verifica a meteorologia de ${parkName} antes da tua visita: previsão a ${totalDays || 7} dias, temperaturas, chuva e vento.`
  }
};

const PARK_OPENING_HOURS_SEO_COPY: Record<string, ParkOpeningHoursSeoCopy> = {
  en: {
    parkFallback: 'this park',
    breadcrumbLabel: 'Opening hours',
    title: (parkName: string): string => `Dates and opening hours for ${parkName}`,
    description: (parkName: string, totalDays: number): string => `Browse the listed opening dates and hours for ${parkName}${totalDays > 0 ? ` across ${totalDays} days` : ''}, with daily schedules and closure notes.`
  },
  fr: {
    parkFallback: 'ce parc',
    breadcrumbLabel: 'Dates et horaires',
    title: (parkName: string): string => `Dates et horaires de ${parkName}`,
    description: (parkName: string, totalDays: number): string => `Parcours les dates et horaires renseignés pour ${parkName}${totalDays > 0 ? ` sur ${totalDays} jours` : ''}, avec les ouvertures, fermetures et infos utiles.`
  },
  es: {
    parkFallback: 'este parque',
    breadcrumbLabel: 'Fechas y horarios',
    title: (parkName: string): string => `Fechas y horarios de ${parkName}`,
    description: (parkName: string, totalDays: number): string => `Consulta las fechas y horarios publicados para ${parkName}${totalDays > 0 ? ` durante ${totalDays} días` : ''}, con aperturas, cierres y notas prácticas.`
  },
  de: {
    parkFallback: 'diesem Park',
    breadcrumbLabel: 'Öffnungszeiten',
    title: (parkName: string): string => `Termine und Öffnungszeiten von ${parkName}`,
    description: (parkName: string, totalDays: number): string => `Sieh dir die erfassten Termine und Öffnungszeiten von ${parkName}${totalDays > 0 ? ` für ${totalDays} Tage` : ''} mit Tagesplänen und Schließhinweisen an.`
  },
  it: {
    parkFallback: 'questo parco',
    breadcrumbLabel: 'Date e orari',
    title: (parkName: string): string => `Date e orari di apertura di ${parkName}`,
    description: (parkName: string, totalDays: number): string => `Consulta date e orari indicati per ${parkName}${totalDays > 0 ? ` su ${totalDays} giorni` : ''}, con aperture, chiusure e note operative.`
  },
  nl: {
    parkFallback: 'dit park',
    breadcrumbLabel: 'Datums en uren',
    title: (parkName: string): string => `Datums en openingstijden van ${parkName}`,
    description: (parkName: string, totalDays: number): string => `Bekijk de ingevulde datums en openingstijden van ${parkName}${totalDays > 0 ? ` voor ${totalDays} dagen` : ''}, met daguren en sluitingsnotities.`
  },
  pl: {
    parkFallback: 'tym parku',
    breadcrumbLabel: 'Daty i godziny',
    title: (parkName: string): string => `Daty i godziny otwarcia ${parkName}`,
    description: (parkName: string, totalDays: number): string => `Przeglądaj zapisane daty i godziny otwarcia ${parkName}${totalDays > 0 ? ` dla ${totalDays} dni` : ''}, wraz z dniami zamknięcia i notatkami.`
  },
  pt: {
    parkFallback: 'este parque',
    breadcrumbLabel: 'Datas e horários',
    title: (parkName: string): string => `Datas e horários de abertura de ${parkName}`,
    description: (parkName: string, totalDays: number): string => `Consulta as datas e horários registados para ${parkName}${totalDays > 0 ? ` em ${totalDays} dias` : ''}, com aberturas, fechos e notas úteis.`
  }
};

const PARK_ZONES_SEO_COPY: Record<string, ParkZonesSeoCopy> = {
  en: {
    parkFallback: 'this park',
    breadcrumbLabel: 'Zones',
    title: (parkName: string): string => `${parkName} themed zones`,
    description: (parkName: string, zoneCount: number, totalItems: number): string => {
      const zoneLabel: string = zoneCount > 0 ? `${zoneCount} themed zones` : 'the themed zones';
      const itemLabel: string = totalItems > 0 ? ` with ${totalItems} listed places` : '';
      return `Browse ${zoneLabel} of ${parkName}${itemLabel}: attractions, restaurants, shops and useful places.`;
    }
  },
  fr: {
    parkFallback: 'ce parc',
    breadcrumbLabel: 'Zones',
    title: (parkName: string): string => `Zones de ${parkName}`,
    description: (parkName: string, zoneCount: number, totalItems: number): string => {
      const zoneLabel: string = zoneCount > 0 ? `${zoneCount} zones` : 'les zones';
      const itemLabel: string = totalItems > 0 ? ` avec ${totalItems} lieux répertoriés` : '';
      return `Découvre ${zoneLabel} de ${parkName}${itemLabel} : attractions, restaurants, boutiques et points utiles.`;
    }
  },
  es: {
    parkFallback: 'este parque',
    breadcrumbLabel: 'Zonas',
    title: (parkName: string): string => `Zonas de ${parkName}`,
    description: (parkName: string, zoneCount: number, totalItems: number): string => {
      const zoneLabel: string = zoneCount > 0 ? `${zoneCount} zonas` : 'las zonas';
      const itemLabel: string = totalItems > 0 ? ` con ${totalItems} lugares listados` : '';
      return `Explora ${zoneLabel} de ${parkName}${itemLabel}: atracciones, restaurantes, tiendas y lugares útiles.`;
    }
  },
  de: {
    parkFallback: 'diesem Park',
    breadcrumbLabel: 'Bereiche',
    title: (parkName: string): string => `Themenbereiche von ${parkName}`,
    description: (parkName: string, zoneCount: number, totalItems: number): string => {
      const zoneLabel: string = zoneCount > 0 ? `${zoneCount} Themenbereiche` : 'die Themenbereiche';
      const itemLabel: string = totalItems > 0 ? ` mit ${totalItems} gelisteten Orten` : '';
      return `Entdecke ${zoneLabel} von ${parkName}${itemLabel}: Attraktionen, Restaurants, Shops und praktische Orte.`;
    }
  },
  it: {
    parkFallback: 'questo parco',
    breadcrumbLabel: 'Zone',
    title: (parkName: string): string => `Zone di ${parkName}`,
    description: (parkName: string, zoneCount: number, totalItems: number): string => {
      const zoneLabel: string = zoneCount > 0 ? `${zoneCount} zone` : 'le zone';
      const itemLabel: string = totalItems > 0 ? ` con ${totalItems} luoghi elencati` : '';
      return `Esplora ${zoneLabel} di ${parkName}${itemLabel}: attrazioni, ristoranti, negozi e luoghi utili.`;
    }
  },
  nl: {
    parkFallback: 'dit park',
    breadcrumbLabel: 'Zones',
    title: (parkName: string): string => `Zones van ${parkName}`,
    description: (parkName: string, zoneCount: number, totalItems: number): string => {
      const zoneLabel: string = zoneCount > 0 ? `${zoneCount} zones` : 'de zones';
      const itemLabel: string = totalItems > 0 ? ` met ${totalItems} plekken` : '';
      return `Bekijk ${zoneLabel} van ${parkName}${itemLabel}: attracties, restaurants, winkels en handige plekken.`;
    }
  },
  pl: {
    parkFallback: 'tym parku',
    breadcrumbLabel: 'Strefy',
    title: (parkName: string): string => `Strefy parku ${parkName}`,
    description: (parkName: string, zoneCount: number, totalItems: number): string => {
      const zoneLabel: string = zoneCount > 0 ? `${zoneCount} stref` : 'strefy';
      const itemLabel: string = totalItems > 0 ? ` z ${totalItems} miejscami` : '';
      return `Przeglądaj ${zoneLabel} parku ${parkName}${itemLabel}: atrakcje, restauracje, sklepy i przydatne miejsca.`;
    }
  },
  pt: {
    parkFallback: 'este parque',
    breadcrumbLabel: 'Zonas',
    title: (parkName: string): string => `Zonas de ${parkName}`,
    description: (parkName: string, zoneCount: number, totalItems: number): string => {
      const zoneLabel: string = zoneCount > 0 ? `${zoneCount} zonas` : 'as zonas';
      const itemLabel: string = totalItems > 0 ? ` com ${totalItems} locais listados` : '';
      return `Explora ${zoneLabel} de ${parkName}${itemLabel}: atrações, restaurantes, lojas e locais úteis.`;
    }
  }
};

const PARK_ZONE_SEO_COPY: Record<string, ParkZoneSeoCopy> = {
  en: {
    parkFallback: 'this park',
    zoneFallback: 'this zone',
    title: (zoneName: string, parkName: string): string => `${zoneName} at ${parkName}`,
    description: (zoneName: string, parkName: string, totalItems: number): string => {
      const itemLabel: string = totalItems > 0 ? `${totalItems} listed places` : 'attractions, restaurants and places';
      return `Explore ${zoneName} at ${parkName}: ${itemLabel} in this park zone.`;
    }
  },
  fr: {
    parkFallback: 'ce parc',
    zoneFallback: 'cette zone',
    title: (zoneName: string, parkName: string): string => `${zoneName} à ${parkName}`,
    description: (zoneName: string, parkName: string, totalItems: number): string => {
      const itemLabel: string = totalItems > 0 ? `${totalItems} lieux répertoriés` : 'attractions, restaurants et lieux';
      return `Explore ${zoneName} à ${parkName} : ${itemLabel} dans cette zone du parc.`;
    }
  },
  es: {
    parkFallback: 'este parque',
    zoneFallback: 'esta zona',
    title: (zoneName: string, parkName: string): string => `${zoneName} en ${parkName}`,
    description: (zoneName: string, parkName: string, totalItems: number): string => {
      const itemLabel: string = totalItems > 0 ? `${totalItems} lugares listados` : 'atracciones, restaurantes y lugares';
      return `Explora ${zoneName} en ${parkName}: ${itemLabel} dentro de esta zona del parque.`;
    }
  },
  de: {
    parkFallback: 'diesem Park',
    zoneFallback: 'diesem Bereich',
    title: (zoneName: string, parkName: string): string => `${zoneName} in ${parkName}`,
    description: (zoneName: string, parkName: string, totalItems: number): string => {
      const itemLabel: string = totalItems > 0 ? `${totalItems} gelistete Orte` : 'Attraktionen, Restaurants und Orte';
      return `Entdecke ${zoneName} in ${parkName}: ${itemLabel} in diesem Parkbereich.`;
    }
  },
  it: {
    parkFallback: 'questo parco',
    zoneFallback: 'questa zona',
    title: (zoneName: string, parkName: string): string => `${zoneName} a ${parkName}`,
    description: (zoneName: string, parkName: string, totalItems: number): string => {
      const itemLabel: string = totalItems > 0 ? `${totalItems} luoghi elencati` : 'attrazioni, ristoranti e luoghi';
      return `Esplora ${zoneName} a ${parkName}: ${itemLabel} in questa zona del parco.`;
    }
  },
  nl: {
    parkFallback: 'dit park',
    zoneFallback: 'deze zone',
    title: (zoneName: string, parkName: string): string => `${zoneName} in ${parkName}`,
    description: (zoneName: string, parkName: string, totalItems: number): string => {
      const itemLabel: string = totalItems > 0 ? `${totalItems} plekken` : 'attracties, restaurants en plekken';
      return `Bekijk ${zoneName} in ${parkName}: ${itemLabel} in deze parkzone.`;
    }
  },
  pl: {
    parkFallback: 'tym parku',
    zoneFallback: 'tej strefie',
    title: (zoneName: string, parkName: string): string => `${zoneName} w ${parkName}`,
    description: (zoneName: string, parkName: string, totalItems: number): string => {
      const itemLabel: string = totalItems > 0 ? `${totalItems} miejsc` : 'atrakcje, restauracje i miejsca';
      return `Przeglądaj ${zoneName} w ${parkName}: ${itemLabel} w tej strefie parku.`;
    }
  },
  pt: {
    parkFallback: 'este parque',
    zoneFallback: 'esta zona',
    title: (zoneName: string, parkName: string): string => `${zoneName} em ${parkName}`,
    description: (zoneName: string, parkName: string, totalItems: number): string => {
      const itemLabel: string = totalItems > 0 ? `${totalItems} locais listados` : 'atrações, restaurantes e locais';
      return `Explora ${zoneName} em ${parkName}: ${itemLabel} nesta zona do parque.`;
    }
  }
};

const PARK_REFERENCE_SEO_COPY: Record<string, ParkReferenceSeoCopy> = {
  en: {
    breadcrumbLabel: 'Reference',
    kindLabels: {
      founder: 'founder',
      operator: 'operator',
      manufacturer: 'manufacturer'
    },
    title: (referenceName: string, kindLabel: string): string => `${referenceName} ${kindLabel}`,
    description: (referenceName: string, kindLabel: string, legalName: string | null, attractionCount: number): string => {
      const legalLabel: string = legalName ? ` also known as ${legalName}` : '';
      const attractionLabel: string = attractionCount > 0 ? ` and ${attractionCount} linked attractions` : '';
      return `Explore ${referenceName}${legalLabel}: public ${kindLabel} details${attractionLabel}.`;
    }
  },
  fr: {
    breadcrumbLabel: 'Référence',
    kindLabels: {
      founder: 'fondateur',
      operator: 'exploitant',
      manufacturer: 'constructeur'
    },
    title: (referenceName: string, kindLabel: string): string => `${referenceName}, ${kindLabel}`,
    description: (referenceName: string, kindLabel: string, legalName: string | null, attractionCount: number): string => {
      const legalLabel: string = legalName ? ` aussi référencé comme ${legalName}` : '';
      const attractionLabel: string = attractionCount > 0 ? ` et ${attractionCount} attractions liées` : '';
      return `Découvre ${referenceName}${legalLabel} : fiche publique ${kindLabel}${attractionLabel}.`;
    }
  },
  es: {
    breadcrumbLabel: 'Referencia',
    kindLabels: {
      founder: 'fundador',
      operator: 'operador',
      manufacturer: 'fabricante'
    },
    title: (referenceName: string, kindLabel: string): string => `${referenceName}, ${kindLabel}`,
    description: (referenceName: string, kindLabel: string, legalName: string | null, attractionCount: number): string => {
      const legalLabel: string = legalName ? ` también conocido como ${legalName}` : '';
      const attractionLabel: string = attractionCount > 0 ? ` y ${attractionCount} atracciones vinculadas` : '';
      return `Consulta ${referenceName}${legalLabel}: ficha pública de ${kindLabel}${attractionLabel}.`;
    }
  },
  de: {
    breadcrumbLabel: 'Referenz',
    kindLabels: {
      founder: 'Gründer',
      operator: 'Betreiber',
      manufacturer: 'Hersteller'
    },
    title: (referenceName: string, kindLabel: string): string => `${referenceName}, ${kindLabel}`,
    description: (referenceName: string, kindLabel: string, legalName: string | null, attractionCount: number): string => {
      const legalLabel: string = legalName ? ` auch bekannt als ${legalName}` : '';
      const attractionLabel: string = attractionCount > 0 ? ` und ${attractionCount} verknüpfte Attraktionen` : '';
      return `Entdecke ${referenceName}${legalLabel}: öffentliche ${kindLabel}-Infos${attractionLabel}.`;
    }
  },
  it: {
    breadcrumbLabel: 'Riferimento',
    kindLabels: {
      founder: 'fondatore',
      operator: 'operatore',
      manufacturer: 'costruttore'
    },
    title: (referenceName: string, kindLabel: string): string => `${referenceName}, ${kindLabel}`,
    description: (referenceName: string, kindLabel: string, legalName: string | null, attractionCount: number): string => {
      const legalLabel: string = legalName ? ` noto anche come ${legalName}` : '';
      const attractionLabel: string = attractionCount > 0 ? ` e ${attractionCount} attrazioni collegate` : '';
      return `Scopri ${referenceName}${legalLabel}: scheda pubblica ${kindLabel}${attractionLabel}.`;
    }
  },
  nl: {
    breadcrumbLabel: 'Referentie',
    kindLabels: {
      founder: 'oprichter',
      operator: 'operator',
      manufacturer: 'bouwer'
    },
    title: (referenceName: string, kindLabel: string): string => `${referenceName}, ${kindLabel}`,
    description: (referenceName: string, kindLabel: string, legalName: string | null, attractionCount: number): string => {
      const legalLabel: string = legalName ? ` ook bekend als ${legalName}` : '';
      const attractionLabel: string = attractionCount > 0 ? ` en ${attractionCount} gekoppelde attracties` : '';
      return `Bekijk ${referenceName}${legalLabel}: openbare ${kindLabel}-fiche${attractionLabel}.`;
    }
  },
  pl: {
    breadcrumbLabel: 'Referencja',
    kindLabels: {
      founder: 'założyciel',
      operator: 'operator',
      manufacturer: 'producent'
    },
    title: (referenceName: string, kindLabel: string): string => `${referenceName}, ${kindLabel}`,
    description: (referenceName: string, kindLabel: string, legalName: string | null, attractionCount: number): string => {
      const legalLabel: string = legalName ? ` znany także jako ${legalName}` : '';
      const attractionLabel: string = attractionCount > 0 ? ` i ${attractionCount} powiązanych atrakcji` : '';
      return `Przeglądaj ${referenceName}${legalLabel}: publiczna karta typu ${kindLabel}${attractionLabel}.`;
    }
  },
  pt: {
    breadcrumbLabel: 'Referência',
    kindLabels: {
      founder: 'fundador',
      operator: 'operador',
      manufacturer: 'fabricante'
    },
    title: (referenceName: string, kindLabel: string): string => `${referenceName}, ${kindLabel}`,
    description: (referenceName: string, kindLabel: string, legalName: string | null, attractionCount: number): string => {
      const legalLabel: string = legalName ? ` também conhecido como ${legalName}` : '';
      const attractionLabel: string = attractionCount > 0 ? ` e ${attractionCount} atrações ligadas` : '';
      return `Explora ${referenceName}${legalLabel}: ficha pública de ${kindLabel}${attractionLabel}.`;
    }
  }
};

const PARK_DETAIL_SEO_COPY: Record<string, ParkDetailSeoCopy> = {
  en: {
    description: (parkName: string, locationLabel: string): string =>
      `Explore ${parkName}${locationLabel ? ` in ${locationLabel}` : ''}: practical information, attractions, restaurants, hotels and park map.`
  },
  fr: {
    description: (parkName: string, locationLabel: string): string =>
      `Découvre ${parkName}${locationLabel ? ` à ${locationLabel}` : ''} : infos pratiques, attractions, restaurants, hôtels et carte du parc.`
  },
  es: {
    description: (parkName: string, locationLabel: string): string =>
      `Explora ${parkName}${locationLabel ? ` en ${locationLabel}` : ''}: información práctica, atracciones, restaurantes, hoteles y mapa del parque.`
  },
  de: {
    description: (parkName: string, locationLabel: string): string =>
      `Entdecke ${parkName}${locationLabel ? ` in ${locationLabel}` : ''}: praktische Infos, Attraktionen, Restaurants, Hotels und Parkkarte.`
  },
  it: {
    description: (parkName: string, locationLabel: string): string =>
      `Scopri ${parkName}${locationLabel ? ` a ${locationLabel}` : ''}: informazioni pratiche, attrazioni, ristoranti, hotel e mappa del parco.`
  },
  pl: {
    description: (parkName: string, locationLabel: string): string =>
      `Poznaj ${parkName}${locationLabel ? ` w ${locationLabel}` : ''}: informacje praktyczne, atrakcje, restauracje, hotele i mapa parku.`
  },
  nl: {
    description: (parkName: string, locationLabel: string): string =>
      `Ontdek ${parkName}${locationLabel ? ` in ${locationLabel}` : ''}: praktische info, attracties, restaurants, hotels en parkkaart.`
  },
  pt: {
    description: (parkName: string, locationLabel: string): string =>
      `Explora ${parkName}${locationLabel ? ` em ${locationLabel}` : ''}: informação prática, atrações, restaurantes, hotéis e mapa do parque.`
  }
};

const PARK_ITEM_DETAIL_SEO_COPY: Record<string, ParkItemDetailSeoCopy> = {
  en: {
    parkContextPrefix: 'at',
    description: (itemName: string, parkLabel: string): string =>
      `${itemName}${parkLabel}: category, type, practical details, photos and map information.`
  },
  fr: {
    parkContextPrefix: 'à',
    description: (itemName: string, parkLabel: string): string =>
      `${itemName}${parkLabel} : catégorie, type, infos pratiques, photos et repères sur la carte.`
  },
  es: {
    parkContextPrefix: 'en',
    description: (itemName: string, parkLabel: string): string =>
      `${itemName}${parkLabel}: categoría, tipo, datos prácticos, fotos e información en el mapa.`
  },
  de: {
    parkContextPrefix: 'in',
    description: (itemName: string, parkLabel: string): string =>
      `${itemName}${parkLabel}: Kategorie, Typ, praktische Details, Fotos und Karteninfos.`
  },
  it: {
    parkContextPrefix: 'a',
    description: (itemName: string, parkLabel: string): string =>
      `${itemName}${parkLabel}: categoria, tipo, dettagli pratici, foto e informazioni sulla mappa.`
  },
  pl: {
    parkContextPrefix: 'w',
    description: (itemName: string, parkLabel: string): string =>
      `${itemName}${parkLabel}: kategoria, typ, praktyczne szczegóły, zdjęcia i informacje na mapie.`
  },
  nl: {
    parkContextPrefix: 'in',
    description: (itemName: string, parkLabel: string): string =>
      `${itemName}${parkLabel}: categorie, type, praktische details, foto's en kaartinformatie.`
  },
  pt: {
    parkContextPrefix: 'em',
    description: (itemName: string, parkLabel: string): string =>
      `${itemName}${parkLabel}: categoria, tipo, detalhes práticos, fotos e informação no mapa.`
  }
};

const VIDEO_DETAIL_SEO_COPY: Record<string, VideoDetailSeoCopy> = {
  en: {
    parkFallback: 'Park',
    itemFallback: 'Item',
    videoFallback: 'Video',
    parkDescription: (videoTitle: string, parkName: string): string => `Watch ${videoTitle} from ${parkName}.`,
    itemDescription: (videoTitle: string, itemName: string, parkName: string): string => `Watch ${videoTitle} for ${itemName} at ${parkName}.`
  },
  fr: {
    parkFallback: 'Parc',
    itemFallback: 'Lieu',
    videoFallback: 'Vidéo',
    parkDescription: (videoTitle: string, parkName: string): string => `Regarde ${videoTitle} de ${parkName}.`,
    itemDescription: (videoTitle: string, itemName: string, parkName: string): string => `Regarde ${videoTitle} pour ${itemName} à ${parkName}.`
  },
  es: {
    parkFallback: 'Parque',
    itemFallback: 'Lugar',
    videoFallback: 'Vídeo',
    parkDescription: (videoTitle: string, parkName: string): string => `Mira ${videoTitle} de ${parkName}.`,
    itemDescription: (videoTitle: string, itemName: string, parkName: string): string => `Mira ${videoTitle} de ${itemName} en ${parkName}.`
  },
  de: {
    parkFallback: 'Park',
    itemFallback: 'Ort',
    videoFallback: 'Video',
    parkDescription: (videoTitle: string, parkName: string): string => `Sieh dir ${videoTitle} von ${parkName} an.`,
    itemDescription: (videoTitle: string, itemName: string, parkName: string): string => `Sieh dir ${videoTitle} zu ${itemName} in ${parkName} an.`
  },
  it: {
    parkFallback: 'Parco',
    itemFallback: 'Luogo',
    videoFallback: 'Video',
    parkDescription: (videoTitle: string, parkName: string): string => `Guarda ${videoTitle} di ${parkName}.`,
    itemDescription: (videoTitle: string, itemName: string, parkName: string): string => `Guarda ${videoTitle} per ${itemName} a ${parkName}.`
  },
  pl: {
    parkFallback: 'Park',
    itemFallback: 'Miejsce',
    videoFallback: 'Wideo',
    parkDescription: (videoTitle: string, parkName: string): string => `Obejrzyj ${videoTitle} z ${parkName}.`,
    itemDescription: (videoTitle: string, itemName: string, parkName: string): string => `Obejrzyj ${videoTitle} dla ${itemName} w ${parkName}.`
  },
  nl: {
    parkFallback: 'Park',
    itemFallback: 'Plek',
    videoFallback: 'Video',
    parkDescription: (videoTitle: string, parkName: string): string => `Bekijk ${videoTitle} van ${parkName}.`,
    itemDescription: (videoTitle: string, itemName: string, parkName: string): string => `Bekijk ${videoTitle} voor ${itemName} in ${parkName}.`
  },
  pt: {
    parkFallback: 'Parque',
    itemFallback: 'Local',
    videoFallback: 'Vídeo',
    parkDescription: (videoTitle: string, parkName: string): string => `Vê ${videoTitle} de ${parkName}.`,
    itemDescription: (videoTitle: string, itemName: string, parkName: string): string => `Vê ${videoTitle} para ${itemName} em ${parkName}.`
  }
};

const STATIC_SEO_COPY: Record<string, Record<string, StaticSeoCopy>> = {
  en: {
    home: {
      title: 'Amusement Parks — Explore parks, rides and destinations',
      description: DEFAULT_DESCRIPTION,
    },
    parks: {
      title: 'Amusement parks around the world — Amusement Parks',
      description: 'Browse visible amusement parks, theme parks, water parks, zoos and resorts with public details and map exploration.',
    },
    sitemap: {
      title: 'Sitemap - Amusement Parks',
      description: 'Explore the public sitemap for Amusement Parks with parks, interactive maps, technical guides and reference pages.',
    },
    rankings: {
      title: 'Rankings — Amusement Parks',
      description: 'Discover parks, attractions, restaurants, hotels and services that visitors consistently rate highly.',
    },
    technical: {
      title: 'Technical attraction guides - Amusement Parks',
      description: 'Understand attraction systems, coaster lifts, restraints, trains, materials and other technical elements.',
    },
    manufacturers: {
      title: 'Attraction manufacturers - Amusement Parks',
      description: 'Browse attraction and coaster manufacturers with their public profile, history and useful links.',
    },
    about: {
      title: 'About Amusement Parks — Project and data approach',
      description: 'Learn about the Amusement Parks project, its purpose, its public park portfolio and its careful data publication approach.',
    },
    contact: {
      title: 'Contact Amusement Parks — Feedback and requests',
      description: 'Contact Amusement Parks by email or leave a short protected message for the project administrators.',
    },
    versions: {
      title: 'Version history — Amusement Parks',
      description: 'Follow the public version history of Amusement Parks with short notes for each release.',
    },
    privacy: {
      title: 'Privacy policy — Amusement Parks',
      description: 'Read how Amusement Parks handles privacy, cookies, authentication data and analytics consent.',
    },
    notFound: {
      title: 'Page not found — Amusement Parks',
      description: 'The requested page does not exist or is no longer available on Amusement Parks.',
    },
    account: {
      title: 'Account — Amusement Parks',
      description: 'Private account page for Amusement Parks users.',
    },
    admin: {
      title: 'Administration — Amusement Parks',
      description: 'Private administration page for Amusement Parks.',
    },
  },
  fr: {
    manufacturers: {
      title: "Constructeurs d'attractions - Amusement Parks",
      description: "Parcours les constructeurs d'attractions et de coasters avec leur fiche publique, leur histoire et leurs liens utiles.",
    },
    home: {
      title: 'Amusement Parks — Explorer les parcs, attractions et destinations',
      description: 'Explore les parcs de loisirs, attractions, restaurants, hôtels et références du secteur partout dans le monde.',
    },
    parks: {
      title: 'Parcs de loisirs dans le monde — Amusement Parks',
      description: 'Parcours les parcs visibles, parcs à thèmes, parcs aquatiques, zoos et resorts avec leurs informations publiques et leur carte.',
    },
    sitemap: {
      title: 'Plan du site - Amusement Parks',
      description: 'Explore le plan public d’Amusement Parks avec les parcs, cartes interactives, dossiers techniques et pages de référence.',
    },
    rankings: {
      title: 'Classements — Amusement Parks',
      description: 'Découvre les parcs, attractions, restaurants, hôtels et services les plus régulièrement appréciés des visiteurs.',
    },
    technical: {
      title: 'Dossiers techniques - Amusement Parks',
      description: 'Explore les lifts, retenues, trains, materiaux et autres systemes techniques des attractions.',
    },
    about: {
      title: 'À propos — Amusement Parks',
      description: 'Découvre le projet Amusement Parks, son objectif, son portefeuille public de parcs et sa démarche de publication des données.',
    },
    contact: {
      title: 'Contact — Amusement Parks',
      description: 'Contacte Amusement Parks par email ou laisse un message court et protege aux administrateurs du projet.',
    },
    versions: {
      title: 'Historique des versions — Amusement Parks',
      description: "Suis l'historique public des versions d'Amusement Parks avec une note courte pour chaque mise en ligne.",
    },
    privacy: {
      title: 'Politique de confidentialité — Amusement Parks',
      description: 'Consulte la manière dont Amusement Parks gère la confidentialité, les cookies, les données de connexion et le consentement analytics.',
    },
    notFound: {
      title: 'Page introuvable — Amusement Parks',
      description: 'La page demandée n’existe pas ou n’est plus disponible sur Amusement Parks.',
    },
    account: {
      title: 'Compte — Amusement Parks',
      description: 'Page privée de compte utilisateur Amusement Parks.',
    },
    admin: {
      title: 'Administration — Amusement Parks',
      description: 'Page privée d’administration Amusement Parks.',
    },
  },

  es: {
    manufacturers: { title: 'Fabricantes de atracciones - Amusement Parks', description: 'Explora fabricantes de atracciones y coasters con su ficha pública, historia y enlaces útiles.' },
    home: { title: 'Amusement Parks — Explora parques, atracciones y destinos', description: 'Explora parques de ocio, atracciones, restaurantes, hoteles y referencias del sector en todo el mundo.' },
    parks: { title: 'Parques de ocio del mundo — Amusement Parks', description: 'Consulta parques visibles, parques temáticos, acuáticos, zoológicos y resorts con información pública y mapa.' },
    sitemap: { title: 'Mapa del sitio - Amusement Parks', description: 'Explora el mapa público de Amusement Parks con parques, mapas interactivos, guías técnicas y páginas de referencia.' },
    rankings: { title: 'Rankings — Amusement Parks', description: 'Descubre parques, atracciones, restaurantes, hoteles y servicios valorados de forma constante por visitantes.' },
    technical: { title: 'Guías técnicas - Amusement Parks', description: 'Explora lifts, sujeciones, trenes, materiales y otros sistemas técnicos de las atracciones.' },
    about: { title: 'Acerca de Amusement Parks — Proyecto y datos', description: 'Conoce el proyecto Amusement Parks, su objetivo y su enfoque cuidadoso de publicación de datos.' },
    contact: { title: 'Contacto — Amusement Parks', description: 'Contacta con Amusement Parks por correo o deja un mensaje breve y protegido para los administradores.' },
    versions: { title: 'Historial de versiones — Amusement Parks', description: 'Sigue el historial público de versiones de Amusement Parks con una nota breve por lanzamiento.' },
    privacy: { title: 'Política de privacidad — Amusement Parks', description: 'Consulta cómo Amusement Parks gestiona privacidad, cookies, datos de autenticación y consentimiento analítico.' },
    notFound: { title: 'Página no encontrada — Amusement Parks', description: 'La página solicitada no existe o ya no está disponible en Amusement Parks.' },
    account: { title: 'Cuenta — Amusement Parks', description: 'Página privada de cuenta de usuario de Amusement Parks.' },
    admin: { title: 'Administración — Amusement Parks', description: 'Página privada de administración de Amusement Parks.' },
  },
  de: {
    manufacturers: { title: 'Hersteller von Attraktionen - Amusement Parks', description: 'Durchsuche Hersteller von Attraktionen und Coastern mit Profil, Geschichte und nützlichen Links.' },
    home: { title: 'Amusement Parks — Parks, Attraktionen und Reiseziele entdecken', description: 'Entdecke Freizeitparks, Attraktionen, Restaurants, Hotels und Branchenreferenzen weltweit.' },
    parks: { title: 'Freizeitparks weltweit — Amusement Parks', description: 'Durchsuche sichtbare Freizeitparks, Themenparks, Wasserparks, Zoos und Resorts mit öffentlichen Details und Karte.' },
    sitemap: { title: 'Sitemap - Amusement Parks', description: 'Erkunde die öffentliche Sitemap von Amusement Parks mit Parks, interaktiven Karten, Technik-Guides und Referenzseiten.' },
    rankings: { title: 'Ranglisten — Amusement Parks', description: 'Entdecke Parks, Attraktionen, Restaurants, Hotels und Services, die Besucher dauerhaft hoch bewerten.' },
    technical: { title: 'Technik-Guides - Amusement Parks', description: 'Entdecke Lifts, Rückhaltesysteme, Züge, Materialien und weitere technische Systeme von Attraktionen.' },
    about: { title: 'Über Amusement Parks — Projekt und Datenansatz', description: 'Erfahre mehr über das Projekt Amusement Parks, seinen Zweck und seine sorgfältige Veröffentlichung von Daten.' },
    contact: { title: 'Kontakt — Amusement Parks', description: 'Kontaktiere Amusement Parks per E-Mail oder sende eine kurze geschützte Nachricht an die Administration.' },
    versions: { title: 'Versionsverlauf — Amusement Parks', description: 'Verfolge den öffentlichen Versionsverlauf von Amusement Parks mit kurzen Hinweisen je Version.' },
    privacy: { title: 'Datenschutzerklärung — Amusement Parks', description: 'Lies, wie Amusement Parks Datenschutz, Cookies, Anmeldedaten und Analytics-Zustimmung verarbeitet.' },
    notFound: { title: 'Seite nicht gefunden — Amusement Parks', description: 'Die angeforderte Seite existiert nicht oder ist auf Amusement Parks nicht mehr verfügbar.' },
    account: { title: 'Konto — Amusement Parks', description: 'Private Kontoseite für Benutzer von Amusement Parks.' },
    admin: { title: 'Administration — Amusement Parks', description: 'Private Administrationsseite von Amusement Parks.' },
  },
  it: {
    manufacturers: { title: 'Costruttori di attrazioni - Amusement Parks', description: 'Sfoglia costruttori di attrazioni e coaster con scheda pubblica, storia e link utili.' },
    home: { title: 'Amusement Parks — Esplora parchi, attrazioni e destinazioni', description: 'Esplora parchi divertimento, attrazioni, ristoranti, hotel e riferimenti del settore in tutto il mondo.' },
    parks: { title: 'Parchi divertimento nel mondo — Amusement Parks', description: 'Sfoglia parchi visibili, parchi a tema, acquatici, zoo e resort con informazioni pubbliche e mappa.' },
    sitemap: { title: 'Mappa del sito - Amusement Parks', description: 'Esplora la mappa pubblica di Amusement Parks con parchi, mappe interattive, guide tecniche e pagine di riferimento.' },
    rankings: { title: 'Classifiche — Amusement Parks', description: 'Scopri parchi, attrazioni, ristoranti, hotel e servizi valutati con continuità dai visitatori.' },
    technical: { title: 'Guide tecniche - Amusement Parks', description: 'Esplora lift, sistemi di ritenuta, treni, materiali e altri sistemi tecnici delle attrazioni.' },
    about: { title: 'Informazioni su Amusement Parks — Progetto e dati', description: 'Scopri il progetto Amusement Parks, il suo obiettivo e il suo approccio prudente alla pubblicazione dei dati.' },
    contact: { title: 'Contatto — Amusement Parks', description: 'Contatta Amusement Parks via email o lascia un breve messaggio protetto agli amministratori.' },
    versions: { title: 'Cronologia versioni — Amusement Parks', description: 'Segui la cronologia pubblica delle versioni di Amusement Parks con brevi note per ogni rilascio.' },
    privacy: { title: 'Informativa sulla privacy — Amusement Parks', description: 'Leggi come Amusement Parks gestisce privacy, cookie, dati di autenticazione e consenso analytics.' },
    notFound: { title: 'Pagina non trovata — Amusement Parks', description: 'La pagina richiesta non esiste o non è più disponibile su Amusement Parks.' },
    account: { title: 'Account — Amusement Parks', description: 'Pagina privata dell’account utente Amusement Parks.' },
    admin: { title: 'Amministrazione — Amusement Parks', description: 'Pagina privata di amministrazione Amusement Parks.' },
  },
  pl: {
    manufacturers: { title: 'Producenci atrakcji - Amusement Parks', description: 'Przeglądaj producentów atrakcji i coasterów z publicznym profilem, historią i przydatnymi linkami.' },
    home: { title: 'Amusement Parks — Odkrywaj parki, atrakcje i kierunki', description: 'Odkrywaj parki rozrywki, atrakcje, restauracje, hotele i referencje branżowe na całym świecie.' },
    parks: { title: 'Parki rozrywki na świecie — Amusement Parks', description: 'Przeglądaj widoczne parki rozrywki, parki tematyczne, wodne, zoo i resorty z publicznymi informacjami oraz mapą.' },
    sitemap: { title: 'Mapa strony - Amusement Parks', description: 'Przeglądaj publiczną mapę Amusement Parks z parkami, interaktywnymi mapami, przewodnikami technicznymi i stronami referencyjnymi.' },
    rankings: { title: 'Rankingi — Amusement Parks', description: 'Odkrywaj parki, atrakcje, restauracje, hotele i usługi stale wysoko oceniane przez odwiedzających.' },
    technical: { title: 'Przewodniki techniczne - Amusement Parks', description: 'Poznaj windy, zabezpieczenia, pociagi, materialy i inne systemy techniczne atrakcji.' },
    about: { title: 'O Amusement Parks — Projekt i dane', description: 'Poznaj projekt Amusement Parks, jego cel oraz ostrożne podejście do publikacji danych.' },
    contact: { title: 'Kontakt — Amusement Parks', description: 'Skontaktuj się z Amusement Parks e-mailem lub zostaw krótką chronioną wiadomość dla administratorów.' },
    versions: { title: 'Historia wersji — Amusement Parks', description: 'Śledź publiczną historię wersji Amusement Parks z krótkimi notatkami dla każdego wydania.' },
    privacy: { title: 'Polityka prywatności — Amusement Parks', description: 'Sprawdź, jak Amusement Parks zarządza prywatnością, cookies, danymi logowania i zgodą analityczną.' },
    notFound: { title: 'Nie znaleziono strony — Amusement Parks', description: 'Żądana strona nie istnieje albo nie jest już dostępna w Amusement Parks.' },
    account: { title: 'Konto — Amusement Parks', description: 'Prywatna strona konta użytkownika Amusement Parks.' },
    admin: { title: 'Administracja — Amusement Parks', description: 'Prywatna strona administracyjna Amusement Parks.' },
  },
  nl: {
    manufacturers: { title: 'Attractiebouwers - Amusement Parks', description: 'Bekijk attractie- en coasterbouwers met publiek profiel, geschiedenis en nuttige links.' },
    home: { title: 'Amusement Parks — Ontdek parken, attracties en bestemmingen', description: 'Ontdek pretparken, attracties, restaurants, hotels en brancheverwijzingen over de hele wereld.' },
    parks: { title: 'Pretparken wereldwijd — Amusement Parks', description: 'Bekijk zichtbare pretparken, themaparken, waterparken, dierentuinen en resorts met publieke info en kaart.' },
    sitemap: { title: 'Sitemap - Amusement Parks', description: 'Bekijk de publieke sitemap van Amusement Parks met parken, interactieve kaarten, technische gidsen en referentiepagina’s.' },
    rankings: { title: 'Ranglijsten — Amusement Parks', description: 'Ontdek parken, attracties, restaurants, hotels en services die bezoekers blijvend hoog beoordelen.' },
    technical: { title: 'Technische gidsen - Amusement Parks', description: 'Ontdek liften, beugels, treinen, materialen en andere technische systemen van attracties.' },
    about: { title: 'Over Amusement Parks — Project en data-aanpak', description: 'Lees meer over het Amusement Parks-project, het doel en de zorgvuldige aanpak voor datapublicatie.' },
    contact: { title: 'Contact — Amusement Parks', description: 'Neem contact op met Amusement Parks per e-mail of laat een kort beschermd bericht achter voor de beheerders.' },
    versions: { title: 'Versiegeschiedenis — Amusement Parks', description: 'Volg de publieke versiegeschiedenis van Amusement Parks met korte notities per release.' },
    privacy: { title: 'Privacybeleid — Amusement Parks', description: 'Lees hoe Amusement Parks omgaat met privacy, cookies, authenticatiegegevens en analytics-toestemming.' },
    notFound: { title: 'Pagina niet gevonden — Amusement Parks', description: 'De gevraagde pagina bestaat niet of is niet meer beschikbaar op Amusement Parks.' },
    account: { title: 'Account — Amusement Parks', description: 'Privé-accountpagina voor gebruikers van Amusement Parks.' },
    admin: { title: 'Administratie — Amusement Parks', description: 'Privé-administratiepagina van Amusement Parks.' },
  },
  pt: {
    manufacturers: { title: 'Fabricantes de atrações - Amusement Parks', description: 'Explore fabricantes de atrações e coasters com perfil público, história e links úteis.' },
    home: { title: 'Amusement Parks — Explore parques, atrações e destinos', description: 'Explore parques de diversão, atrações, restaurantes, hotéis e referências do setor em todo o mundo.' },
    parks: { title: 'Parques de diversão no mundo — Amusement Parks', description: 'Veja parques visíveis, parques temáticos, aquáticos, zoológicos e resorts com informações públicas e mapa.' },
    sitemap: { title: 'Mapa do site - Amusement Parks', description: 'Explora o mapa público do Amusement Parks com parques, mapas interativos, guias técnicos e páginas de referência.' },
    rankings: { title: 'Rankings — Amusement Parks', description: 'Descubra parques, atrações, restaurantes, hotéis e serviços avaliados de forma consistente pelos visitantes.' },
    technical: { title: 'Guias técnicos - Amusement Parks', description: 'Explora lifts, retenções, trens, materiais e outros sistemas técnicos das atrações.' },
    about: { title: 'Sobre o Amusement Parks — Projeto e dados', description: 'Conheça o projeto Amusement Parks, seu objetivo e sua abordagem cuidadosa de publicação de dados.' },
    contact: { title: 'Contacto — Amusement Parks', description: 'Contacte o Amusement Parks por email ou deixe uma mensagem curta e protegida para a administração.' },
    versions: { title: 'Histórico de versões — Amusement Parks', description: 'Acompanhe o histórico público de versões do Amusement Parks com notas curtas por lançamento.' },
    privacy: { title: 'Política de privacidade — Amusement Parks', description: 'Leia como o Amusement Parks trata privacidade, cookies, dados de autenticação e consentimento analítico.' },
    notFound: { title: 'Página não encontrada — Amusement Parks', description: 'A página solicitada não existe ou já não está disponível no Amusement Parks.' },
    account: { title: 'Conta — Amusement Parks', description: 'Página privada da conta de usuário do Amusement Parks.' },
    admin: { title: 'Administração — Amusement Parks', description: 'Página privada de administração do Amusement Parks.' },
  },
};

@Injectable({
  providedIn: 'root'
})
export class SeoService {
  private readonly managedAlternateSelector: string = 'link[rel="alternate"][data-managed-by="amusementpark-seo"]';
  private readonly canonicalSelector: string = 'link[rel="canonical"]';

  constructor(
    private readonly title: Title,
    private readonly meta: Meta,
    private readonly canonicalUrlService: CanonicalUrlService,
    private readonly hreflangService: HreflangService,
    private readonly jsonLdService: JsonLdService,
    @Inject(DOCUMENT) private readonly document: Document
  ) {
  }

  applyRouteDefaults(url: string): void {
    const language: string = this.resolveLanguageFromUrl(url);

    if (this.isAdminRoute(url)) {
      this.apply(this.buildStaticRouteData('admin', language, url, 'noindex,nofollow'));
      return;
    }

    if (this.isAccountRoute(url)) {
      this.apply(this.buildStaticRouteData('account', language, url, 'noindex,nofollow'));
      return;
    }

    if (this.isFilteredPublicParkItemsRoute(url) || this.isFilteredPublicParkZonesRoute(url) || this.isFilteredPublicParkZoneRoute(url) || this.isFilteredPublicParkImagesRoute(url) || this.isFilteredPublicParkItemImagesRoute(url) || this.isFilteredPublicParkVideosRoute(url) || this.isFilteredPublicParkItemVideosRoute(url) || this.isFilteredPublicParkWeatherRoute(url) || this.isFilteredPublicParkOpeningHoursRoute(url)) {
      this.apply({
        title: SITE_NAME,
        description: DEFAULT_DESCRIPTION,
        canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(url),
        robots: 'noindex,follow',
        alternates: this.hreflangService.buildAlternates(url),
      });
      return;
    }

    const staticRouteKey: string | null = this.resolveStaticRouteKey(url);
    if (staticRouteKey === 'notFound') {
      this.apply(this.buildStaticRouteData(staticRouteKey, language, url, 'noindex,follow'));
      return;
    }

    if (staticRouteKey) {
      this.apply(this.buildStaticRouteData(staticRouteKey, language, url, 'index,follow'));
      return;
    }

    this.apply({
      title: SITE_NAME,
      description: DEFAULT_DESCRIPTION,
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(url),
      robots: 'index,follow',
      alternates: this.hreflangService.buildAlternates(url),
    });
  }

  applyHomeSeo(language: string, url: string): void {
    this.apply(this.buildStaticRouteData('home', language, url, 'index,follow'));
  }

  applyParkListSeo(language: string, url: string): void {
    this.apply(this.buildStaticRouteData('parks', language, url, 'index,follow'));
  }

  applyTechnicalPageSeo(page: TechnicalPage, language: string, url: string): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const pageTitle: string = this.normalizeOptionalText(resolveLocalizedText(page.titles, normalizedLanguage, page.slug)) ?? page.slug;
    const pageDescription: string = this.normalizeOptionalText(resolveLocalizedText(page.summaries, normalizedLanguage, DEFAULT_DESCRIPTION)) ?? DEFAULT_DESCRIPTION;

    this.apply({
      title: `${pageTitle} - ${SITE_NAME}`,
      description: truncateSeoText(pageDescription, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(url),
      robots: 'index,follow',
      alternates: this.hreflangService.buildAlternates(url),
      imageUrl: this.resolveImageIdAbsoluteUrl(this.resolveTechnicalPageImageId(page)) ?? undefined,
      imageAlt: pageTitle,
      jsonLd: [this.buildTechnicalPageBreadcrumbJsonLd(pageTitle, url)]
    });
  }

  applyNotFoundSeo(language: string, url: string): void {
    if (this.isAdminRoute(url) || this.isAccountRoute(url)) {
      this.applyRouteDefaults(url);
      return;
    }

    this.apply(this.buildStaticRouteData('notFound', language, url, 'noindex,follow'));
  }

  applyParkDetailSeo(park: ParkDetailViewModel, language: string, url: string, canonicalPath: string | null = null): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: ParkDetailSeoCopy = PARK_DETAIL_SEO_COPY[normalizedLanguage] ?? PARK_DETAIL_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const locationLabel: string = [park.city, park.countryName ?? park.countryCode]
      .filter((value: string | null | undefined): value is string => !!value)
      .join(', ');
    const titleSuffix: string = locationLabel ? ` — ${locationLabel}` : '';
    const descriptionFallback: string = copy.description(park.name, locationLabel);

    this.apply({
      title: `${park.name}${titleSuffix} — ${SITE_NAME}`,
      description: truncateSeoText(normalizeSeoText(park.description, descriptionFallback), 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: 'index,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveImageIdAbsoluteUrl(park.primaryPhoto?.imageId) ?? undefined,
      imageAlt: park.name,
      jsonLd: this.buildParkDetailJsonLd(park, seoUrl)
    });
  }

  applyParkImagesSeo(
    park: Park,
    language: string,
    url: string,
    totalImages: number = 0,
    socialImageId: string | null = null,
    canonicalPath: string | null = null
  ): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: ParkImagesSeoCopy = PARK_IMAGES_SEO_COPY[normalizedLanguage] ?? PARK_IMAGES_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const parkName: string = this.normalizeOptionalText(park.name) ?? copy.parkFallback;
    const locationLabel: string = this.buildLocalizedLocationLabel(park, normalizedLanguage);
    const titleSuffix: string = locationLabel ? ` — ${locationLabel}` : '';
    const description: string = copy.description(parkName, locationLabel, totalImages);

    this.apply({
      title: `${copy.titlePrefix} ${parkName}${titleSuffix} — ${SITE_NAME}`,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: this.hasQueryString(url) || totalImages <= 0 ? 'noindex,follow' : 'index,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveImageIdAbsoluteUrl(socialImageId) ?? undefined,
      imageAlt: parkName,
      jsonLd: [this.buildParkSubpageBreadcrumbJsonLd(park, seoUrl, this.resolveParkImagesBreadcrumbLabel(normalizedLanguage, parkName))]
    });
  }

  applyParkItemImagesSeo(
    item: ParkItem,
    park: Park,
    language: string,
    url: string,
    totalImages: number = 0,
    socialImageId: string | null = null,
    canonicalPath: string | null = null
  ): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: ParkItemImagesSeoCopy = PARK_ITEM_IMAGES_SEO_COPY[normalizedLanguage] ?? PARK_ITEM_IMAGES_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const itemName: string = this.normalizeOptionalText(item.name) ?? copy.itemFallback;
    const parkName: string = this.normalizeOptionalText(park.name) ?? '';
    const locationLabel: string = this.buildLocalizedLocationLabel(park, normalizedLanguage);
    const titleContext: string = [parkName, locationLabel].filter((value: string) => value.length > 0).join(' — ');
    const titleSuffix: string = titleContext ? ` — ${titleContext}` : '';
    const description: string = copy.description(itemName, parkName, locationLabel, totalImages);

    this.apply({
      title: `${copy.titlePrefix} ${itemName}${titleSuffix} — ${SITE_NAME}`,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: this.hasQueryString(url) || totalImages <= 0 ? 'noindex,follow' : 'index,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveImageIdAbsoluteUrl(socialImageId) ?? undefined,
      imageAlt: itemName,
      jsonLd: [this.buildParkItemImagesBreadcrumbJsonLd(item, park, seoUrl, this.resolveParkItemImagesBreadcrumbLabel(normalizedLanguage, itemName))]
    });
  }

  applyParkVideosSeo(
    park: Park,
    language: string,
    url: string,
    totalVideos: number = 0,
    primaryVideoThumbnailPathOrUrl: string | null = null,
    parkImageId: string | null = null,
    canonicalPath: string | null = null
  ): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: ParkVideosSeoCopy = PARK_VIDEOS_SEO_COPY[normalizedLanguage] ?? PARK_VIDEOS_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const parkName: string = this.normalizeOptionalText(park.name) ?? copy.parkFallback;
    const locationLabel: string = this.buildLocalizedLocationLabel(park, normalizedLanguage);
    const titleSuffix: string = locationLabel ? ` — ${locationLabel}` : '';
    const description: string = copy.description(parkName, locationLabel, totalVideos);

    this.apply({
      title: `${copy.titlePrefix} ${parkName}${titleSuffix} — ${SITE_NAME}`,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: this.hasQueryString(url) || totalVideos <= 0 ? 'noindex,follow' : 'index,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveInternalImageIdAbsoluteUrl(primaryVideoThumbnailPathOrUrl)
        ?? this.resolveImageIdAbsoluteUrl(parkImageId)
        ?? undefined,
      imageAlt: parkName,
      jsonLd: [this.buildParkSubpageBreadcrumbJsonLd(park, seoUrl, this.resolveParkVideosBreadcrumbLabel(normalizedLanguage, parkName))]
    });
  }

  applyParkItemVideosSeo(
    item: ParkItem,
    park: Park,
    language: string,
    url: string,
    totalVideos: number = 0,
    primaryVideoThumbnailPathOrUrl: string | null = null,
    itemImageId: string | null = null,
    parkImageId: string | null = null,
    canonicalPath: string | null = null
  ): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: ParkItemVideosSeoCopy = PARK_ITEM_VIDEOS_SEO_COPY[normalizedLanguage] ?? PARK_ITEM_VIDEOS_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const itemName: string = this.normalizeOptionalText(item.name) ?? copy.itemFallback;
    const parkName: string = this.normalizeOptionalText(park.name) ?? '';
    const locationLabel: string = this.buildLocalizedLocationLabel(park, normalizedLanguage);
    const titleContext: string = [parkName, locationLabel].filter((value: string) => value.length > 0).join(' — ');
    const titleSuffix: string = titleContext ? ` — ${titleContext}` : '';
    const description: string = copy.description(itemName, parkName, locationLabel, totalVideos);

    this.apply({
      title: `${copy.titlePrefix} ${itemName}${titleSuffix} — ${SITE_NAME}`,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: this.hasQueryString(url) || totalVideos <= 0 ? 'noindex,follow' : 'index,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveInternalImageIdAbsoluteUrl(primaryVideoThumbnailPathOrUrl)
        ?? this.resolveImageIdAbsoluteUrl(itemImageId)
        ?? this.resolveImageIdAbsoluteUrl(parkImageId)
        ?? undefined,
      imageAlt: itemName,
      jsonLd: [this.buildParkItemSubpageBreadcrumbJsonLd(item, park, seoUrl, this.resolveParkItemVideosBreadcrumbLabel(normalizedLanguage, itemName))]
    });
  }

  applyParkVideoSeo(
    video: VideoDto,
    park: Park,
    language: string,
    url: string,
    parkImageId: string | null = null,
    canonicalPath: string | null = null
  ): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: VideoDetailSeoCopy = VIDEO_DETAIL_SEO_COPY[normalizedLanguage] ?? VIDEO_DETAIL_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const videoTitle: string = this.resolveVideoTitle(video, normalizedLanguage);
    const parkName: string = this.normalizeOptionalText(park.name) ?? copy.parkFallback;
    const description: string = this.resolveVideoDescription(video, normalizedLanguage)
      ?? copy.parkDescription(videoTitle, parkName);

    this.apply({
      title: `${videoTitle} — ${parkName} — ${SITE_NAME}`,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: 'index,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveVideoInternalThumbnailAbsoluteUrl(video)
        ?? this.resolveImageIdAbsoluteUrl(parkImageId)
        ?? undefined,
      imageAlt: videoTitle,
      jsonLd: this.buildParkVideoJsonLd(video, park, seoUrl, videoTitle, description)
    });
  }

  applyParkItemVideoSeo(
    video: VideoDto,
    item: ParkItem,
    park: Park,
    language: string,
    url: string,
    itemImageId: string | null = null,
    parkImageId: string | null = null,
    canonicalPath: string | null = null
  ): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: VideoDetailSeoCopy = VIDEO_DETAIL_SEO_COPY[normalizedLanguage] ?? VIDEO_DETAIL_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const videoTitle: string = this.resolveVideoTitle(video, normalizedLanguage);
    const itemName: string = this.normalizeOptionalText(item.name) ?? copy.itemFallback;
    const parkName: string = this.normalizeOptionalText(park.name) ?? copy.parkFallback;
    const description: string = this.resolveVideoDescription(video, normalizedLanguage)
      ?? copy.itemDescription(videoTitle, itemName, parkName);

    this.apply({
      title: `${videoTitle} — ${itemName} — ${SITE_NAME}`,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: 'index,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveVideoInternalThumbnailAbsoluteUrl(video)
        ?? this.resolveImageIdAbsoluteUrl(itemImageId)
        ?? this.resolveImageIdAbsoluteUrl(parkImageId)
        ?? undefined,
      imageAlt: videoTitle,
      jsonLd: this.buildParkItemVideoJsonLd(video, item, park, seoUrl, videoTitle, description)
    });
  }

  applyParkMapSeo(
    park: Park,
    language: string,
    url: string,
    parkImageId: string | null = null,
    canonicalPath: string | null = null,
    isIndexable: boolean = true
  ): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: ParkMapSeoCopy = PARK_MAP_SEO_COPY[normalizedLanguage] ?? PARK_MAP_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const parkName: string = this.normalizeOptionalText(park.name) ?? copy.parkFallback;
    const locationLabel: string = this.buildLocalizedLocationLabel(park, normalizedLanguage);
    const titleSuffix: string = locationLabel ? ` — ${locationLabel}` : '';
    const description: string = copy.description(parkName, locationLabel);

    this.apply({
      title: `${copy.titlePrefix} ${parkName}${titleSuffix} — ${SITE_NAME}`,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: isIndexable ? 'index,follow' : 'noindex,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveImageIdAbsoluteUrl(parkImageId) ?? undefined,
      imageAlt: parkName,
      jsonLd: [this.buildParkSubpageBreadcrumbJsonLd(park, seoUrl, this.resolveParkMapBreadcrumbLabel(normalizedLanguage, parkName))]
    });
  }

  applyParkItemsSeo(parkName: string, language: string, url: string, parkImageId: string | null = null, canonicalPath: string | null = null): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: ParkItemsSeoCopy = PARK_ITEMS_SEO_COPY[normalizedLanguage] ?? PARK_ITEMS_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const normalizedParkName: string = this.normalizeOptionalText(parkName) ?? copy.parkFallback;
    const title: string = `${copy.title(normalizedParkName)} — ${SITE_NAME}`;
    const description: string = copy.description(normalizedParkName);

    this.apply({
      title,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: this.hasQueryString(url) ? 'noindex,follow' : 'index,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveImageIdAbsoluteUrl(parkImageId) ?? undefined,
      imageAlt: normalizedParkName,
      jsonLd: [this.buildParkSubpageBreadcrumbJsonLd({ name: normalizedParkName } as Park, seoUrl, this.resolveParkItemsBreadcrumbLabel(normalizedLanguage, normalizedParkName))]
    });
  }

  applyParkWeatherSeo(
    parkName: string,
    language: string,
    url: string,
    totalDays: number = 0,
    parkImageId: string | null = null,
    canonicalPath: string | null = null
  ): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: ParkWeatherSeoCopy = PARK_WEATHER_SEO_COPY[normalizedLanguage] ?? PARK_WEATHER_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const normalizedParkName: string = this.normalizeOptionalText(parkName) ?? copy.parkFallback;
    const title: string = `${copy.title(normalizedParkName)} - ${SITE_NAME}`;
    const description: string = copy.description(normalizedParkName, totalDays);

    this.apply({
      title,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: this.hasQueryString(url) || totalDays <= 0 ? 'noindex,follow' : 'index,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveImageIdAbsoluteUrl(parkImageId) ?? undefined,
      imageAlt: normalizedParkName,
      jsonLd: [this.buildParkSubpageBreadcrumbJsonLd({ name: normalizedParkName } as Park, seoUrl, this.resolveParkWeatherBreadcrumbLabel(normalizedLanguage, normalizedParkName))]
    });
  }

  applyParkOpeningHoursSeo(
    parkName: string,
    language: string,
    url: string,
    totalDays: number = 0,
    parkImageId: string | null = null,
    canonicalPath: string | null = null
  ): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: ParkOpeningHoursSeoCopy = PARK_OPENING_HOURS_SEO_COPY[normalizedLanguage] ?? PARK_OPENING_HOURS_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const normalizedParkName: string = this.normalizeOptionalText(parkName) ?? copy.parkFallback;
    const title: string = `${copy.title(normalizedParkName)} - ${SITE_NAME}`;
    const description: string = copy.description(normalizedParkName, totalDays);

    this.apply({
      title,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: this.hasQueryString(url) || totalDays <= 0 ? 'noindex,follow' : 'index,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveImageIdAbsoluteUrl(parkImageId) ?? undefined,
      imageAlt: normalizedParkName,
      jsonLd: [this.buildParkSubpageBreadcrumbJsonLd({ name: normalizedParkName } as Park, seoUrl, this.resolveParkOpeningHoursBreadcrumbLabel(normalizedLanguage, normalizedParkName))]
    });
  }

  applyParkZonesSeo(
    parkName: string,
    language: string,
    url: string,
    parkImageId: string | null = null,
    zoneCount: number = 0,
    totalItems: number = 0,
    canonicalPath: string | null = null
  ): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: ParkZonesSeoCopy = PARK_ZONES_SEO_COPY[normalizedLanguage] ?? PARK_ZONES_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const normalizedParkName: string = this.normalizeOptionalText(parkName) ?? copy.parkFallback;
    const title: string = `${copy.title(normalizedParkName)} — ${SITE_NAME}`;
    const description: string = copy.description(normalizedParkName, zoneCount, totalItems);

    this.apply({
      title,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: this.hasQueryString(url) ? 'noindex,follow' : 'index,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveImageIdAbsoluteUrl(parkImageId) ?? undefined,
      imageAlt: normalizedParkName,
      jsonLd: [this.buildParkSubpageBreadcrumbJsonLd({ name: normalizedParkName } as Park, seoUrl, this.resolveParkZonesBreadcrumbLabel(normalizedLanguage, normalizedParkName))]
    });
  }

  applyParkZoneSeo(
    parkName: string,
    zoneName: string,
    language: string,
    url: string,
    parkImageId: string | null = null,
    totalItems: number = 0,
    canonicalPath: string | null = null
  ): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: ParkZoneSeoCopy = PARK_ZONE_SEO_COPY[normalizedLanguage] ?? PARK_ZONE_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const normalizedParkName: string = this.normalizeOptionalText(parkName) ?? copy.parkFallback;
    const normalizedZoneName: string = this.normalizeOptionalText(zoneName) ?? copy.zoneFallback;
    const title: string = `${copy.title(normalizedZoneName, normalizedParkName)} — ${SITE_NAME}`;
    const description: string = copy.description(normalizedZoneName, normalizedParkName, totalItems);

    this.apply({
      title,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: this.hasQueryString(url) ? 'noindex,follow' : 'index,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveImageIdAbsoluteUrl(parkImageId) ?? undefined,
      imageAlt: normalizedZoneName,
      jsonLd: [this.buildParkSubpageBreadcrumbJsonLd({ name: normalizedParkName } as Park, seoUrl, normalizedZoneName)]
    });
  }

  applyParkReferenceSeo(
    reference: ParkReferenceDetailViewModel,
    language: string,
    url: string,
    canonicalPath: string | null = null
  ): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: ParkReferenceSeoCopy = PARK_REFERENCE_SEO_COPY[normalizedLanguage] ?? PARK_REFERENCE_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const referenceName: string = this.normalizeOptionalText(reference.name) ?? SITE_NAME;
    const kindLabel: string = copy.kindLabels[reference.kind] ?? copy.kindLabels.operator;
    const attractionCount: number = reference.attractionsPagination?.totalItems ?? reference.attractions?.length ?? 0;
    const fallbackDescription: string = copy.description(
      referenceName,
      kindLabel,
      this.normalizeOptionalText(reference.legalName),
      attractionCount
    );
    const description: string = normalizeSeoText(stripHtml(reference.richDescription), fallbackDescription);
    const imageId: string | null = this.normalizeOptionalText(reference.heroLogoImageId)
      ?? this.normalizeOptionalText(reference.photos?.[0]?.imageId);

    this.apply({
      title: `${copy.title(referenceName, kindLabel)} — ${SITE_NAME}`,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: 'index,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveImageIdAbsoluteUrl(imageId) ?? undefined,
      imageAlt: referenceName,
      jsonLd: this.buildParkReferenceJsonLd(reference, seoUrl, referenceName, description)
    });
  }

  applyParkItemDetailSeo(detail: ParkItemDetailViewModel, language: string, url: string, canonicalPath: string | null = null): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: ParkItemDetailSeoCopy = PARK_ITEM_DETAIL_SEO_COPY[normalizedLanguage] ?? PARK_ITEM_DETAIL_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const parkLabel: string = detail.parkName ? ` ${copy.parkContextPrefix} ${detail.parkName}` : '';
    const title: string = `${detail.name}${parkLabel} — ${SITE_NAME}`;
    const specSummary: string = this.buildParkItemSpecSummary(detail);
    const descriptionFallback: string = specSummary
      ? this.buildParkItemSpecDescription(normalizedLanguage, detail.name, parkLabel, specSummary)
      : copy.description(detail.name, parkLabel, specSummary);

    this.apply({
      title,
      description: truncateSeoText(normalizeSeoText(detail.description, descriptionFallback), 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: 'index,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveImageIdAbsoluteUrl(detail.heroPhoto?.imageId) ?? undefined,
      imageAlt: detail.name,
      jsonLd: this.buildParkItemDetailJsonLd(detail, seoUrl)
    });
  }

  applyHistoryTimelineSeo(timeline: HistoryTimelinePageViewModel, language: string, url: string, canonicalPath: string | null = null): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: HistorySeoCopy = HISTORY_SEO_COPY[normalizedLanguage] ?? HISTORY_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const title: string = `${timeline.title} — ${SITE_NAME}`;
    const description: string = copy.timelineDescription(timeline.ownerName);
    const imageId: string | null = timeline.events.find((event) => !!event.mainImageId)?.mainImageId ?? null;

    this.apply({
      title,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: timeline.events.length > 0 ? 'index,follow' : 'noindex,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveImageIdAbsoluteUrl(imageId) ?? undefined,
      imageAlt: timeline.ownerName,
      jsonLd: [
        this.buildHistoryTimelineBreadcrumbJsonLd(timeline, seoUrl),
        this.buildHistoryTimelineJsonLd(timeline, seoUrl, description)
      ]
    });
  }

  applyHistoryArticleSeo(article: HistoryArticlePageViewModel, language: string, url: string, canonicalPath: string | null = null): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: HistorySeoCopy = HISTORY_SEO_COPY[normalizedLanguage] ?? HISTORY_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const seoUrl: string = this.resolveSeoUrl(url, canonicalPath);
    const descriptionFallback: string = copy.articleDescription(article.ownerName, article.dateLabel);
    const description: string = truncateSeoText(normalizeSeoText(article.summary, descriptionFallback), 160);

    this.apply({
      title: `${article.title} — ${SITE_NAME}`,
      description,
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(seoUrl),
      robots: 'index,follow',
      alternates: this.hreflangService.buildAlternates(seoUrl),
      imageUrl: this.resolveImageIdAbsoluteUrl(article.mainImageId) ?? undefined,
      imageAlt: article.title,
      jsonLd: [
        this.buildHistoryArticleBreadcrumbJsonLd(article, seoUrl),
        this.buildHistoryArticleJsonLd(article, seoUrl, description)
      ]
    });
  }

  setHtmlLanguage(language: string): void {
    const normalizedLanguage: string = language?.trim() || SEO_DEFAULT_LANGUAGE;
    this.document.documentElement.lang = normalizedLanguage;
  }

  private apply(data: SeoRouteData): void {
    this.title.setTitle(data.title);
    this.meta.updateTag({ name: 'description', content: data.description });
    this.meta.updateTag({ name: 'robots', content: data.robots });
    this.meta.updateTag({ name: 'googlebot', content: data.robots });
    const socialImage: SocialImageMetadata = this.buildSocialImage(data.imageUrl);
    const socialImageAlt: string = this.resolveSocialImageAlt(data);
    const locale: string = this.resolveOpenGraphLocale(data.canonicalUrl);

    this.meta.updateTag({ property: 'og:site_name', content: SITE_NAME });
    this.meta.updateTag({ property: 'og:title', content: data.title });
    this.meta.updateTag({ property: 'og:description', content: data.description });
    this.meta.updateTag({ property: 'og:url', content: data.canonicalUrl });
    this.meta.updateTag({ property: 'og:type', content: 'website' });
    this.meta.updateTag({ property: 'og:locale', content: locale });
    this.meta.updateTag({ property: 'og:image', content: socialImage.url });
    this.meta.updateTag({ property: 'og:image:secure_url', content: socialImage.url });
    this.updateOpenGraphImageDimension('width', socialImage.width);
    this.updateOpenGraphImageDimension('height', socialImage.height);
    this.meta.updateTag({ property: 'og:image:alt', content: socialImageAlt });
    this.meta.updateTag({ name: 'twitter:card', content: 'summary_large_image' });
    this.meta.updateTag({ name: 'twitter:title', content: data.title });
    this.meta.updateTag({ name: 'twitter:description', content: data.description });
    this.meta.updateTag({ name: 'twitter:image', content: socialImage.url });
    this.meta.updateTag({ name: 'twitter:image:alt', content: socialImageAlt });
    this.setCanonical(data.canonicalUrl);
    this.setAlternates(data.alternates);
    this.jsonLdService.setJsonLd(data.jsonLd ?? []);
  }

  private buildSocialImage(imageUrl: string | undefined): SocialImageMetadata {
    const fallbackImage: SocialImageMetadata = {
      url: this.canonicalUrlService.buildAbsoluteUrl(DEFAULT_SOCIAL_IMAGE_PATH),
      width: DEFAULT_SOCIAL_IMAGE_WIDTH,
      height: DEFAULT_SOCIAL_IMAGE_HEIGHT
    };
    const normalizedImageUrl: string | null = this.normalizeOptionalText(imageUrl);

    if (normalizedImageUrl === null) {
      return fallbackImage;
    }

    try {
      const parsedUrl: URL = new URL(normalizedImageUrl);
      const normalizedPath: string = parsedUrl.pathname.toLowerCase();

      if (normalizedPath === DEFAULT_SOCIAL_IMAGE_PATH) {
        return {
          ...fallbackImage,
          url: parsedUrl.href
        };
      }

      if (normalizedPath.startsWith('/images/') || normalizedPath.startsWith('/api/images/')) {
        parsedUrl.searchParams.set('width', String(SOCIAL_IMAGE_WIDTH));
        parsedUrl.searchParams.set('v', RESPONSIVE_IMAGE_VERSION);

        return {
          url: parsedUrl.href,
          width: SOCIAL_IMAGE_WIDTH,
          height: null
        };
      }

      return {
        url: parsedUrl.href,
        width: null,
        height: null
      };
    } catch {
      return fallbackImage;
    }
  }

  private updateOpenGraphImageDimension(dimension: 'width' | 'height', value: number | null): void {
    if (value === null) {
      this.meta.removeTag(`property="og:image:${dimension}"`);
      return;
    }

    this.meta.updateTag({ property: `og:image:${dimension}`, content: String(value) });
  }

  private resolveSocialImageAlt(data: SeoRouteData): string {
    return truncateSeoText(this.normalizeOptionalText(data.imageAlt) ?? data.title, 420);
  }

  private resolveOpenGraphLocale(url: string): string {
    const language: string = this.resolveLanguageFromUrl(url);

    switch (language) {
      case 'fr':
        return 'fr_FR';
      case 'es':
        return 'es_ES';
      case 'de':
        return 'de_DE';
      case 'it':
        return 'it_IT';
      case 'pl':
        return 'pl_PL';
      case 'nl':
        return 'nl_NL';
      case 'pt':
        return 'pt_PT';
      default:
        return 'en_US';
    }
  }

  private buildLocalizedLocationLabel(park: Park, language: string): string {
    return [
      this.normalizeOptionalText(park.city),
      this.resolveLocalizedCountryName(park.countryCode, language)
    ]
      .filter((value: string | null): value is string => value !== null)
      .join(', ');
  }

  private resolveLocalizedCountryName(countryCode: string | null | undefined, language: string): string | null {
    const normalizedCountryCode: string | null = this.normalizeOptionalText(countryCode)?.toUpperCase() ?? null;

    if (!normalizedCountryCode) {
      return null;
    }

    const displayNamesConstructor = (Intl as unknown as IntlWithDisplayNames).DisplayNames;

    if (!displayNamesConstructor) {
      return normalizedCountryCode;
    }

    try {
      const regionDisplayNames: RegionDisplayNames = new displayNamesConstructor([language], { type: 'region' });
      return regionDisplayNames.of(normalizedCountryCode) ?? normalizedCountryCode;
    } catch {
      return normalizedCountryCode;
    }
  }

  private normalizeOptionalText(value: string | null | undefined): string | null {
    const normalized: string = value?.trim() ?? '';
    return normalized.length > 0 ? normalized : null;
  }

  private normalizeLanguage(language: string | null | undefined): string {
    const normalizedLanguage: string = language?.trim().toLowerCase() ?? '';
    return normalizedLanguage in PARK_IMAGES_SEO_COPY ? normalizedLanguage : SEO_DEFAULT_LANGUAGE;
  }

  private normalizeLanguageCode(language: string | null | undefined): string {
    const normalizedLanguage: string = language?.trim().toLowerCase() ?? '';
    return normalizedLanguage.length >= 2 ? normalizedLanguage.slice(0, 2) : normalizedLanguage;
  }

  private resolveSeoUrl(url: string, canonicalPath: string | null | undefined): string {
    return this.normalizeOptionalText(canonicalPath) ?? url;
  }

  private buildParkItemSpecSummary(detail: ParkItemDetailViewModel): string {
    const values: string[] = [];

    this.pushUniqueSeoValue(values, detail.manufacturerName);
    this.pushUniqueSeoValue(values, detail.modelName);
    this.pushUniqueSeoValue(values, detail.zoneName);

    for (const row of detail.spotlightRows ?? []) {
      this.pushUniqueSeoValue(values, row.value);
    }

    for (const condition of detail.accessConditions ?? []) {
      for (const metric of condition.metrics ?? []) {
        this.pushUniqueSeoValue(values, metric.value);
      }
    }

    return values.slice(0, 5).join(', ');
  }

  private pushUniqueSeoValue(values: string[], value: string | null | undefined): void {
    const normalizedValue: string | null = this.normalizeOptionalText(value);

    if (!normalizedValue) {
      return;
    }

    if (values.some((existingValue: string): boolean => existingValue.toLowerCase() === normalizedValue.toLowerCase())) {
      return;
    }

    values.push(normalizedValue);
  }

  private buildParkItemSpecDescription(language: string, itemName: string, parkLabel: string, specSummary: string): string {
    if (language === 'fr') {
      return `${itemName}${parkLabel} : ${specSummary}. Photos, repères et infos utiles.`;
    }

    if (language === 'es') {
      return `${itemName}${parkLabel}: ${specSummary}. Fotos, ubicación y datos útiles.`;
    }

    if (language === 'de') {
      return `${itemName}${parkLabel}: ${specSummary}. Fotos, Standort und praktische Infos.`;
    }

    if (language === 'it') {
      return `${itemName}${parkLabel}: ${specSummary}. Foto, posizione e dettagli utili.`;
    }

    if (language === 'nl') {
      return `${itemName}${parkLabel}: ${specSummary}. Foto's, locatie en praktische info.`;
    }

    if (language === 'pl') {
      return `${itemName}${parkLabel}: ${specSummary}. Zdjecia, lokalizacja i praktyczne informacje.`;
    }

    if (language === 'pt') {
      return `${itemName}${parkLabel}: ${specSummary}. Fotos, localização e informação útil.`;
    }

    return `${itemName}${parkLabel}: ${specSummary}. Photos, location and visit details.`;
  }

  private buildParkDetailJsonLd(park: ParkDetailViewModel, url: string): unknown[] {
    const canonicalUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(url);
    const language: string = this.resolveLanguageFromUrl(url);
    const jsonLd: unknown[] = [this.buildBreadcrumbJsonLd([
      { name: this.resolveHomeBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/home`) },
      { name: this.resolveParksBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/parks`) },
      { name: park.name, url: canonicalUrl }
    ])];

    const parkJsonLd: Record<string, unknown> = {
      '@context': 'https://schema.org',
      '@type': 'AmusementPark',
      name: park.name,
      url: canonicalUrl
    };

    const description: string = normalizeSeoText(park.description, '');
    if (description) {
      parkJsonLd['description'] = truncateSeoText(description, 300);
    }

    if (park.websiteUrl) {
      parkJsonLd['sameAs'] = [park.websiteUrl];
    }

    const address: Record<string, string> = {};
    if (park.street) {
      address['streetAddress'] = park.street;
    }
    if (park.city) {
      address['addressLocality'] = park.city;
    }
    if (park.postalCode) {
      address['postalCode'] = park.postalCode;
    }
    if (park.countryCode) {
      address['addressCountry'] = park.countryCode;
    }
    if (Object.keys(address).length > 0) {
      parkJsonLd['address'] = { '@type': 'PostalAddress', ...address };
    }

    if (park.latitude !== null && park.longitude !== null) {
      parkJsonLd['geo'] = {
        '@type': 'GeoCoordinates',
        latitude: park.latitude,
        longitude: park.longitude
      };
    }

    jsonLd.push(parkJsonLd);
    return jsonLd;
  }


  private buildParkSubpageBreadcrumbJsonLd(park: Park, url: string, pageLabel: string): unknown {
    const canonicalUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(url);
    const language: string = this.resolveLanguageFromUrl(url);
    const segments: string[] = this.getPathSegments(url);
    const parkDetailPath: string = segments.length >= 4
      ? `/${segments.slice(0, 4).join('/')}`
      : `/${language}/parks`;

    return this.buildBreadcrumbJsonLd([
      { name: this.resolveHomeBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/home`) },
      { name: this.resolveParksBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/parks`) },
      { name: park.name ?? 'Park', url: this.canonicalUrlService.buildAbsoluteUrl(parkDetailPath) },
      { name: pageLabel, url: canonicalUrl }
    ]);
  }

  private buildParkItemImagesBreadcrumbJsonLd(item: ParkItem, park: Park, url: string, pageLabel: string): unknown {
    return this.buildParkItemSubpageBreadcrumbJsonLd(item, park, url, pageLabel);
  }

  private buildParkItemSubpageBreadcrumbJsonLd(item: ParkItem, park: Park, url: string, pageLabel: string): unknown {
    const canonicalUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(url);
    const language: string = this.resolveLanguageFromUrl(url);
    const segments: string[] = this.getPathSegments(url);
    const parkDetailPath: string = segments.length >= 4
      ? `/${segments.slice(0, 4).join('/')}`
      : `/${language}/parks`;
    const itemDetailPath: string = segments.length >= 7
      ? `/${segments.slice(0, 7).join('/')}`
      : parkDetailPath;

    return this.buildBreadcrumbJsonLd([
      { name: this.resolveHomeBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/home`) },
      { name: this.resolveParksBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/parks`) },
      { name: park.name ?? 'Park', url: this.canonicalUrlService.buildAbsoluteUrl(parkDetailPath) },
      { name: item.name ?? 'Item', url: this.canonicalUrlService.buildAbsoluteUrl(itemDetailPath) },
      { name: pageLabel, url: canonicalUrl }
    ]);
  }

  private buildHistoryTimelineBreadcrumbJsonLd(timeline: HistoryTimelinePageViewModel, url: string): unknown {
    const language: string = this.resolveLanguageFromUrl(url);
    const label: string = this.resolveHistoryBreadcrumbLabel(language, timeline.ownerName);

    if (timeline.parkItem && timeline.park) {
      return this.buildParkItemSubpageBreadcrumbJsonLd(timeline.parkItem, timeline.park, url, label);
    }

    return this.buildParkSubpageBreadcrumbJsonLd(timeline.park ?? { name: timeline.ownerName } as Park, url, label);
  }

  private buildHistoryArticleBreadcrumbJsonLd(article: HistoryArticlePageViewModel, url: string): unknown {
    const canonicalUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(url);
    const language: string = this.resolveLanguageFromUrl(url);
    const segments: string[] = this.getPathSegments(url);
    const parkDetailPath: string = segments.length >= 4
      ? `/${segments.slice(0, 4).join('/')}`
      : `/${language}/parks`;
    const isItemArticle: boolean = !!article.parkItem;
    const itemDetailPath: string = isItemArticle && segments.length >= 7
      ? `/${segments.slice(0, 7).join('/')}`
      : parkDetailPath;
    const timelinePath: string = isItemArticle && segments.length >= 8
      ? `/${segments.slice(0, 8).join('/')}`
      : segments.length >= 5
        ? `/${segments.slice(0, 5).join('/')}`
        : parkDetailPath;
    const items: Array<{ name: string; url: string }> = [
      { name: this.resolveHomeBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/home`) },
      { name: this.resolveParksBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/parks`) },
      { name: article.park?.name ?? article.contextPark?.name ?? 'Park', url: this.canonicalUrlService.buildAbsoluteUrl(parkDetailPath) }
    ];

    if (article.parkItem) {
      items.push({
        name: article.parkItem.name,
        url: this.canonicalUrlService.buildAbsoluteUrl(itemDetailPath)
      });
    }

    items.push(
      { name: this.resolveHistoryBreadcrumbLabel(language, article.ownerName), url: this.canonicalUrlService.buildAbsoluteUrl(timelinePath) },
      { name: article.title, url: canonicalUrl }
    );

    return this.buildBreadcrumbJsonLd(items);
  }

  private buildHistoryTimelineJsonLd(timeline: HistoryTimelinePageViewModel, url: string, description: string): unknown {
    const canonicalUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(url);

    return {
      '@context': 'https://schema.org',
      '@type': 'ItemList',
      name: timeline.title,
      description,
      url: canonicalUrl,
      numberOfItems: timeline.events.length,
      itemListElement: timeline.events.map((event, index) => ({
        '@type': 'ListItem',
        position: index + 1,
        name: event.title,
        description: event.summary || event.eventTypeLabel,
        url: event.articleLink
          ? this.canonicalUrlService.buildAbsoluteUrl(event.articleLink.join('/'))
          : canonicalUrl
      }))
    };
  }

  private buildHistoryArticleJsonLd(article: HistoryArticlePageViewModel, url: string, description: string): unknown {
    const canonicalUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(url);
    const articleJsonLd: Record<string, unknown> = {
      '@context': 'https://schema.org',
      '@type': 'Article',
      headline: article.title,
      description,
      url: canonicalUrl,
      datePublished: this.resolveHistoryEventDatePublished(article),
      dateModified: article.event.updatedAtUtc,
      about: article.ownerName
    };

    const imageUrl: string | null = this.resolveImageIdAbsoluteUrl(article.mainImageId);
    if (imageUrl) {
      articleJsonLd['image'] = imageUrl;
    }

    return articleJsonLd;
  }

  private resolveHistoryEventDatePublished(article: HistoryArticlePageViewModel): string {
    const month: number = article.event.month ?? 1;
    const day: number = article.event.day ?? 1;
    return new Date(Date.UTC(article.event.year, month - 1, day)).toISOString();
  }

  private resolveHistoryBreadcrumbLabel(language: string, ownerName: string): string {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: HistorySeoCopy = HISTORY_SEO_COPY[normalizedLanguage] ?? HISTORY_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    return copy.breadcrumbLabel(ownerName);
  }

  private resolveParkImagesBreadcrumbLabel(language: string, parkLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Images de ${parkLabel}`,
      en: `${parkLabel} images`,
      es: `Imágenes de ${parkLabel}`,
      de: `Bilder von ${parkLabel}`,
      it: `Immagini di ${parkLabel}`,
      nl: `Afbeeldingen van ${parkLabel}`,
      pl: `Zdjęcia ${parkLabel}`,
      pt: `Imagens de ${parkLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkItemImagesBreadcrumbLabel(language: string, itemLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Images de ${itemLabel}`,
      en: `${itemLabel} images`,
      es: `Imágenes de ${itemLabel}`,
      de: `Bilder von ${itemLabel}`,
      it: `Immagini di ${itemLabel}`,
      nl: `Afbeeldingen van ${itemLabel}`,
      pl: `Zdjęcia ${itemLabel}`,
      pt: `Imagens de ${itemLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkVideosBreadcrumbLabel(language: string, parkLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Vidéos de ${parkLabel}`,
      en: `${parkLabel} videos`,
      es: `Vídeos de ${parkLabel}`,
      de: `Videos von ${parkLabel}`,
      it: `Video di ${parkLabel}`,
      nl: `Video's van ${parkLabel}`,
      pl: `Filmy z ${parkLabel}`,
      pt: `Vídeos de ${parkLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkMapBreadcrumbLabel(language: string, parkLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Carte de ${parkLabel}`,
      en: `${parkLabel} map`,
      es: `Mapa de ${parkLabel}`,
      de: `Karte von ${parkLabel}`,
      it: `Mappa di ${parkLabel}`,
      nl: `Kaart van ${parkLabel}`,
      pl: `Mapa ${parkLabel}`,
      pt: `Mapa de ${parkLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkItemVideosBreadcrumbLabel(language: string, itemLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Vidéos de ${itemLabel}`,
      en: `${itemLabel} videos`,
      es: `Vídeos de ${itemLabel}`,
      de: `Videos von ${itemLabel}`,
      it: `Video di ${itemLabel}`,
      nl: `Video's van ${itemLabel}`,
      pl: `Filmy z ${itemLabel}`,
      pt: `Vídeos de ${itemLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkItemsBreadcrumbLabel(language: string, parkLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Lieux de ${parkLabel}`,
      en: `Places at ${parkLabel}`,
      es: `Lugares de ${parkLabel}`,
      de: `Orte in ${parkLabel}`,
      it: `Luoghi di ${parkLabel}`,
      nl: `Plekken in ${parkLabel}`,
      pl: `Miejsca w ${parkLabel}`,
      pt: `Locais de ${parkLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkWeatherBreadcrumbLabel(language: string, parkLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Météo de ${parkLabel}`,
      en: `Weather for ${parkLabel}`,
      es: `Tiempo de ${parkLabel}`,
      de: `Wetter für ${parkLabel}`,
      it: `Meteo di ${parkLabel}`,
      nl: `Weer voor ${parkLabel}`,
      pl: `Pogoda dla ${parkLabel}`,
      pt: `Meteorologia de ${parkLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkOpeningHoursBreadcrumbLabel(language: string, parkLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Dates et horaires de ${parkLabel}`,
      en: `Opening hours for ${parkLabel}`,
      es: `Fechas y horarios de ${parkLabel}`,
      de: `Öffnungszeiten von ${parkLabel}`,
      it: `Date e orari di ${parkLabel}`,
      nl: `Datums en openingstijden van ${parkLabel}`,
      pl: `Godziny otwarcia ${parkLabel}`,
      pt: `Datas e horários de ${parkLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParkZonesBreadcrumbLabel(language: string, parkLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Zones de ${parkLabel}`,
      en: `${parkLabel} zones`,
      es: `Zonas de ${parkLabel}`,
      de: `Bereiche von ${parkLabel}`,
      it: `Zone di ${parkLabel}`,
      nl: `Zones van ${parkLabel}`,
      pl: `Strefy ${parkLabel}`,
      pt: `Zonas de ${parkLabel}`
    };

    return labels[language] ?? labels['en'];
  }

  private buildParkVideoJsonLd(video: VideoDto, park: Park, url: string, videoTitle: string, description: string): unknown[] {
    const jsonLd: unknown[] = [
      this.buildParkVideoBreadcrumbJsonLd(park, url, videoTitle)
    ];
    const videoObject: Record<string, unknown> | null = this.buildVideoObjectJsonLd(video, url, videoTitle, description);

    if (videoObject) {
      jsonLd.push(videoObject);
    }

    return jsonLd;
  }

  private buildParkItemVideoJsonLd(video: VideoDto, item: ParkItem, park: Park, url: string, videoTitle: string, description: string): unknown[] {
    const jsonLd: unknown[] = [
      this.buildParkItemVideoBreadcrumbJsonLd(item, park, url, videoTitle)
    ];
    const videoObject: Record<string, unknown> | null = this.buildVideoObjectJsonLd(video, url, videoTitle, description);

    if (videoObject) {
      jsonLd.push(videoObject);
    }

    return jsonLd;
  }

  private buildVideoObjectJsonLd(video: VideoDto, url: string, videoTitle: string, description: string): Record<string, unknown> | null {
    const thumbnailUrl: string | null = this.resolveVideoThumbnailAbsoluteUrl(video);
    const uploadDate: string | null = this.resolveVideoUploadDate(video);

    if (!thumbnailUrl || !uploadDate) {
      return null;
    }

    const canonicalUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(url);
    const videoObject: Record<string, unknown> = {
      '@context': 'https://schema.org',
      '@type': 'VideoObject',
      name: videoTitle,
      description: truncateSeoText(description, 300),
      thumbnailUrl: [thumbnailUrl],
      uploadDate,
      url: canonicalUrl
    };

    const duration: string | null = this.formatVideoDuration(video.durationSeconds);
    const embedUrl: string | null = this.normalizeHttpsUrl(video.embedUrl);
    const contentUrl: string | null = this.normalizeHttpsUrl(video.canonicalUrl) ?? this.normalizeHttpsUrl(video.originalUrl);
    const viewCount: number | null = this.resolveVideoViewCount(video);

    if (duration) {
      videoObject['duration'] = duration;
    }

    if (embedUrl) {
      videoObject['embedUrl'] = embedUrl;
    }

    if (contentUrl) {
      videoObject['contentUrl'] = contentUrl;
    }

    if (viewCount !== null) {
      videoObject['interactionStatistic'] = {
        '@type': 'InteractionCounter',
        interactionType: { '@type': 'WatchAction' },
        userInteractionCount: viewCount
      };
    }

    return videoObject;
  }

  private buildParkVideoBreadcrumbJsonLd(park: Park, url: string, videoLabel: string): unknown {
    const canonicalUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(url);
    const language: string = this.resolveLanguageFromUrl(url);
    const breadcrumbUrl: string = buildCanonicalVideoRouteRedirectPath(url) ?? url;
    const segments: string[] = this.getPathSegments(breadcrumbUrl);
    const parkDetailPath: string = segments.length >= 4
      ? `/${segments.slice(0, 4).join('/')}`
      : `/${language}/parks`;
    const videosPath: string = segments.length >= 5
      ? `/${segments.slice(0, 5).join('/')}`
      : parkDetailPath;

    return this.buildBreadcrumbJsonLd([
      { name: this.resolveHomeBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/home`) },
      { name: this.resolveParksBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/parks`) },
      { name: park.name ?? 'Park', url: this.canonicalUrlService.buildAbsoluteUrl(parkDetailPath) },
      { name: this.resolveParkVideosBreadcrumbLabel(language, park.name ?? 'Park'), url: this.canonicalUrlService.buildAbsoluteUrl(videosPath) },
      { name: videoLabel, url: canonicalUrl }
    ]);
  }

  private buildParkItemVideoBreadcrumbJsonLd(item: ParkItem, park: Park, url: string, videoLabel: string): unknown {
    const canonicalUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(url);
    const language: string = this.resolveLanguageFromUrl(url);
    const breadcrumbUrl: string = buildCanonicalVideoRouteRedirectPath(url) ?? url;
    const segments: string[] = this.getPathSegments(breadcrumbUrl);
    const parkDetailPath: string = segments.length >= 4
      ? `/${segments.slice(0, 4).join('/')}`
      : `/${language}/parks`;
    const itemDetailPath: string = segments.length >= 7
      ? `/${segments.slice(0, 7).join('/')}`
      : parkDetailPath;
    const videosPath: string = segments.length >= 8
      ? `/${segments.slice(0, 8).join('/')}`
      : itemDetailPath;

    return this.buildBreadcrumbJsonLd([
      { name: this.resolveHomeBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/home`) },
      { name: this.resolveParksBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/parks`) },
      { name: park.name ?? 'Park', url: this.canonicalUrlService.buildAbsoluteUrl(parkDetailPath) },
      { name: item.name ?? 'Item', url: this.canonicalUrlService.buildAbsoluteUrl(itemDetailPath) },
      { name: this.resolveParkItemVideosBreadcrumbLabel(language, item.name ?? 'Item'), url: this.canonicalUrlService.buildAbsoluteUrl(videosPath) },
      { name: videoLabel, url: canonicalUrl }
    ]);
  }

  private buildParkReferenceJsonLd(
    reference: ParkReferenceDetailViewModel,
    url: string,
    referenceName: string,
    description: string
  ): unknown[] {
    const canonicalUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(url);
    const language: string = this.resolveLanguageFromUrl(url);
    const parentPath: string = reference.kind === 'manufacturer'
      ? `/${language}/manufacturers`
      : `/${language}/parks`;
    const referenceJsonLd: Record<string, unknown> = {
      '@context': 'https://schema.org',
      '@type': reference.kind === 'founder' ? 'Person' : 'Organization',
      name: referenceName,
      url: canonicalUrl
    };

    const normalizedDescription: string | null = this.normalizeOptionalText(description);
    if (normalizedDescription) {
      referenceJsonLd['description'] = truncateSeoText(normalizedDescription, 300);
    }

    const legalName: string | null = this.normalizeOptionalText(reference.legalName);
    if (legalName && referenceJsonLd['@type'] === 'Organization') {
      referenceJsonLd['legalName'] = legalName;
    }

    return [
      this.buildBreadcrumbJsonLd([
        { name: this.resolveHomeBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/home`) },
        { name: reference.kind === 'manufacturer' ? this.resolveManufacturersBreadcrumbLabel(language) : this.resolveParksBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(parentPath) },
        { name: referenceName, url: canonicalUrl }
      ]),
      referenceJsonLd
    ];
  }

  private buildParkItemDetailJsonLd(detail: ParkItemDetailViewModel, url: string): unknown[] {
    const canonicalUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(url);
    const language: string = this.resolveLanguageFromUrl(url);
    const breadcrumbItems = [
      { name: this.resolveHomeBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/home`) },
      { name: this.resolveParksBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/parks`) }
    ];

    if (detail.parkName && detail.parkLink) {
      breadcrumbItems.push({ name: detail.parkName, url: this.canonicalUrlService.buildAbsoluteUrl(detail.parkLink.join('/')) });
    }

    breadcrumbItems.push({ name: detail.name, url: canonicalUrl });

    const itemJsonLd: Record<string, unknown> = {
      '@context': 'https://schema.org',
      '@type': 'TouristAttraction',
      name: detail.name,
      url: canonicalUrl
    };

    const description: string = normalizeSeoText(detail.description, '');
    if (description) {
      itemJsonLd['description'] = truncateSeoText(description, 300);
    }

    if (detail.parkName) {
      itemJsonLd['containedInPlace'] = {
        '@type': 'AmusementPark',
        name: detail.parkName
      };
    }

    if (detail.manufacturerName) {
      itemJsonLd['manufacturer'] = {
        '@type': 'Organization',
        name: detail.manufacturerName
      };
    }

    return [this.buildBreadcrumbJsonLd(breadcrumbItems), itemJsonLd];
  }

  private buildTechnicalPageBreadcrumbJsonLd(pageLabel: string, url: string): unknown {
    const canonicalUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(url);
    const language: string = this.resolveLanguageFromUrl(url);
    const technicalPath: string = `/${language}/technical`;

    return this.buildBreadcrumbJsonLd([
      { name: this.resolveHomeBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/home`) },
      { name: this.resolveTechnicalBreadcrumbLabel(language), url: this.canonicalUrlService.buildAbsoluteUrl(technicalPath) },
      { name: pageLabel, url: canonicalUrl }
    ]);
  }

  private resolveHomeBreadcrumbLabel(language: string): string {
    const labels: Record<string, string> = {
      fr: 'Accueil',
      en: 'Home',
      es: 'Inicio',
      de: 'Startseite',
      it: 'Home',
      nl: 'Startpagina',
      pl: 'Strona główna',
      pt: 'Início'
    };

    return labels[language] ?? labels['en'];
  }

  private resolveParksBreadcrumbLabel(language: string): string {
    const labels: Record<string, string> = {
      fr: 'Liste des parcs',
      en: 'Parks list',
      es: 'Lista de parques',
      de: 'Parkliste',
      it: 'Elenco dei parchi',
      nl: 'Parkenlijst',
      pl: 'Lista parków',
      pt: 'Lista de parques'
    };

    return labels[language] ?? labels['en'];
  }

  private resolveManufacturersBreadcrumbLabel(language: string): string {
    const labels: Record<string, string> = {
      fr: 'Constructeurs',
      en: 'Manufacturers',
      es: 'Fabricantes',
      de: 'Hersteller',
      it: 'Costruttori',
      nl: 'Bouwers',
      pl: 'Producenci',
      pt: 'Fabricantes'
    };

    return labels[language] ?? labels['en'];
  }

  private resolveTechnicalBreadcrumbLabel(language: string): string {
    const labels: Record<string, string> = {
      fr: 'Technique',
      en: 'Technical guides',
      es: 'Guías técnicas',
      de: 'Technik',
      it: 'Guide tecniche',
      nl: 'Techniek',
      pl: 'Technika',
      pt: 'Guias técnicos'
    };

    return labels[language] ?? labels['en'];
  }

  private resolveTechnicalPageImageId(page: TechnicalPage): string | null {
    return this.resolveTechnicalBlockImageId(page.contentBlocks);
  }

  private resolveTechnicalBlockImageId(blocks: TechnicalContentBlock[] | null | undefined): string | null {
    for (const block of blocks ?? []) {
      const imageId: string | null = this.normalizeOptionalText(block.imageId);
      if (imageId) {
        return imageId;
      }

      const nestedImageId: string | null = this.resolveTechnicalBlockImageId(block.columns);
      if (nestedImageId) {
        return nestedImageId;
      }
    }

    return null;
  }

  private resolveVideoTitle(video: VideoDto, language: string): string {
    const localizedTitle: string = resolveLocalizedText(video.titles, language, video.title || 'Video');
    return this.normalizeOptionalText(localizedTitle) ?? this.normalizeOptionalText(video.title) ?? 'Video';
  }

  private resolveVideoDescription(video: VideoDto, language: string): string | null {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const localizedDescription: string | null = this.normalizeOptionalText(stripHtml(
      video.descriptions?.find((description) => {
        return this.normalizeLanguageCode(description.languageCode) === normalizedLanguage
          && this.normalizeOptionalText(description.value) !== null;
      })?.value
    ));

    if (localizedDescription) {
      return localizedDescription;
    }

    if (normalizedLanguage !== SEO_DEFAULT_LANGUAGE) {
      return null;
    }

    const defaultLocalizedDescription: string = resolveLocalizedText(video.descriptions, SEO_DEFAULT_LANGUAGE, video.description ?? '');
    return this.normalizeOptionalText(stripHtml(defaultLocalizedDescription));
  }

  private resolveVideoThumbnailAbsoluteUrl(video: VideoDto): string | null {
    const thumbnailImageId: string | null = this.normalizeOptionalText(video.thumbnailImageId);

    if (thumbnailImageId) {
      return this.resolveImageIdAbsoluteUrl(thumbnailImageId);
    }

    return this.normalizeHttpsUrl(video.thumbnailUrl);
  }

  private resolveVideoInternalThumbnailAbsoluteUrl(video: VideoDto): string | null {
    return this.resolveImageIdAbsoluteUrl(video.thumbnailImageId);
  }

  private resolveInternalImageIdAbsoluteUrl(value: string | null | undefined): string | null {
    const normalizedValue: string | null = this.normalizeOptionalText(value);

    if (!normalizedValue) {
      return null;
    }

    return this.isAbsoluteUrl(normalizedValue) ? null : this.resolveImageIdAbsoluteUrl(normalizedValue);
  }

  private resolveImageIdAbsoluteUrl(imageId: string | null | undefined): string | null {
    const normalizedImageId: string | null = this.normalizeOptionalText(imageId);

    if (!normalizedImageId) {
      return null;
    }

    return this.buildAbsoluteAssetUrl(`${environment.imagesBaseUrl}/${encodeURIComponent(normalizedImageId)}`);
  }

  private resolveVideoUploadDate(video: VideoDto): string | null {
    const dateValue: string | null = this.normalizeOptionalText(video.publishedAtUtc)
      ?? this.normalizeOptionalText(video.createdAt);

    if (!dateValue) {
      return null;
    }

    const date: Date = new Date(dateValue);
    return Number.isNaN(date.getTime()) ? null : date.toISOString();
  }

  private formatVideoDuration(durationSeconds: number | null | undefined): string | null {
    if (durationSeconds === null || durationSeconds === undefined || !Number.isFinite(durationSeconds) || durationSeconds <= 0) {
      return null;
    }

    let remainingSeconds: number = Math.round(durationSeconds);
    const hours: number = Math.floor(remainingSeconds / 3600);
    remainingSeconds -= hours * 3600;
    const minutes: number = Math.floor(remainingSeconds / 60);
    const seconds: number = remainingSeconds - minutes * 60;
    const parts: string[] = ['PT'];

    if (hours > 0) {
      parts.push(`${hours}H`);
    }

    if (minutes > 0) {
      parts.push(`${minutes}M`);
    }

    if (seconds > 0 || parts.length === 1) {
      parts.push(`${seconds}S`);
    }

    return parts.join('');
  }

  private resolveVideoViewCount(video: VideoDto): number | null {
    const viewCount: number | null | undefined = video.externalMetadata?.providerViewCount;

    if (viewCount === null || viewCount === undefined || !Number.isFinite(viewCount) || viewCount < 0) {
      return null;
    }

    return Math.round(viewCount);
  }

  private normalizeHttpsUrl(value: string | null | undefined): string | null {
    const normalizedValue: string | null = this.normalizeOptionalText(value);

    if (!normalizedValue) {
      return null;
    }

    try {
      const parsedUrl: URL = new URL(normalizedValue);
      return parsedUrl.protocol === 'https:' ? parsedUrl.href : null;
    } catch {
      return null;
    }
  }

  private isAbsoluteUrl(value: string): boolean {
    try {
      new URL(value);
      return true;
    } catch {
      return false;
    }
  }

  private buildAbsoluteAssetUrl(pathOrUrl: string): string {
    try {
      const parsedUrl: URL = new URL(pathOrUrl);
      return parsedUrl.href;
    } catch {
      return this.canonicalUrlService.buildAbsoluteUrl(pathOrUrl);
    }
  }

  private buildBreadcrumbJsonLd(items: Array<{ name: string; url: string }>): unknown {
    return {
      '@context': 'https://schema.org',
      '@type': 'BreadcrumbList',
      itemListElement: items.map((item: { name: string; url: string }, index: number) => ({
        '@type': 'ListItem',
        position: index + 1,
        name: item.name,
        item: item.url
      }))
    };
  }

  private buildStaticRouteData(routeKey: string, language: string, url: string, robots: SeoRouteData['robots']): SeoRouteData {
    const copy: StaticSeoCopy = this.resolveStaticCopy(routeKey, language);

    return {
      title: copy.title,
      description: copy.description,
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(url),
      robots,
      alternates: this.hreflangService.buildAlternates(url),
    };
  }

  private resolveStaticCopy(routeKey: string, language: string): StaticSeoCopy {
    const localizedCopy: Record<string, StaticSeoCopy> | undefined = STATIC_SEO_COPY[language] ?? STATIC_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const fallbackCopy: StaticSeoCopy = STATIC_SEO_COPY[SEO_DEFAULT_LANGUAGE]?.['home'] ?? {
      title: SITE_NAME,
      description: DEFAULT_DESCRIPTION,
    };

    return localizedCopy?.[routeKey] ?? fallbackCopy;
  }

  private setCanonical(url: string): void {
    let linkElement: HTMLLinkElement | null = this.document.head.querySelector<HTMLLinkElement>(this.canonicalSelector);

    if (!linkElement) {
      linkElement = this.document.createElement('link');
      linkElement.setAttribute('rel', 'canonical');
      this.document.head.appendChild(linkElement);
    }

    linkElement.setAttribute('href', url);
  }

  private setAlternates(alternates: SeoAlternateLink[]): void {
    this.document.head.querySelectorAll<HTMLLinkElement>(this.managedAlternateSelector)
      .forEach((element: HTMLLinkElement): void => element.remove());

    for (const alternate of alternates) {
      const linkElement: HTMLLinkElement = this.document.createElement('link');
      linkElement.setAttribute('rel', 'alternate');
      linkElement.setAttribute('hreflang', alternate.hreflang);
      linkElement.setAttribute('href', alternate.href);
      linkElement.setAttribute('data-managed-by', 'amusementpark-seo');
      this.document.head.appendChild(linkElement);
    }
  }

  private resolveLanguageFromUrl(url: string): string {
    const firstSegment: string | undefined = this.getPathSegments(url)[0];
    return firstSegment?.trim() || SEO_DEFAULT_LANGUAGE;
  }

  private resolveStaticRouteKey(url: string): string | null {
    const segments: string[] = this.getPathSegments(url);
    const routeSegment: string = segments[1] ?? 'home';

    if (routeSegment === 'home' || routeSegment === '') {
      return 'home';
    }

    if (routeSegment === 'parks') {
      return 'parks';
    }

    if (routeSegment === 'sitemap') {
      return 'sitemap';
    }

    if (routeSegment === 'rankings') {
      return 'rankings';
    }

    if (routeSegment === 'technical') {
      return 'technical';
    }

    if (routeSegment === 'manufacturers') {
      return 'manufacturers';
    }

    if (routeSegment === 'about') {
      return 'about';
    }

    if (routeSegment === 'contact') {
      return 'contact';
    }

    if (routeSegment === 'versions') {
      return 'versions';
    }

    if (routeSegment === 'privacy') {
      return 'privacy';
    }

    if (routeSegment === 'not-found') {
      return 'notFound';
    }

    return null;
  }

  private isAdminRoute(url: string): boolean {
    return /^\/[a-z]{2}\/admin(?:\/|$)/i.test(this.normalizePath(url));
  }

  private isAccountRoute(url: string): boolean {
    return /^\/[a-z]{2}\/(?:profile|confirm-account|forgot-password|reset-password)(?:\/|$)/i.test(this.normalizePath(url));
  }

  private isFilteredPublicParkItemsRoute(url: string): boolean {
    return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/items\/?$/i.test(this.normalizePath(url))
      && this.hasQueryString(url);
  }

  private isFilteredPublicParkZonesRoute(url: string): boolean {
    return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/zones\/?$/i.test(this.normalizePath(url))
      && this.hasQueryString(url);
  }

  private isFilteredPublicParkZoneRoute(url: string): boolean {
    return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/zone\/[^/]+\/[^/]+\/?$/i.test(this.normalizePath(url))
      && this.hasQueryString(url);
  }

  private isFilteredPublicParkWeatherRoute(url: string): boolean {
    return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/weather\/?$/i.test(this.normalizePath(url))
      && this.hasQueryString(url);
  }

  private isFilteredPublicParkOpeningHoursRoute(url: string): boolean {
    return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/opening-hours\/?$/i.test(this.normalizePath(url))
      && this.hasQueryString(url);
  }

  private isFilteredPublicParkImagesRoute(url: string): boolean {
    return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/images\/?$/i.test(this.normalizePath(url))
      && this.hasQueryString(url);
  }

  private isFilteredPublicParkItemImagesRoute(url: string): boolean {
    return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/images\/?$/i.test(this.normalizePath(url))
      && this.hasQueryString(url);
  }

  private isFilteredPublicParkVideosRoute(url: string): boolean {
    return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/videos\/?$/i.test(this.normalizePath(url))
      && this.hasQueryString(url);
  }

  private isFilteredPublicParkItemVideosRoute(url: string): boolean {
    return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/videos\/?$/i.test(this.normalizePath(url))
      && this.hasQueryString(url);
  }

  private hasQueryString(url: string): boolean {
    const trimmedUrl: string = url?.trim() ?? '';

    try {
      const documentOrigin: string | undefined = this.document.location?.origin;
      const baseUrl: string = documentOrigin && documentOrigin !== 'null' ? documentOrigin : 'https://amusement-parks.fun';
      const parsedUrl: URL = new URL(trimmedUrl || '/', baseUrl);
      return parsedUrl.search.length > 0;
    } catch {
      return trimmedUrl.includes('?');
    }
  }

  private getPathSegments(url: string): string[] {
    return this.normalizePath(url)
      .split('/')
      .filter((segment: string) => !!segment);
  }

  private normalizePath(url: string): string {
    const rawUrl: string = url?.trim() ?? '';

    if (!rawUrl) {
      return '/';
    }

    try {
      const documentOrigin: string | undefined = this.document.location?.origin;
      const baseUrl: string = documentOrigin && documentOrigin !== 'null' ? documentOrigin : 'https://amusement-parks.fun';
      const parsedUrl: URL = new URL(rawUrl, baseUrl);
      const normalizedPath: string = parsedUrl.pathname.replace(/\/+/g, '/');

      return normalizedPath || '/';
    } catch {
      const withoutHash: string = rawUrl.split('#')[0] ?? '';
      const withoutQuery: string = withoutHash.split('?')[0] ?? '';
      const withLeadingSlash: string = withoutQuery.startsWith('/') ? withoutQuery : `/${withoutQuery}`;

      return withLeadingSlash.replace(/\/+/g, '/');
    }
  }
}
