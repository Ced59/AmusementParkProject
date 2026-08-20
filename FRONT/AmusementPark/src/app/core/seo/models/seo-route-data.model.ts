export type SeoRobotsDirective = 'index,follow' | 'noindex,follow' | 'noindex,nofollow' | 'noindex,nofollow,noarchive';

export interface SeoAlternateLink {
  hreflang: string;
  href: string;
}

export interface SeoRouteData {
  title: string;
  description: string;
  canonicalUrl: string;
  robots: SeoRobotsDirective;
  alternates: SeoAlternateLink[];
  openGraphLocale?: string | null;
  openGraphType?: 'website' | 'article';
  imageUrl?: string;
  imageAlt?: string;
  jsonLd?: unknown[];
}
