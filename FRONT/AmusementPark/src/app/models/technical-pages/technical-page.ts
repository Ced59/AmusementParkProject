import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { LocalizedItem } from '@app/models/shared/localized-item';

export interface TechnicalPage {
  id?: string | null;
  categoryKey: string;
  categoryNames: LocalizedItem<string>[];
  slug: string;
  titles: LocalizedItem<string>[];
  summaries: LocalizedItem<string>[];
  aliases: TechnicalPageAlias[];
  contentBlocks: TechnicalContentBlock[];
  sortOrder: number;
  isVisible: boolean;
  adminReviewStatus: AdminReviewStatus;
  updatedAtUtc?: string | null;
}

export interface TechnicalPagesJsonUpsert {
  pages: TechnicalPage[];
}

export interface TechnicalPagesJsonUpsertResult {
  createdCount: number;
  updatedCount: number;
  pages: TechnicalPage[];
}

export interface TechnicalPageAlias {
  categoryKey: string;
  labels: LocalizedItem<string>[];
}

export interface TechnicalContentBlock {
  blockType: string;
  tone?: string | null;
  imageUrl?: string | null;
  imageId?: string | null;
  diagramKey?: string | null;
  titles?: LocalizedItem<string>[];
  bodies?: LocalizedItem<string>[];
  captions?: LocalizedItem<string>[];
  altTexts?: LocalizedItem<string>[];
  items?: TechnicalContentListItem[];
  table?: TechnicalContentTable | null;
  metrics?: TechnicalContentMetric[];
  links?: TechnicalContentLink[];
  columns?: TechnicalContentBlock[];
}

export interface TechnicalContentListItem {
  texts: LocalizedItem<string>[];
}

export interface TechnicalContentTable {
  headers: TechnicalContentTableCell[];
  rows: TechnicalContentTableRow[];
}

export interface TechnicalContentTableRow {
  cells: TechnicalContentTableCell[];
}

export interface TechnicalContentTableCell {
  texts: LocalizedItem<string>[];
}

export interface TechnicalContentMetric {
  label: LocalizedItem<string>[];
  value: LocalizedItem<string>[];
  helpText: LocalizedItem<string>[];
}

export interface TechnicalContentLink {
  url: string;
  label: LocalizedItem<string>[];
}
