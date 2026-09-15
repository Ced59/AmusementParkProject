export interface PaginationLabels {
  readonly first: string;
  readonly previous: string;
  readonly next: string;
  readonly last: string;
  readonly page: string;
}

const LABELS: Readonly<Record<string, PaginationLabels>> = {
  en: { first: 'First page', previous: 'Previous page', next: 'Next page', last: 'Last page', page: 'Page' },
  fr: { first: 'Première page', previous: 'Page précédente', next: 'Page suivante', last: 'Dernière page', page: 'Page' },
  de: { first: 'Erste Seite', previous: 'Vorherige Seite', next: 'Nächste Seite', last: 'Letzte Seite', page: 'Seite' },
  nl: { first: 'Eerste pagina', previous: 'Vorige pagina', next: 'Volgende pagina', last: 'Laatste pagina', page: 'Pagina' },
  it: { first: 'Prima pagina', previous: 'Pagina precedente', next: 'Pagina successiva', last: 'Ultima pagina', page: 'Pagina' },
  es: { first: 'Primera página', previous: 'Página anterior', next: 'Página siguiente', last: 'Última página', page: 'Página' },
  pl: { first: 'Pierwsza strona', previous: 'Poprzednia strona', next: 'Następna strona', last: 'Ostatnia strona', page: 'Strona' },
  pt: { first: 'Primeira página', previous: 'Página anterior', next: 'Página seguinte', last: 'Última página', page: 'Página' }
};

export function resolvePaginationLabels(language: string): PaginationLabels {
  return LABELS[language] ?? LABELS['en'];
}
