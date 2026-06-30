import { ImageDto } from '@app/models/images/image-dto';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import {
  HistoryArticleBlock,
  HistoryArticleContent,
  HistoryEntityType,
  HistoryEvent,
  HistorySourceReference
} from '@app/models/history/history.models';

export interface HistoryTimelinePageViewModel {
  entityType: HistoryEntityType | string;
  title: string;
  subtitle: string;
  ownerName: string;
  park: Park | null;
  parkItem: ParkItem | null;
  includedParkItems: ParkItem[];
  showParkItemControls: boolean;
  events: HistoryTimelineEventViewModel[];
  yearStart: number;
  yearEnd: number;
}

export interface HistoryTimelineEventViewModel {
  id: string;
  key: string;
  title: string;
  summary: string;
  dateLabel: string;
  year: number;
  month: number | null;
  day: number | null;
  eventType: string;
  eventTypeLabel: string;
  entityType: HistoryEntityType | string;
  isMajor: boolean;
  ownerName: string;
  contextParkName: string | null;
  parkItemName: string | null;
  mainImageId: string | null;
  mainImage: ImageDto | null;
  articleLink: string[] | null;
  sourceCount: number;
  positionPercent: number;
  isFirstInYear: boolean;
}

export interface HistoryArticlePageViewModel {
  title: string;
  subtitle: string;
  summary: string;
  dateLabel: string;
  eventTypeLabel: string;
  ownerName: string;
  park: Park | null;
  parkItem: ParkItem | null;
  contextPark: Park | null;
  mainImageId: string | null;
  mainImage: ImageDto | null;
  blocks: HistoryArticleBlockViewModel[];
  sources: HistorySourceReference[];
  timelineLink: string[] | null;
  event: HistoryEvent;
  article: HistoryArticleContent;
}

export interface HistoryArticleBlockViewModel {
  id: string;
  type: string;
  sortOrder: number;
  headingLevel: number;
  text: string;
  imageId: string | null;
  imageIds: string[];
  caption: string;
  source: HistoryArticleBlock;
}
