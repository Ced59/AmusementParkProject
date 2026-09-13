import { Observable, of } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';

import { ImageDto } from '@app/models/images/image-dto';

import { ImageOwnerType } from '@app/models/images/image-owner-type';

import { ParkItemDetailImagesPort } from '../../park-item-detail-data.ports';

export class FakeImagesPort implements ParkItemDetailImagesPort {
  public photosResponse$: Observable<ImageDto[]> = of([]);
  public readonly imageCalls: {
    ownerType: ImageOwnerType;
    ownerId: string;
    category: ImageCategory;
    page?: number;
    size?: number;
  }[] = [];

  getImages(
    ownerType: ImageOwnerType,
    ownerId: string,
    category: ImageCategory,
    page?: number,
    size?: number,
  ): Observable<ImageDto[]> {
    this.imageCalls.push({ ownerType, ownerId, category, page, size });
    return this.photosResponse$;
  }
}
