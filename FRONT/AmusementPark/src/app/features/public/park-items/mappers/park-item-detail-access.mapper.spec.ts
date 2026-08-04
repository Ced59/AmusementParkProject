import { ParkItem } from '@app/models/parks/park-item';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';

import { buildAccessConditions } from './park-item-detail-access.mapper';

describe('park item detail access mapper', () => {
  const measurementConversionService: MeasurementConversionService = new MeasurementConversionService();

  it('falls back to the localized predefined type when its editorial label is not translated', () => {
    const item: ParkItem = createItem('PregnancyRestriction');
    const condition = buildAccessConditions(item, 'de', 'Metric', measurementConversionService)[0];

    expect(condition.title).toBeNull();
    expect(condition.titleKey).toBe('parkItems.accessConditionTypes.pregnancyRestriction');
  });

  it('falls back to the localized custom type instead of leaking an English custom label', () => {
    const item: ParkItem = createItem('Custom');
    const condition = buildAccessConditions(item, 'de', 'Metric', measurementConversionService)[0];

    expect(condition.title).toBeNull();
    expect(condition.titleKey).toBe('parkItems.accessConditionTypes.custom');
  });

  function createItem(type: 'PregnancyRestriction' | 'Custom'): ParkItem {
    return {
      parkId: 'park-1',
      name: 'Mine Train Ulven',
      category: 'Attraction',
      type: 'RollerCoaster',
      latitude: null,
      longitude: null,
      attractionDetails: {
        accessConditions: [
          {
            type,
            label: [{ languageCode: 'en', value: 'Not recommended during pregnancy' }],
            displayOrder: 1
          }
        ]
      }
    };
  }
});
