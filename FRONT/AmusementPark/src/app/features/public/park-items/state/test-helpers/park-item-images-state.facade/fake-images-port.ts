import { Observable, of } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';

import { ImageDto } from '@app/models/images/image-dto';

import { ImageOwnerType } from '@app/models/images/image-owner-type';

import { ImageTagDto } from '@app/models/images/image-tag-dto';

import { PagedResult } from '@shared/models/contracts';

import { ParkItemImagesImagesPort } from '../../park-item-images-data.ports';

function createImage(id: string): ImageDto {
  return {
    id,
    category: ImageCategory.PARK_ITEM,
    ownerType: ImageOwnerType.PARK_ITEM,
    ownerId: 'item-1',
    path: `items/${id}`,
    description: `Photo ${id}`,
    isCurrent: id === 'image-1',
    isWatermarked: false,
    isPublished: true,
    width: 1200,
    height: 800,
    sizeInBytes: 1000,
    originalFileName: `${id}.jpg`,
    contentType: 'image/jpeg',
    geoLocation: null,
    altTexts: [],
    captions: [],
    credits: [],
    tagIds: [],
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };
}

function createImagePage(
  items: ImageDto[],
  currentPage: number,
  totalPages: number,
  totalItems: number,
): PagedResult<ImageDto> {
  return {
    items,
    pagination: {
      currentPage,
      totalPages,
      totalItems,
      itemsPerPage: 100,
    },
  };
}

export class FakeImagesPort implements ParkItemImagesImagesPort {
  public firstPage$: Observable<PagedResult<ImageDto>> = of(
    createImagePage([createImage('image-1')], 1, 2, 2),
  );
  public nextPage$: Observable<PagedResult<ImageDto>> = of(
    createImagePage([createImage('image-2')], 2, 2, 2),
  );
  public tags$: Observable<ImageTagDto[]> = of([]);
  public readonly pageCalls: {
    ownerType: ImageOwnerType;
    ownerId: string;
    category: ImageCategory;
    page?: number;
    size?: number;
  }[] = [];
  public tagCallCount: number = 0;

  getImagesPage(
    ownerType: ImageOwnerType,
    ownerId: string,
    category: ImageCategory,
    page?: number,
    size?: number,
  ): Observable<PagedResult<ImageDto>> {
    this.pageCalls.push({ ownerType, ownerId, category, page, size });
    return page === 2 ? this.nextPage$ : this.firstPage$;
  }

  getImageTags(): Observable<ImageTagDto[]> {
    this.tagCallCount += 1;
    return this.tags$;
  }
}
