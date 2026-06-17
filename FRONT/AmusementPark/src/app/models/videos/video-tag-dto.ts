import { LocalizedItemDto } from '../shared/localized-item-dto';

export interface VideoTagDto {
  id: string;
  slug: string;
  labels: LocalizedItemDto<string>[];
  descriptions: LocalizedItemDto<string>[];
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}
