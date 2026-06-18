import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { MeasurementSystem, DEFAULT_MEASUREMENT_SYSTEM } from '@shared/models/measurements/measurement-system.model';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { mapParkItemToCardViewModel } from './park-item-card.mapper';
import { ParkItemCardViewModel } from '../models/park-item-card.model';

export function buildRelatedItems(
  item: ParkItem,
  park: Park | null,
  relatedItems: ParkItem[],
  currentLanguage: string,
  zoneName: string | null,
  textTruncator: NaturalTextTruncatorService | null = null,
  measurementSystem: MeasurementSystem = DEFAULT_MEASUREMENT_SYSTEM,
  measurementConversionService: MeasurementConversionService = new MeasurementConversionService()
): ParkItemCardViewModel[] {
  return relatedItems
    .filter((candidate: ParkItem) => candidate.id !== item.id)
    .filter((candidate: ParkItem) => candidate.category === item.category || candidate.type === item.type || candidate.zoneId === item.zoneId)
    .slice(0, 3)
    .map((candidate: ParkItem) => mapParkItemToCardViewModel(
      candidate,
      park,
      currentLanguage,
      null,
      candidate.zoneId === item.zoneId ? zoneName : null,
      textTruncator,
      measurementSystem,
      measurementConversionService
    ));
}
