import { ImageDto } from '@app/models/images/image-dto';
import { ParkItem } from '@app/models/parks/park-item';

export interface ParkDetailReferenceNames {
  founderName?: string | null;
  operatorName?: string | null;
  countryName?: string | null;
}

export interface ParkDetailStatsSource {
  totalItems?: number | null;
  zoneCount?: number | null;
}

export interface ParkDetailItemPhotoSource {
  item: ParkItem;
  photos: ImageDto[];
}
