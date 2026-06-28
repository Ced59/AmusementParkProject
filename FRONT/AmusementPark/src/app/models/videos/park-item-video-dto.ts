import { ParkItem } from '../parks/park-item';
import { VideoDto } from './video-dto';

export interface ParkItemVideoDto {
  video: VideoDto;
  item: ParkItem;
}
