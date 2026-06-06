import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { mapParkItemToCardViewModel } from './park-item-card.mapper';
import { ParkItemCardViewModel } from '../models/park-item-card.model';

export function buildRelatedItems(
  item: ParkItem,
  park: Park | null,
  relatedItems: ParkItem[],
  currentLanguage: string,
  zoneName: string | null
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
      candidate.zoneId === item.zoneId ? zoneName : null
    ));
}
