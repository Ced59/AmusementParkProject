export type SeoRobotsDirective = 'index,follow' | 'noindex,follow' | 'noindex,nofollow';

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
  imageUrl?: string;
  jsonLd?: unknown[];
}
