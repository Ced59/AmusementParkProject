import { TechnicalPage } from '@app/models/technical-pages/technical-page';

const CATEGORY_ALIASES: Record<string, string[]> = {
  launch: ['lift', 'launch', 'propulsion'],
  material: ['material', 'track', 'rail'],
  restraint: ['restraint', 'restraints', 'lapbar', 'harness'],
  seating: ['seating', 'seat', 'vehicle']
};

export function buildTechnicalPageRouterLink(
  pages: TechnicalPage[],
  categoryKeys: string[],
  value: string | null | undefined,
  currentLanguage: string
): string[] | null {
  const normalizedValue: string = normalizeSearchToken(value);

  if (normalizedValue.length === 0 || pages.length === 0) {
    return null;
  }

  const expectedCategoryKeys: Set<string> = buildExpectedCategoryKeys(categoryKeys);
  const matchingPage: TechnicalPage | undefined = pages.find((page: TechnicalPage) =>
    page.isVisible !== false
    && hasMatchingCategory(page, expectedCategoryKeys)
    && collectPageLabels(page, expectedCategoryKeys).has(normalizedValue)
  );

  if (!matchingPage) {
    return null;
  }

  return ['/', currentLanguage || 'en', 'technical', matchingPage.slug];
}

function buildExpectedCategoryKeys(categoryKeys: string[]): Set<string> {
  const expectedCategoryKeys: Set<string> = new Set<string>();

  for (const categoryKey of categoryKeys) {
    const normalizedCategoryKey: string = normalizeCategoryKey(categoryKey);

    if (normalizedCategoryKey.length === 0) {
      continue;
    }

    expectedCategoryKeys.add(normalizedCategoryKey);

    for (const alias of CATEGORY_ALIASES[normalizedCategoryKey] ?? []) {
      expectedCategoryKeys.add(normalizeCategoryKey(alias));
    }
  }

  return expectedCategoryKeys;
}

function hasMatchingCategory(page: TechnicalPage, expectedCategoryKeys: Set<string>): boolean {
  const pageCategoryKey: string = normalizeCategoryKey(page.categoryKey);

  if (expectedCategoryKeys.has(pageCategoryKey)) {
    return true;
  }

  return (page.aliases ?? []).some((alias) => expectedCategoryKeys.has(normalizeCategoryKey(alias.categoryKey)));
}

function collectPageLabels(page: TechnicalPage, expectedCategoryKeys: Set<string>): Set<string> {
  const labels: Set<string> = new Set<string>();

  addLocalizedValues(labels, page.titles);
  addSearchValue(labels, page.slug);

  for (const alias of page.aliases ?? []) {
    if (expectedCategoryKeys.has(normalizeCategoryKey(alias.categoryKey)) || expectedCategoryKeys.has(normalizeCategoryKey(page.categoryKey))) {
      addLocalizedValues(labels, alias.labels);
    }
  }

  return labels;
}

function addLocalizedValues(labels: Set<string>, values: { value?: string | null }[] | null | undefined): void {
  for (const item of values ?? []) {
    addSearchValue(labels, item.value);
  }
}

function addSearchValue(labels: Set<string>, value: string | null | undefined): void {
  const normalizedValue: string = normalizeSearchToken(value);

  if (normalizedValue.length > 0) {
    labels.add(normalizedValue);
  }
}

function normalizeCategoryKey(value: string | null | undefined): string {
  return normalizeSearchToken(value).replace(/s$/, '');
}

function normalizeSearchToken(value: string | null | undefined): string {
  return (value ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/&/g, ' and ')
    .replace(/[^a-z0-9]+/g, ' ')
    .trim()
    .replace(/\s+/g, ' ');
}
