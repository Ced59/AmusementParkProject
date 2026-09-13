import { Observable, of } from 'rxjs';

import { AdminImageBulkMetadataResult, AdminImageBulkMetadataUpdate } from '@app/models/images/admin-image-bulk-metadata-update';

import { AdminImageSearchQuery } from '@app/models/images/admin-image-search-query';

import { ImageCategory } from '@app/models/images/image-category';

import { ImageDto } from '@app/models/images/image-dto';

import { ImageOwnerType } from '@app/models/images/image-owner-type';

import { ImageTagDto } from '@app/models/images/image-tag-dto';

import { createPagedResult } from '@shared/utils/mapping';

import { PagedResult } from '@shared/models/contracts';

import { AdminSiteStateImagesApiServicePort } from '../../admin-site-state-data.ports';

type UpdateAdminImageRequest = Parameters<
  AdminSiteStateImagesApiServicePort['updateAdminImage']
>[1];

type CreateAdminImageTagRequest = Parameters<
  AdminSiteStateImagesApiServicePort['createAdminImageTag']
>[0];

function createImage(id: string, partial: Partial<ImageDto> = {}): ImageDto {
  return {
    id,
    category: ImageCategory.PARK,
    ownerType: ImageOwnerType.PARK,
    ownerId: 'park-1',
    path: `${id}.webp`,
    description: id,
    isCurrent: false,
    isPublished: true,
    isWatermarked: false,
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

function createTag(id: string): ImageTagDto {
  return {
    id,
    slug: id,
    labels: [],
    descriptions: [],
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-02T00:00:00Z',
  };
}

export class FakeImagesPort implements AdminSiteStateImagesApiServicePort {
  public pageResponse$: Observable<PagedResult<ImageDto>> = of(
    createPagedResult<ImageDto>([
      createImage('image-1'),
      createImage('image-2', {
        category: ImageCategory.LOGO,
        isWatermarked: false,
      }),
    ]),
  );
  public tagsResponse$: Observable<ImageTagDto[]> = of([createTag('tag-1')]);
  public updateResponse$: Observable<ImageDto> = of(createImage('image-1'));
  public watermarkResponse$: Observable<ImageDto> = of(
    createImage('image-1', { isWatermarked: true }),
  );
  public bulkResponse$: Observable<AdminImageBulkMetadataResult> = of({
    requestedCount: 1,
    updatedCount: 1,
  });
  public deleteResponse$: Observable<boolean> = of(true);
  public createTagResponse$: Observable<ImageTagDto> = of(
    createTag('created-tag'),
  );

  public readonly queryCalls: AdminImageSearchQuery[] = [];
  public readonly updateCalls: Array<{
    id: string;
    request: UpdateAdminImageRequest;
  }> = [];
  public readonly watermarkCalls: string[] = [];
  public readonly bulkCalls: AdminImageBulkMetadataUpdate[] = [];
  public readonly deleteCalls: string[] = [];
  public readonly createTagCalls: CreateAdminImageTagRequest[] = [];

  getAdminImages(
    query: Partial<AdminImageSearchQuery> = {},
  ): Observable<PagedResult<ImageDto>> {
    this.queryCalls.push(query as AdminImageSearchQuery);
    return this.pageResponse$;
  }

  getAdminImageTags(): Observable<ImageTagDto[]> {
    return this.tagsResponse$;
  }

  updateAdminImage(
    id: string,
    request: UpdateAdminImageRequest,
  ): Observable<ImageDto> {
    this.updateCalls.push({ id, request });
    return this.updateResponse$;
  }

  applyWatermark(imageId: string): Observable<ImageDto> {
    this.watermarkCalls.push(imageId);
    return this.watermarkResponse$;
  }

  createAdminImageTag(
    request: CreateAdminImageTagRequest,
  ): Observable<ImageTagDto> {
    this.createTagCalls.push(request);
    return this.createTagResponse$;
  }

  updateAdminImagesBulkMetadata(
    request: AdminImageBulkMetadataUpdate,
  ): Observable<AdminImageBulkMetadataResult> {
    this.bulkCalls.push(request);
    return this.bulkResponse$;
  }

  deleteImage(imageId: string): Observable<boolean> {
    this.deleteCalls.push(imageId);
    return this.deleteResponse$;
  }
}
