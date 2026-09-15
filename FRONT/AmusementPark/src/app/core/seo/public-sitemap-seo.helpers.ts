interface PublicSitemapSeoCopy {
  readonly label: string;
  readonly pageLabel: string;
  readonly description: (context: string) => string;
}

const COPY: Readonly<Record<string, PublicSitemapSeoCopy>> = {
  en: { label: 'Sitemap', pageLabel: 'Page', description: context => `Explore the links for ${context} in the Amusement Parks sitemap.` },
  fr: { label: 'Plan du site', pageLabel: 'Page', description: context => `Explore les liens de la rubrique ${context} dans le plan du site Amusement Parks.` },
  es: { label: 'Mapa del sitio', pageLabel: 'Página', description: context => `Explora los enlaces de ${context} en el mapa del sitio de Amusement Parks.` },
  de: { label: 'Sitemap', pageLabel: 'Seite', description: context => `Entdecke die Links zu ${context} in der Sitemap von Amusement Parks.` },
  it: { label: 'Mappa del sito', pageLabel: 'Pagina', description: context => `Esplora i collegamenti di ${context} nella mappa del sito di Amusement Parks.` },
  nl: { label: 'Sitemap', pageLabel: 'Pagina', description: context => `Ontdek de links voor ${context} in de sitemap van Amusement Parks.` },
  pt: { label: 'Mapa do site', pageLabel: 'Página', description: context => `Explora as ligações de ${context} no mapa do site Amusement Parks.` },
  pl: { label: 'Mapa witryny', pageLabel: 'Strona', description: context => `Odkryj odnośniki do sekcji ${context} na mapie witryny Amusement Parks.` }
};

export function resolvePublicSitemapSeoCopy(language: string): PublicSitemapSeoCopy {
  return COPY[language] ?? COPY['en'];
}

/** Only for node IDs and a page already validated against a loaded sitemap branch. */
export function buildPublicSitemapCanonicalUrl(rootUrl: string, nodeIds: readonly string[], page: number = 1): string {
  // Match Angular RouterLink encoding for the node grammar; keep other query policies unchanged.
  const query: string[] = nodeIds.length > 0
    ? [`node=${encodeURIComponent(nodeIds.join('/')).replace(/%3A/gi, ':')}`]
    : [];
  if (page > 1) {
    query.push(`page=${page}`);
  }
  return `${rootUrl}${query.length > 0 ? `?${query.join('&')}` : ''}`;
}
