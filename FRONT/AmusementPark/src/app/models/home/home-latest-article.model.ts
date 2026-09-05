import { HistoryEntityType } from '../history/history.models';
import { LocalizedItem } from '../shared/localized-item';

export interface HomeLatestArticleModel {
  eventId: string | null;
  entityType: HistoryEntityType | string;
  parkId: string | null;
  parkName: string | null;
  parkItemId: string | null;
  parkItemName: string | null;
  slug: string | null;
  titles: LocalizedItem<string>[];
  summaries: LocalizedItem<string>[];
  mainImageId: string | null;
}
