import { Observable, of } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';

import { ImageDto } from '@app/models/images/image-dto';

import { ImageOwnerType } from '@app/models/images/image-owner-type';

import { ParkItemImageDto } from '@app/models/images/park-item-image-dto';

import { PagedResult } from '@shared/models/contracts';

import { ParkDetailImagesPort } from '../../park-detail-data.ports';

function createImagePage<TItem>(
  items: TItem[],
  totalItems: number = items.length,
): PagedResult<TItem> {
  return {
    items,
    pagination: {
      totalItems,
      totalPages: totalItems > 0 ? 1 : 0,
      currentPage: 1,
      itemsPerPage: 1,
    },
  };
}

export class FakeImagesPort implements ParkDetailImagesPort {
  public parkImagesResponse$: Observable<PagedResult<ImageDto>> = of(
    createImagePage<ImageDto>([]),
  );
  public logoImagesResponse$: Observable<PagedResult<ImageDto>> = of(
    createImagePage<ImageDto>([]),
  );
  public itemImagesResponse$: Observable<PagedResult<ParkItemImageDto>> = of(
    createImagePage<ParkItemImageDto>([]),
  );
  public readonly pageCalls: {
    ownerType: ImageOwnerType;
    ownerId: string;
    category: ImageCategory;
    page?: number;
    size?: number;
  }[] = [];
  public readonly itemImageCalls: {
    parkId: string;
    page?: number;
    size?: number;
  }[] = [];

  getImagesPage(
    ownerType: ImageOwnerType,
    ownerId: string,
    category: ImageCategory,
    page?: number,
    size?: number,
  ): Observable<PagedResult<ImageDto>> {
    this.pageCalls.push({ ownerType, ownerId, category, page, size });
    return category === ImageCategory.LOGO
      ? this.logoImagesResponse$
      : this.parkImagesResponse$;
  }

  getParkItemImagesByPark(
    parkId: string,
    page?: number,
    size?: number,
  ): Observable<PagedResult<ParkItemImageDto>> {
    this.itemImageCalls.push({ parkId, page, size });
    return this.itemImagesResponse$;
  }
}
