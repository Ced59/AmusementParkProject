import { ParkItem } from '@app/models/parks/park-item';
import { ImageDto } from './image-dto';

export interface ParkItemImageDto {
  image: ImageDto;
  item: ParkItem;
}
