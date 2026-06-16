import { DOCUMENT } from '@angular/common';
import { Inject, Injectable } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkDetailViewModel } from '@features/public/parks/models/park-detail-view.model';
import { ParkItemDetailViewModel } from '@features/public/park-items/models/park-item-detail-view.model';
import { CanonicalUrlService } from './canonical-url.service';
import { HreflangService } from './hreflang.service';
import { JsonLdService } from './json-ld.service';
import { SeoAlternateLink, SeoRouteData } from './models/seo-route-data.model';
import { SEO_DEFAULT_LANGUAGE } from './seo-languages';
import { normalizeSeoText, truncateSeoText } from './seo-text.utils';

interface StaticSeoCopy {
  title: string;
  description: string;
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

interface ParkMapSeoCopy {
  titlePrefix: string;
  parkFallback: string;
  description: (parkName: string, locationLabel: string) => string;
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
      return `Découvrez ${countLabel} de ${parkName}${locationLabel ? ` à ${locationLabel}` : ''}.`;
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
      return `Découvrez ${countLabel} de ${itemName}${parkName ? ` à ${parkName}` : ''}${locationLabel ? ` (${locationLabel})` : ''}.`;
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


const PARK_MAP_SEO_COPY: Record<string, ParkMapSeoCopy> = {
  en: {
    titlePrefix: 'Map of',
    parkFallback: 'this park',
    description: (parkName: string, locationLabel: string): string => `Interactive map of ${parkName}${locationLabel ? ` in ${locationLabel}` : ''}.`
  },
  fr: {
    titlePrefix: 'Carte de',
    parkFallback: 'ce parc',
    description: (parkName: string, locationLabel: string): string => `Carte interactive de ${parkName}${locationLabel ? ` à ${locationLabel}` : ''}.`
  },
  es: {
    titlePrefix: 'Mapa de',
    parkFallback: 'este parque',
    description: (parkName: string, locationLabel: string): string => `Mapa interactivo de ${parkName}${locationLabel ? ` en ${locationLabel}` : ''}.`
  },
  de: {
    titlePrefix: 'Karte von',
    parkFallback: 'diesem Park',
    description: (parkName: string, locationLabel: string): string => `Interaktive Karte von ${parkName}${locationLabel ? ` in ${locationLabel}` : ''}.`
  },
  it: {
    titlePrefix: 'Mappa di',
    parkFallback: 'questo parco',
    description: (parkName: string, locationLabel: string): string => `Mappa interattiva di ${parkName}${locationLabel ? ` a ${locationLabel}` : ''}.`
  },
  nl: {
    titlePrefix: 'Kaart van',
    parkFallback: 'dit park',
    description: (parkName: string, locationLabel: string): string => `Interactieve kaart van ${parkName}${locationLabel ? ` in ${locationLabel}` : ''}.`
  },
  pl: {
    titlePrefix: 'Mapa',
    parkFallback: 'tego parku',
    description: (parkName: string, locationLabel: string): string => `Interaktywna mapa parku ${parkName}${locationLabel ? ` w ${locationLabel}` : ''}.`
  },
  pt: {
    titlePrefix: 'Mapa de',
    parkFallback: 'este parque',
    description: (parkName: string, locationLabel: string): string => `Mapa interativo de ${parkName}${locationLabel ? ` em ${locationLabel}` : ''}.`
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
    about: {
      title: 'About Amusement Parks — Project and data approach',
      description: 'Learn about the Amusement Parks project, its purpose, its public park portfolio and its careful data publication approach.',
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
    home: {
      title: 'Amusement Parks — Explorer les parcs, attractions et destinations',
      description: 'Explorez les parcs de loisirs, attractions, restaurants, hôtels et références du secteur partout dans le monde.',
    },
    parks: {
      title: 'Parcs de loisirs dans le monde — Amusement Parks',
      description: 'Parcourez les parcs visibles, parcs à thèmes, parcs aquatiques, zoos et resorts avec leurs informations publiques et leur carte.',
    },
    about: {
      title: 'À propos — Amusement Parks',
      description: 'Découvrez le projet Amusement Parks, son objectif, son portefeuille public de parcs et sa démarche de publication des données.',
    },
    privacy: {
      title: 'Politique de confidentialité — Amusement Parks',
      description: 'Consultez la manière dont Amusement Parks gère la confidentialité, les cookies, les données de connexion et le consentement analytics.',
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
    home: { title: 'Amusement Parks — Explora parques, atracciones y destinos', description: 'Explora parques de ocio, atracciones, restaurantes, hoteles y referencias del sector en todo el mundo.' },
    parks: { title: 'Parques de ocio del mundo — Amusement Parks', description: 'Consulta parques visibles, parques temáticos, acuáticos, zoológicos y resorts con información pública y mapa.' },
    about: { title: 'Acerca de Amusement Parks — Proyecto y datos', description: 'Conoce el proyecto Amusement Parks, su objetivo y su enfoque cuidadoso de publicación de datos.' },
    privacy: { title: 'Política de privacidad — Amusement Parks', description: 'Consulta cómo Amusement Parks gestiona privacidad, cookies, datos de autenticación y consentimiento analítico.' },
    notFound: { title: 'Página no encontrada — Amusement Parks', description: 'La página solicitada no existe o ya no está disponible en Amusement Parks.' },
    account: { title: 'Cuenta — Amusement Parks', description: 'Página privada de cuenta de usuario de Amusement Parks.' },
    admin: { title: 'Administración — Amusement Parks', description: 'Página privada de administración de Amusement Parks.' },
  },
  de: {
    home: { title: 'Amusement Parks — Parks, Attraktionen und Reiseziele entdecken', description: 'Entdecke Freizeitparks, Attraktionen, Restaurants, Hotels und Branchenreferenzen weltweit.' },
    parks: { title: 'Freizeitparks weltweit — Amusement Parks', description: 'Durchsuche sichtbare Freizeitparks, Themenparks, Wasserparks, Zoos und Resorts mit öffentlichen Details und Karte.' },
    about: { title: 'Über Amusement Parks — Projekt und Datenansatz', description: 'Erfahre mehr über das Projekt Amusement Parks, seinen Zweck und seine sorgfältige Veröffentlichung von Daten.' },
    privacy: { title: 'Datenschutzerklärung — Amusement Parks', description: 'Lies, wie Amusement Parks Datenschutz, Cookies, Anmeldedaten und Analytics-Zustimmung verarbeitet.' },
    notFound: { title: 'Seite nicht gefunden — Amusement Parks', description: 'Die angeforderte Seite existiert nicht oder ist auf Amusement Parks nicht mehr verfügbar.' },
    account: { title: 'Konto — Amusement Parks', description: 'Private Kontoseite für Benutzer von Amusement Parks.' },
    admin: { title: 'Administration — Amusement Parks', description: 'Private Administrationsseite von Amusement Parks.' },
  },
  it: {
    home: { title: 'Amusement Parks — Esplora parchi, attrazioni e destinazioni', description: 'Esplora parchi divertimento, attrazioni, ristoranti, hotel e riferimenti del settore in tutto il mondo.' },
    parks: { title: 'Parchi divertimento nel mondo — Amusement Parks', description: 'Sfoglia parchi visibili, parchi a tema, acquatici, zoo e resort con informazioni pubbliche e mappa.' },
    about: { title: 'Informazioni su Amusement Parks — Progetto e dati', description: 'Scopri il progetto Amusement Parks, il suo obiettivo e il suo approccio prudente alla pubblicazione dei dati.' },
    privacy: { title: 'Informativa sulla privacy — Amusement Parks', description: 'Leggi come Amusement Parks gestisce privacy, cookie, dati di autenticazione e consenso analytics.' },
    notFound: { title: 'Pagina non trovata — Amusement Parks', description: 'La pagina richiesta non esiste o non è più disponibile su Amusement Parks.' },
    account: { title: 'Account — Amusement Parks', description: 'Pagina privata dell’account utente Amusement Parks.' },
    admin: { title: 'Amministrazione — Amusement Parks', description: 'Pagina privata di amministrazione Amusement Parks.' },
  },
  pl: {
    home: { title: 'Amusement Parks — Odkrywaj parki, atrakcje i kierunki', description: 'Odkrywaj parki rozrywki, atrakcje, restauracje, hotele i referencje branżowe na całym świecie.' },
    parks: { title: 'Parki rozrywki na świecie — Amusement Parks', description: 'Przeglądaj widoczne parki rozrywki, parki tematyczne, wodne, zoo i resorty z publicznymi informacjami oraz mapą.' },
    about: { title: 'O Amusement Parks — Projekt i dane', description: 'Poznaj projekt Amusement Parks, jego cel oraz ostrożne podejście do publikacji danych.' },
    privacy: { title: 'Polityka prywatności — Amusement Parks', description: 'Sprawdź, jak Amusement Parks zarządza prywatnością, cookies, danymi logowania i zgodą analityczną.' },
    notFound: { title: 'Nie znaleziono strony — Amusement Parks', description: 'Żądana strona nie istnieje albo nie jest już dostępna w Amusement Parks.' },
    account: { title: 'Konto — Amusement Parks', description: 'Prywatna strona konta użytkownika Amusement Parks.' },
    admin: { title: 'Administracja — Amusement Parks', description: 'Prywatna strona administracyjna Amusement Parks.' },
  },
  nl: {
    home: { title: 'Amusement Parks — Ontdek parken, attracties en bestemmingen', description: 'Ontdek pretparken, attracties, restaurants, hotels en brancheverwijzingen over de hele wereld.' },
    parks: { title: 'Pretparken wereldwijd — Amusement Parks', description: 'Bekijk zichtbare pretparken, themaparken, waterparken, dierentuinen en resorts met publieke info en kaart.' },
    about: { title: 'Over Amusement Parks — Project en data-aanpak', description: 'Lees meer over het Amusement Parks-project, het doel en de zorgvuldige aanpak voor datapublicatie.' },
    privacy: { title: 'Privacybeleid — Amusement Parks', description: 'Lees hoe Amusement Parks omgaat met privacy, cookies, authenticatiegegevens en analytics-toestemming.' },
    notFound: { title: 'Pagina niet gevonden — Amusement Parks', description: 'De gevraagde pagina bestaat niet of is niet meer beschikbaar op Amusement Parks.' },
    account: { title: 'Account — Amusement Parks', description: 'Privé-accountpagina voor gebruikers van Amusement Parks.' },
    admin: { title: 'Administratie — Amusement Parks', description: 'Privé-administratiepagina van Amusement Parks.' },
  },
  pt: {
    home: { title: 'Amusement Parks — Explore parques, atrações e destinos', description: 'Explore parques de diversão, atrações, restaurantes, hotéis e referências do setor em todo o mundo.' },
    parks: { title: 'Parques de diversão no mundo — Amusement Parks', description: 'Veja parques visíveis, parques temáticos, aquáticos, zoológicos e resorts com informações públicas e mapa.' },
    about: { title: 'Sobre o Amusement Parks — Projeto e dados', description: 'Conheça o projeto Amusement Parks, seu objetivo e sua abordagem cuidadosa de publicação de dados.' },
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

    if (this.isPublicParkMapRoute(url) || this.isFilteredPublicParkItemsRoute(url) || this.isFilteredPublicParkZonesRoute(url) || this.isFilteredPublicParkZoneRoute(url) || this.isFilteredPublicParkImagesRoute(url) || this.isFilteredPublicParkItemImagesRoute(url)) {
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

  applyNotFoundSeo(language: string, url: string): void {
    if (this.isAdminRoute(url) || this.isAccountRoute(url)) {
      this.applyRouteDefaults(url);
      return;
    }

    this.apply(this.buildStaticRouteData('notFound', language, url, 'noindex,follow'));
  }

  applyParkDetailSeo(park: ParkDetailViewModel, language: string, url: string): void {
    const locationLabel: string = [park.city, park.countryName ?? park.countryCode]
      .filter((value: string | null | undefined): value is string => !!value)
      .join(', ');
    const titleSuffix: string = locationLabel ? ` — ${locationLabel}` : '';
    const descriptionFallback: string = locationLabel
      ? `Explore ${park.name} in ${locationLabel}: practical information, attractions, restaurants, hotels and park map.`
      : `Explore ${park.name}: practical information, attractions, restaurants, hotels and park map.`;

    this.apply({
      title: `${park.name}${titleSuffix} — ${SITE_NAME}`,
      description: truncateSeoText(normalizeSeoText(park.description, descriptionFallback), 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(url),
      robots: 'index,follow',
      alternates: this.hreflangService.buildAlternates(url),
      jsonLd: this.buildParkDetailJsonLd(park, url)
    });
  }

  applyParkImagesSeo(park: Park, language: string, url: string, totalImages: number = 0): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: ParkImagesSeoCopy = PARK_IMAGES_SEO_COPY[normalizedLanguage] ?? PARK_IMAGES_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const parkName: string = this.normalizeOptionalText(park.name) ?? copy.parkFallback;
    const locationLabel: string = this.buildLocalizedLocationLabel(park, normalizedLanguage);
    const titleSuffix: string = locationLabel ? ` — ${locationLabel}` : '';
    const description: string = copy.description(parkName, locationLabel, totalImages);

    this.apply({
      title: `${copy.titlePrefix} ${parkName}${titleSuffix} — ${SITE_NAME}`,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(url),
      robots: this.hasQueryString(url) || totalImages <= 0 ? 'noindex,follow' : 'index,follow',
      alternates: this.hreflangService.buildAlternates(url),
      jsonLd: [this.buildParkSubpageBreadcrumbJsonLd(park, url, this.resolveParkImagesBreadcrumbLabel(normalizedLanguage, parkName))]
    });
  }

  applyParkItemImagesSeo(item: ParkItem, park: Park, language: string, url: string, totalImages: number = 0): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: ParkItemImagesSeoCopy = PARK_ITEM_IMAGES_SEO_COPY[normalizedLanguage] ?? PARK_ITEM_IMAGES_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const itemName: string = this.normalizeOptionalText(item.name) ?? copy.itemFallback;
    const parkName: string = this.normalizeOptionalText(park.name) ?? '';
    const locationLabel: string = this.buildLocalizedLocationLabel(park, normalizedLanguage);
    const titleContext: string = [parkName, locationLabel].filter((value: string) => value.length > 0).join(' — ');
    const titleSuffix: string = titleContext ? ` — ${titleContext}` : '';
    const description: string = copy.description(itemName, parkName, locationLabel, totalImages);

    this.apply({
      title: `${copy.titlePrefix} ${itemName}${titleSuffix} — ${SITE_NAME}`,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(url),
      robots: this.hasQueryString(url) || totalImages <= 0 ? 'noindex,follow' : 'index,follow',
      alternates: this.hreflangService.buildAlternates(url),
      jsonLd: [this.buildParkItemImagesBreadcrumbJsonLd(item, park, url, this.resolveParkItemImagesBreadcrumbLabel(normalizedLanguage, itemName))]
    });
  }

  applyParkMapSeo(park: Park, language: string, url: string): void {
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const copy: ParkMapSeoCopy = PARK_MAP_SEO_COPY[normalizedLanguage] ?? PARK_MAP_SEO_COPY[SEO_DEFAULT_LANGUAGE];
    const parkName: string = this.normalizeOptionalText(park.name) ?? copy.parkFallback;
    const locationLabel: string = this.buildLocalizedLocationLabel(park, normalizedLanguage);
    const titleSuffix: string = locationLabel ? ` — ${locationLabel}` : '';
    const description: string = copy.description(parkName, locationLabel);

    this.apply({
      title: `${copy.titlePrefix} ${parkName}${titleSuffix} — ${SITE_NAME}`,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(url),
      robots: 'noindex,follow',
      alternates: this.hreflangService.buildAlternates(url),
      jsonLd: [this.buildParkSubpageBreadcrumbJsonLd(park, url, this.resolveParkMapBreadcrumbLabel(normalizedLanguage, parkName))]
    });
  }

  applyParkItemsSeo(parkName: string, language: string, url: string): void {
    const normalizedParkName: string = this.normalizeOptionalText(parkName) ?? 'Park';
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const title: string = `${normalizedParkName} attractions and places — ${SITE_NAME}`;
    const description: string = `Browse attractions, restaurants, hotels, shows and practical places at ${normalizedParkName}.`;

    this.apply({
      title,
      description: truncateSeoText(description, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(url),
      robots: this.hasQueryString(url) ? 'noindex,follow' : 'index,follow',
      alternates: this.hreflangService.buildAlternates(url),
      jsonLd: [this.buildParkSubpageBreadcrumbJsonLd({ name: normalizedParkName } as Park, url, normalizedLanguage === 'fr' ? 'Elements' : 'Items')]
    });
  }

  applyParkZonesSeo(parkName: string, language: string, url: string): void {
    const normalizedParkName: string = this.normalizeOptionalText(parkName) ?? 'Park';
    const normalizedLanguage: string = this.normalizeLanguage(language);
    const pageLabel: string = normalizedLanguage === 'fr' ? 'Zones' : 'Zones';

    this.apply({
      title: `${normalizedParkName} zones — ${SITE_NAME}`,
      description: truncateSeoText(`Browse the themed zones and areas of ${normalizedParkName}, then explore their attractions, restaurants and places.`, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(url),
      robots: this.hasQueryString(url) ? 'noindex,follow' : 'index,follow',
      alternates: this.hreflangService.buildAlternates(url),
      jsonLd: [this.buildParkSubpageBreadcrumbJsonLd({ name: normalizedParkName } as Park, url, pageLabel)]
    });
  }

  applyParkZoneSeo(parkName: string, zoneName: string, language: string, url: string): void {
    const normalizedParkName: string = this.normalizeOptionalText(parkName) ?? 'Park';
    const normalizedZoneName: string = this.normalizeOptionalText(zoneName) ?? 'Zone';

    this.apply({
      title: `${normalizedZoneName} at ${normalizedParkName} — ${SITE_NAME}`,
      description: truncateSeoText(`Explore ${normalizedZoneName} at ${normalizedParkName}: attractions, restaurants, places and map markers in this park zone.`, 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(url),
      robots: this.hasQueryString(url) ? 'noindex,follow' : 'index,follow',
      alternates: this.hreflangService.buildAlternates(url),
      jsonLd: [this.buildParkSubpageBreadcrumbJsonLd({ name: normalizedParkName } as Park, url, normalizedZoneName)]
    });
  }

  applyParkItemDetailSeo(detail: ParkItemDetailViewModel, language: string, url: string): void {
    const parkLabel: string = detail.parkName ? ` at ${detail.parkName}` : '';
    const title: string = `${detail.name}${parkLabel} — ${SITE_NAME}`;
    const descriptionFallback: string = `${detail.name}${parkLabel}: category, type, practical details, photos and map information.`;

    this.apply({
      title,
      description: truncateSeoText(normalizeSeoText(detail.description, descriptionFallback), 160),
      canonicalUrl: this.canonicalUrlService.buildCanonicalFromCurrentUrl(url),
      robots: 'index,follow',
      alternates: this.hreflangService.buildAlternates(url),
      jsonLd: this.buildParkItemDetailJsonLd(detail, url)
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
    const socialImageUrl: string = data.imageUrl ?? this.canonicalUrlService.buildAbsoluteUrl(DEFAULT_SOCIAL_IMAGE_PATH);
    const locale: string = this.resolveOpenGraphLocale(data.canonicalUrl);

    this.meta.updateTag({ property: 'og:site_name', content: SITE_NAME });
    this.meta.updateTag({ property: 'og:title', content: data.title });
    this.meta.updateTag({ property: 'og:description', content: data.description });
    this.meta.updateTag({ property: 'og:url', content: data.canonicalUrl });
    this.meta.updateTag({ property: 'og:type', content: 'website' });
    this.meta.updateTag({ property: 'og:locale', content: locale });
    this.meta.updateTag({ property: 'og:image', content: socialImageUrl });
    this.meta.updateTag({ name: 'twitter:card', content: 'summary_large_image' });
    this.meta.updateTag({ name: 'twitter:title', content: data.title });
    this.meta.updateTag({ name: 'twitter:description', content: data.description });
    this.meta.updateTag({ name: 'twitter:image', content: socialImageUrl });
    this.setCanonical(data.canonicalUrl);
    this.setAlternates(data.alternates);
    this.jsonLdService.setJsonLd(data.jsonLd ?? []);
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

  private buildParkDetailJsonLd(park: ParkDetailViewModel, url: string): unknown[] {
    const canonicalUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(url);
    const jsonLd: unknown[] = [this.buildBreadcrumbJsonLd([
      { name: 'Home', url: this.canonicalUrlService.buildAbsoluteUrl(`/${this.resolveLanguageFromUrl(url)}/home`) },
      { name: 'Parks', url: this.canonicalUrlService.buildAbsoluteUrl(`/${this.resolveLanguageFromUrl(url)}/parks`) },
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
      { name: 'Home', url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/home`) },
      { name: 'Parks', url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/parks`) },
      { name: park.name ?? 'Park', url: this.canonicalUrlService.buildAbsoluteUrl(parkDetailPath) },
      { name: pageLabel, url: canonicalUrl }
    ]);
  }

  private buildParkItemImagesBreadcrumbJsonLd(item: ParkItem, park: Park, url: string, pageLabel: string): unknown {
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
      { name: 'Home', url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/home`) },
      { name: 'Parks', url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/parks`) },
      { name: park.name ?? 'Park', url: this.canonicalUrlService.buildAbsoluteUrl(parkDetailPath) },
      { name: item.name ?? 'Item', url: this.canonicalUrlService.buildAbsoluteUrl(itemDetailPath) },
      { name: pageLabel, url: canonicalUrl }
    ]);
  }

  private resolveParkImagesBreadcrumbLabel(language: string, parkLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Images ${parkLabel}`,
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
      fr: `Images ${itemLabel}`,
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

  private resolveParkMapBreadcrumbLabel(language: string, parkLabel: string): string {
    const labels: Record<string, string> = {
      fr: `Carte ${parkLabel}`,
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

  private buildParkItemDetailJsonLd(detail: ParkItemDetailViewModel, url: string): unknown[] {
    const canonicalUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(url);
    const language: string = this.resolveLanguageFromUrl(url);
    const breadcrumbItems = [
      { name: 'Home', url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/home`) },
      { name: 'Parks', url: this.canonicalUrlService.buildAbsoluteUrl(`/${language}/parks`) }
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

    if (routeSegment === 'about') {
      return 'about';
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

  private isPublicParkMapRoute(url: string): boolean {
    return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/map\/?$/i.test(this.normalizePath(url));
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

  private isFilteredPublicParkImagesRoute(url: string): boolean {
    return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/images\/?$/i.test(this.normalizePath(url))
      && this.hasQueryString(url);
  }

  private isFilteredPublicParkItemImagesRoute(url: string): boolean {
    return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/images\/?$/i.test(this.normalizePath(url))
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
