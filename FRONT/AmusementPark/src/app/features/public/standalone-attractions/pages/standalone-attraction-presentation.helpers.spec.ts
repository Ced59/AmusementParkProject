import { StandaloneAttraction } from '@app/models/standalone-attractions/standalone-attraction';

import {
  buildStandaloneAttractionDetailRows,
  getStandaloneAttractionStatusTranslationKey,
  getStandaloneAttractionSubtypeLabel,
  getStandaloneAttractionTypeTranslationKey,
  StandaloneAttractionDetailLabels,
  StandaloneAttractionDetailRow
} from './standalone-attraction-presentation.helpers';

describe('standalone attraction presentation helpers', () => {
  const labels: StandaloneAttractionDetailLabels = {
    type: 'Type',
    subtype: 'Sous-type',
    model: 'Modèle',
    status: 'Statut',
    manufacturer: 'Constructeur',
    length: 'Longueur',
    speed: 'Vitesse',
    duration: 'Durée'
  };

  const attraction: StandaloneAttraction = {
    id: 'standalone-1',
    name: 'Luge sur Rail',
    type: 'RollerCoaster',
    subtype: 'Alpine coaster',
    attractionDetails: {
      manufacturerId: 'c51b632a-7661-45b3-a7ac-714d47512fe2',
      model: 'Alpine Coaster',
      status: 'Operating',
      lengthInMeters: 625
    },
    isVisible: true,
    adminReviewStatus: 'Validated'
  };

  it('uses public translation keys instead of raw enum values', () => {
    expect(getStandaloneAttractionTypeTranslationKey('RollerCoaster')).toBe('parkExplorer.types.rollerCoaster');
    expect(getStandaloneAttractionStatusTranslationKey('Operating')).toBe('parkItems.statuses.operating');
    expect(getStandaloneAttractionStatusTranslationKey('unexpected-value')).toBe('parkItems.statuses.unknown');
  });

  it('preserves the factual subtype instead of over-localizing a technical designation', () => {
    expect(getStandaloneAttractionSubtypeLabel(' Alpine coaster ')).toBe('Alpine coaster');
  });

  it('shows a resolved manufacturer name and never falls back to its technical id', () => {
    const resolvedRows: StandaloneAttractionDetailRow[] = buildStandaloneAttractionDetailRows(
      attraction,
      labels,
      'Wiegand',
      (value: number | null | undefined, suffix: string): string => value == null ? '' : `${value} ${suffix}`
    );
    const unresolvedRows: StandaloneAttractionDetailRow[] = buildStandaloneAttractionDetailRows(
      attraction,
      labels,
      null,
      (value: number | null | undefined, suffix: string): string => value == null ? '' : `${value} ${suffix}`
    );

    expect(resolvedRows.find((row: StandaloneAttractionDetailRow) => row.label === 'Constructeur')?.value).toBe('Wiegand');
    expect(unresolvedRows.some((row: StandaloneAttractionDetailRow) => row.value.includes('c51b632a'))).toBe(false);
    expect(unresolvedRows.some((row: StandaloneAttractionDetailRow) => row.label === 'Constructeur')).toBe(false);
  });
});
