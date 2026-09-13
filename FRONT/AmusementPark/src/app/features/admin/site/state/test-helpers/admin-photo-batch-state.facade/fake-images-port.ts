import { Observable, of, throwError } from 'rxjs';

import { AdminImageSearchQuery } from '@app/models/images/admin-image-search-query';

import { ImageCategory } from '@app/models/images/image-category';

import { ImageDto } from '@app/models/images/image-dto';

import { ImageOwnerType } from '@app/models/images/image-owner-type';

import { ImageTagDto } from '@app/models/images/image-tag-dto';

import { ParkItemImageDto } from '@app/models/images/park-item-image-dto';

import { UploadedImage } from '@app/models/images/uploaded-image';

import { PARK_ITEM_PHOTO_CATEGORY_OPTIONS } from '@features/admin/park-items/models/admin-park-item-edit.model';

import { PARK_PHOTO_CATEGORY_OPTIONS } from '@features/admin/parks/models/admin-park-edit.model';

import { PagedResult } from '@shared/models/contracts';

import { createPagedResult } from '@shared/utils/mapping';

import { AdminPhotoBatchImagesPort } from '../../admin-photo-batch-state-data.ports';

type UpdateAdminImageRequest = Parameters<
  AdminPhotoBatchImagesPort['updateAdminImage']
>[1];

function createCategoryTags(): ImageTagDto[] {
  return [
    ...PARK_PHOTO_CATEGORY_OPTIONS.map((option) =>
      createTag(`${option.slug}-tag`, option.slug),
    ),
    ...PARK_ITEM_PHOTO_CATEGORY_OPTIONS.map((option) =>
      createTag(`${option.slug}-tag`, option.slug),
    ),
  ];
}

function createTag(id: string, slug: string): ImageTagDto {
  return {
    id,
    slug,
    labels: [],
    descriptions: [],
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-02T00:00:00Z',
  };
}

function createImage(id: string, partial: Partial<ImageDto> = {}): ImageDto {
  return {
    id,
    category: ImageCategory.PARK,
    ownerType: ImageOwnerType.PARK,
    ownerId: 'park-1',
    path: `${id}.webp`,
    description: id,
    isCurrent: false,
    isPublished: false,
    isWatermarked: true,
    width: 1200,
    height: 800,
    sizeInBytes: 2048,
    originalFileName: `${id}.jpg`,
    contentType: 'image/jpeg',
    sourceUrl: null,
    geoLocation: null,
    exifMetadata: null,
    altTexts: [],
    captions: [],
    credits: [],
    tagIds: [],
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-02T00:00:00Z',
    ...partial,
  };
}

export class FakeImagesPort implements AdminPhotoBatchImagesPort {
  public parkImagesPage$: Observable<PagedResult<ImageDto>> = of(
    createPagedResult<ImageDto>([]),
  );
  public parkItemImagesPage$: Observable<PagedResult<ParkItemImageDto>> = of(
    createPagedResult<ParkItemImageDto>([]),
  );
  public parkImagesPages: Record<number, PagedResult<ImageDto>> = {};
  public parkItemImagesPages: Record<number, PagedResult<ParkItemImageDto>> =
    {};
  public parkImageResponsesByPage: Record<
    number,
    Observable<PagedResult<ImageDto>>
  > = {};
  public parkItemImageResponsesByPage: Record<
    number,
    Observable<PagedResult<ParkItemImageDto>>
  > = {};
  public uploadResponse$: Observable<UploadedImage> = of({ id: 'uploaded-1' });
  public linkResponse$: Observable<ImageDto> = of(
    createImage('image-1', { isPublished: true }),
  );
  public tagsResponse$: Observable<ImageTagDto[]> = of(createCategoryTags());
  public deleteResponse$: Observable<boolean> = of(true);
  public currentResponse$: Observable<ImageDto> = of(
    createImage('image-1', { isCurrent: true }),
  );
  public uploadResponsesByFileName: Record<string, Observable<UploadedImage>> =
    {};
  public readonly updateErrorsById: Set<string> = new Set<string>();

  public readonly uploadCalls: File[] = [];
  public readonly updateCalls: Array<{
    id: string;
    request: UpdateAdminImageRequest;
  }> = [];
  public readonly deleteCalls: string[] = [];
  public readonly currentCalls: string[] = [];
  public readonly adminImageQueries: Partial<AdminImageSearchQuery>[] = [];
  public readonly parkItemImageCalls: Array<{
    parkId: string;
    page: number;
    size: number;
  }> = [];

  uploadImage(file: File): Observable<UploadedImage> {
    this.uploadCalls.push(file);
    return this.uploadResponsesByFileName[file.name] ?? this.uploadResponse$;
  }

  linkImage(): Observable<ImageDto> {
    return this.linkResponse$;
  }

  updateAdminImage(
    id: string,
    request: UpdateAdminImageRequest,
  ): Observable<ImageDto> {
    this.updateCalls.push({ id, request });
    if (this.updateErrorsById.has(id)) {
      return throwError(() => new Error(`Update failed for ${id}.`));
    }

    return of(
      createImage(id, {
        category: request.category ?? ImageCategory.PARK,
        ownerType: request.ownerType ?? ImageOwnerType.PARK,
        ownerId: request.ownerId ?? 'park-1',
        isCurrent: request.isCurrent ?? false,
        isPublished: request.isPublished,
        geoLocation: request.geoLocation ?? null,
        tagIds: request.tagIds ?? [],
      }),
    );
  }

  deleteImage(imageId: string): Observable<boolean> {
    this.deleteCalls.push(imageId);
    return this.deleteResponse$;
  }

  setCurrentImage(imageId: string): Observable<ImageDto> {
    this.currentCalls.push(imageId);
    return this.currentResponse$;
  }

  getAdminImages(
    query: Partial<AdminImageSearchQuery> = {},
  ): Observable<PagedResult<ImageDto>> {
    this.adminImageQueries.push(query);
    const page: number = query.page ?? 1;
    const pageResponse$: Observable<PagedResult<ImageDto>> | undefined =
      this.parkImageResponsesByPage[page];
    if (pageResponse$) {
      return pageResponse$;
    }

    const pageResponse: PagedResult<ImageDto> | undefined =
      this.parkImagesPages[page];
    return pageResponse ? of(pageResponse) : this.parkImagesPage$;
  }

  getParkItemImagesByPark(
    parkId: string,
    page: number = 1,
    size: number = 24,
  ): Observable<PagedResult<ParkItemImageDto>> {
    this.parkItemImageCalls.push({ parkId, page, size });
    const pageResponse$: Observable<PagedResult<ParkItemImageDto>> | undefined =
      this.parkItemImageResponsesByPage[page];
    if (pageResponse$) {
      return pageResponse$;
    }

    const pageResponse: PagedResult<ParkItemImageDto> | undefined =
      this.parkItemImagesPages[page];
    return pageResponse ? of(pageResponse) : this.parkItemImagesPage$;
  }

  getAdminImageTags(): Observable<ImageTagDto[]> {
    return this.tagsResponse$;
  }

  createAdminImageTag(request: { slug: string }): Observable<ImageTagDto> {
    return of(createTag(`${request.slug}-tag`, request.slug));
  }
}
