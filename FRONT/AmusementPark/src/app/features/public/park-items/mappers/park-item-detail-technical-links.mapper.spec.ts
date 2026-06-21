import { TechnicalPage } from '@app/models/technical-pages/technical-page';

import { buildTechnicalPageRouterLink } from './park-item-detail-technical-links.mapper';

describe('buildTechnicalPageRouterLink', () => {
  it('links a French chain lift launch value to the chain lift technical page', () => {
    const routerLink: string[] | null = buildTechnicalPageRouterLink(
      [createChainLiftPage()],
      ['launch'],
      'Lift à chaîne',
      'fr'
    );

    expect(routerLink).toEqual(['/', 'fr', 'technical', 'chain-lift']);
  });

  it('matches normalized chain lift aliases across launch wording variants', () => {
    const routerLink: string[] | null = buildTechnicalPageRouterLink(
      [createChainLiftPage()],
      ['launch'],
      'chain driven lift',
      'en'
    );

    expect(routerLink).toEqual(['/', 'en', 'technical', 'chain-lift']);
  });

  it('does not link hidden technical pages', () => {
    const routerLink: string[] | null = buildTechnicalPageRouterLink(
      [createChainLiftPage({ isVisible: false })],
      ['launch'],
      'Lift à chaîne',
      'fr'
    );

    expect(routerLink).toBeNull();
  });
});

function createChainLiftPage(overrides: Partial<TechnicalPage> = {}): TechnicalPage {
  return {
    id: 'technical-chain-lift',
    categoryKey: 'lift',
    categoryNames: [
      { languageCode: 'fr', value: 'Systèmes de lift' },
      { languageCode: 'en', value: 'Lift systems' }
    ],
    slug: 'chain-lift',
    titles: [
      { languageCode: 'fr', value: 'Lift à chaîne' },
      { languageCode: 'en', value: 'Chain lift' }
    ],
    summaries: [
      { languageCode: 'fr', value: 'Explication technique du lift à chaîne.' },
      { languageCode: 'en', value: 'Technical explanation of the chain lift.' }
    ],
    aliases: [
      {
        categoryKey: 'launch',
        labels: [
          { languageCode: 'fr', value: 'Lift à chaîne' },
          { languageCode: 'en', value: 'Chain-driven lift' }
        ]
      },
      {
        categoryKey: 'propulsion',
        labels: [
          { languageCode: 'fr', value: 'Montée par chaîne' },
          { languageCode: 'en', value: 'Lift hill' }
        ]
      }
    ],
    contentBlocks: [],
    sortOrder: 1010,
    isVisible: true,
    adminReviewStatus: 'Validated',
    ...overrides
  };
}
