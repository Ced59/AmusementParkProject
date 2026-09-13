import { Observable, of } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';

import { ImageDto } from '@app/models/images/image-dto';

import { ImageOwnerType } from '@app/models/images/image-owner-type';

import { ImageTagDto } from '@app/models/images/image-tag-dto';

import { ParkItemImageDto } from '@app/models/images/park-item-image-dto';

import { PagedResult } from '@shared/models/contracts';

import { ParkImagesImagesPort } from '../../park-images-data.ports';

function createImage(id: string): ImageDto {
  return {
    id,
    category: ImageCategory.PARK,
    ownerType: ImageOwnerType.PARK,
    ownerId: 'park-1',
    path: `parks/${id}`,
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
    exifMetadata: {
      takenOnUtc: '2024-04-02T00:00:00Z',
    },
    altTexts: [],
    captions: [],
    credits: [],
    tagIds: ['tag-map'],
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };
}

function createParkItemImage(id: string): ParkItemImageDto {
  return {
    item: {
      id: 'item-1',
      parkId: 'park-1',
      name: 'Family Ride',
      category: 'Attraction',
      type: 'FlatRide',
      latitude: null,
      longitude: null,
    },
    image: {
      ...createImage(id),
      category: ImageCategory.PARK_ITEM,
      ownerType: ImageOwnerType.PARK_ITEM,
      ownerId: 'item-1',
      tagIds: [],
      isCurrent: id === 'item-image-1',
    },
  };
}

function createImageTag(): ImageTagDto {
  return {
    id: 'tag-map',
    slug: 'park-map',
    labels: [{ languageCode: 'en', value: 'Official park map' }],
    descriptions: [],
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };
}

function createImagePage<TItem>(
  items: TItem[],
  currentPage: number,
  totalPages: number,
  totalItems: number,
): PagedResult<TItem> {
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

export class FakeImagesPort implements ParkImagesImagesPort {
  public firstPage$: Observable<PagedResult<ImageDto>> = of(
    createImagePage([createImage('image-1')], 1, 2, 2),
  );
  public nextPage$: Observable<PagedResult<ImageDto>> = of(
    createImagePage([createImage('image-2')], 2, 2, 2),
  );
  public logoPage$: Observable<PagedResult<ImageDto>> = of(
    createImagePage([], 1, 0, 0),
  );
  public itemProbePage$: Observable<PagedResult<ParkItemImageDto>> = of(
    createImagePage([], 1, 0, 0),
  );
  public firstItemPage$: Observable<PagedResult<ParkItemImageDto>> = of(
    createImagePage([createParkItemImage('item-image-1')], 1, 2, 2),
  );
  public nextItemPage$: Observable<PagedResult<ParkItemImageDto>> = of(
    createImagePage([createParkItemImage('item-image-2')], 2, 2, 2),
  );
  public tags$: Observable<ImageTagDto[]> = of([createImageTag()]);
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
  public tagCallCount: number = 0;

  getImagesPage(
    ownerType: ImageOwnerType,
    ownerId: string,
    category: ImageCategory,
    page?: number,
    size?: number,
  ): Observable<PagedResult<ImageDto>> {
    this.pageCalls.push({ ownerType, ownerId, category, page, size });
    if (category === ImageCategory.LOGO) {
      return this.logoPage$;
    }

    return page === 2 ? this.nextPage$ : this.firstPage$;
  }

  getParkItemImagesByPark(
    parkId: string,
    page?: number,
    size?: number,
  ): Observable<PagedResult<ParkItemImageDto>> {
    this.itemImageCalls.push({ parkId, page, size });
    if (size === 1) {
      return this.itemProbePage$;
    }

    return page === 2 ? this.nextItemPage$ : this.firstItemPage$;
  }

  getImageTags(): Observable<ImageTagDto[]> {
    this.tagCallCount += 1;
    return this.tags$;
  }
}
