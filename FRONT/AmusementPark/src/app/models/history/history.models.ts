import { ImageDto } from '@app/models/images/image-dto';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { PaginationContract } from '@shared/models/contracts';

export type HistoryEntityType = 'Park' | 'ParkItem';
export type HistoryDatePrecision = 'Year' | 'Month' | 'Day';
export type HistoryArticleBlockType = 'Paragraph' | 'Heading' | 'Image' | 'Gallery' | 'Quote' | 'FactBox' | 'SourceNote';

export interface HistorySourceReference {
  label?: string | null;
  url: string;
  accessedAt?: string | null;
}

export interface HistoryArticleBlock {
  id?: string | null;
  type: HistoryArticleBlockType | string;
  sortOrder: number;
  headingLevel?: number | null;
  texts: LocalizedItem<string>[];
  imageId?: string | null;
  imageIds: string[];
  captions: LocalizedItem<string>[];
}

export interface HistoryArticleContent {
  slug?: string | null;
  titles: LocalizedItem<string>[];
  subtitles: LocalizedItem<string>[];
  summaries: LocalizedItem<string>[];
  mainImageId?: string | null;
  blocks: HistoryArticleBlock[];
  sources: HistorySourceReference[];
  isPublished: boolean;
}

export interface HistoryEvent {
  id?: string | null;
  key: string;
  entityType: HistoryEntityType | string;
  ownerId: string;
  parkId?: string | null;
  parkItemId?: string | null;
  contextParkId?: string | null;
  year: number;
  month?: number | null;
  day?: number | null;
  datePrecision: HistoryDatePrecision | string;
  eventType: string;
  isMajor: boolean;
  isVisible: boolean;
  slug?: string | null;
  titles: LocalizedItem<string>[];
  summaries: LocalizedItem<string>[];
  mainImageId?: string | null;
  previousName?: string | null;
  newName?: string | null;
  previousLogoImageId?: string | null;
  newLogoImageId?: string | null;
  previousOperatorId?: string | null;
  newOperatorId?: string | null;
  locationLabel?: string | null;
  relatedParkIds: string[];
  relatedParkItemIds: string[];
  sources: HistorySourceReference[];
  article?: HistoryArticleContent | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface HistoryTimelineEvent {
  event: HistoryEvent;
  contextPark?: Park | null;
  parkItem?: ParkItem | null;
  mainImage?: ImageDto | null;
}

export interface HistoryTimeline {
  entityType: HistoryEntityType | string;
  park?: Park | null;
  parkItem?: ParkItem | null;
  hasParkItemTimelineEvents?: boolean;
  includedParkItems: ParkItem[];
  events: HistoryTimelineEvent[];
  pagination?: PaginationContract | null;
  pageRanges?: HistoryTimelinePageRange[];
}

export interface HistoryTimelinePageRange {
  page: number;
  startYear: number;
  endYear: number;
  eventCount: number;
}

export interface HistoryArticle {
  event: HistoryEvent;
  park?: Park | null;
  parkItem?: ParkItem | null;
  contextPark?: Park | null;
  mainImage?: ImageDto | null;
}

export interface HistoryEventWriteModel {
  id?: string | null;
  key?: string | null;
  entityType: HistoryEntityType;
  ownerId: string;
  parkId?: string | null;
  parkItemId?: string | null;
  contextParkId?: string | null;
  year: number;
  month?: number | null;
  day?: number | null;
  datePrecision?: HistoryDatePrecision | string | null;
  eventType: string;
  isMajor: boolean;
  isVisible: boolean;
  slug?: string | null;
  titles: LocalizedItem<string>[];
  summaries: LocalizedItem<string>[];
  mainImageId?: string | null;
  previousName?: string | null;
  newName?: string | null;
  previousLogoImageId?: string | null;
  newLogoImageId?: string | null;
  previousOperatorId?: string | null;
  newOperatorId?: string | null;
  locationLabel?: string | null;
  relatedParkIds: string[];
  relatedParkItemIds: string[];
  sources: HistorySourceReference[];
  article?: HistoryArticleContent | null;
}

export interface HistoryEventAdminListResponse {
  items: HistoryEvent[];
  totalCount: number;
}
