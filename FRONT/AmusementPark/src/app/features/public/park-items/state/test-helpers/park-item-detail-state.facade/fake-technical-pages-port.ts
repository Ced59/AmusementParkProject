import { Observable, of } from 'rxjs';

import { TechnicalPage } from '@app/models/technical-pages/technical-page';

import { ParkItemDetailTechnicalPagesPort } from '../../park-item-detail-data.ports';

function createTechnicalPage(): TechnicalPage {
  return {
    id: 'technical-lap-bar',
    categoryKey: 'restraint',
    categoryNames: [
      { languageCode: 'fr', value: 'Retenues' },
      { languageCode: 'en', value: 'Restraints' },
    ],
    slug: 'lap-bar',
    titles: [
      { languageCode: 'fr', value: 'Lap bar' },
      { languageCode: 'en', value: 'Lap bar' },
    ],
    summaries: [
      { languageCode: 'fr', value: 'Explication technique de la lap bar.' },
      { languageCode: 'en', value: 'Technical explanation of the lap bar.' },
    ],
    aliases: [
      {
        categoryKey: 'restraint',
        labels: [
          { languageCode: 'fr', value: 'Lap bar' },
          { languageCode: 'en', value: 'Lap bar' },
        ],
      },
    ],
    contentBlocks: [],
    sortOrder: 0,
    isVisible: true,
    adminReviewStatus: 'Validated',
    updatedAtUtc: '2026-01-01T00:00:00Z',
  };
}

export class FakeTechnicalPagesPort implements ParkItemDetailTechnicalPagesPort {
  public response$: Observable<TechnicalPage[]> = of([createTechnicalPage()]);
  public callCount = 0;

  getPublicLinkIndex(): Observable<TechnicalPage[]> {
    this.callCount += 1;
    return this.response$;
  }
}
