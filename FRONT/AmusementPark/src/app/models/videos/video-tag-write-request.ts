import { LocalizedItemDto } from '../shared/localized-item-dto';

export interface CreateVideoTagRequest {
  slug: string;
  labels: LocalizedItemDto<string>[];
  descriptions: LocalizedItemDto<string>[];
}

export interface UpdateVideoTagRequest extends CreateVideoTagRequest {
  isActive: boolean;
}
