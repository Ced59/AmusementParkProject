import type { ParkItemCategory } from '@app/models/parks/park-item-category';

export type ParkItemSchemaType = 'TouristAttraction' | 'FoodEstablishment' | 'LodgingBusiness' | 'Store' | 'Place' | 'Thing';

/** Maps the supplied category for presentation; never infers a business category from a translated label. */
export function resolveParkItemSchemaType(
  category: ParkItemCategory | null | undefined,
  isConceptualPark: boolean
): ParkItemSchemaType {
  if (isConceptualPark) {
    return 'Thing';
  }

  switch (category) {
    case 'Attraction':
      return 'TouristAttraction';
    case 'Restaurant':
      return 'FoodEstablishment';
    case 'Hotel':
      return 'LodgingBusiness';
    case 'Shop':
      return 'Store';
    case 'Service':
      return 'Place';
    default:
      // Animal, Show, Transport and Other do not establish a more specific entity type.
      return 'Thing';
  }
}
