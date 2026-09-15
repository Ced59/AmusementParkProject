import type { ParkItemCategory } from '@app/models/parks/park-item-category';
import { resolveParkItemSchemaType } from './park-item-schema-type';

describe('park item schema type', () => {
  it.each([
    ['Attraction', 'TouristAttraction'],
    ['Restaurant', 'FoodEstablishment'],
    ['Hotel', 'LodgingBusiness'],
    ['Shop', 'Store'],
    ['Service', 'Place'],
    ['Animal', 'Thing'],
    ['Show', 'Thing'],
    ['Transport', 'Thing'],
    ['Other', 'Thing']
  ] as const)('describes category %s as %s', (category, expected) => {
    expect(resolveParkItemSchemaType(category, false)).toBe(expected);
  });

  it.each(['Attraction', 'Restaurant', 'Hotel', 'Shop', 'Service'] as const)(
    'does not describe a conceptual %s as an established place', category => {
      expect(resolveParkItemSchemaType(category, true)).toBe('Thing');
    }
  );

  it.each([null, undefined, 'FutureCategory' as ParkItemCategory])('keeps missing or unknown category %s generic', category => {
    expect(resolveParkItemSchemaType(category, false)).toBe('Thing');
  });
});
