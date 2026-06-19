import { ImageDto } from '@app/models/images/image-dto';
import { RatingSummary } from '@app/models/ratings/rating.models';
import { Park } from './park';

export interface ParkDetailReferenceSummary {
  founderName?: string | null;
  operatorName?: string | null;
}

export interface ParkDetailSummaryStats {
  totalItems: number;
  zoneCount: number;
  attractionCount: number;
  restaurantCount: number;
  showCount: number;
  shopCount: number;
  hotelCount: number;
  countsByCategory: Record<string, number>;
}

export interface ParkDetailSummary {
  park: Park;
  mainImage?: ImageDto | null;
  references: ParkDetailReferenceSummary;
  rating?: RatingSummary | null;
  stats: ParkDetailSummaryStats;
}
