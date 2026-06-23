import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { AdminImageBulkMetadataResult, AdminImageBulkMetadataUpdate } from '@app/models/images/admin-image-bulk-metadata-update';
import { AdminImageSearchQuery } from '@app/models/images/admin-image-search-query';
import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { createPagedResult } from '@shared/utils/mapping';
import { PagedResult } from '@shared/models/contracts';

import {
  ADMIN_SITE_STATE_IMAGES_API_SERVICE_PORT,
  AdminSiteStateImagesApiServicePort
} from './admin-site-state-data.ports';
import { AdminSiteStateFacade } from './admin-site-state.facade';

type UpdateAdminImageRequest = Parameters<AdminSiteStateImagesApiServicePort['updateAdminImage']>[1];
type CreateAdminImageTagRequest = Parameters<AdminSiteStateImagesApiServicePort['createAdminImageTag']>[0];

class FakeImagesPort implements AdminSiteStateImagesApiServicePort {
  public pageResponse$: Observable<PagedResult<ImageDto>> = of(createPagedResult<ImageDto>([
    createImage('image-1'),
    createImage('image-2', { category: ImageCategory.LOGO, isWatermarked: false })
  ]));
  public tagsResponse$: Observable<ImageTagDto[]> = of([createTag('tag-1')]);
  public updateResponse$: Observable<ImageDto> = of(createImage('image-1'));
  public watermarkResponse$: Observable<ImageDto> = of(createImage('image-1', { isWatermarked: true }));
  public bulkResponse$: Observable<AdminImageBulkMetadataResult> = of({ requestedCount: 1, updatedCount: 1 });
  public deleteResponse$: Observable<boolean> = of(true);
  public createTagResponse$: Observable<ImageTagDto> = of(createTag('created-tag'));

  public readonly queryCalls: AdminImageSearchQuery[] = [];
  public readonly updateCalls: Array<{ id: string; request: UpdateAdminImageRequest }> = [];
  public readonly watermarkCalls: string[] = [];
  public readonly bulkCalls: AdminImageBulkMetadataUpdate[] = [];
  public readonly deleteCalls: string[] = [];
  public readonly createTagCalls: CreateAdminImageTagRequest[] = [];

  getAdminImages(query: Partial<AdminImageSearchQuery> = {}): Observable<PagedResult<ImageDto>> {
    this.queryCalls.push(query as AdminImageSearchQuery);
    return this.pageResponse$;
  }

  getAdminImageTags(): Observable<ImageTagDto[]> {
    return this.tagsResponse$;
  }

  updateAdminImage(id: string, request: UpdateAdminImageRequest): Observable<ImageDto> {
    this.updateCalls.push({ id, request });
    return this.updateResponse$;
  }

  applyWatermark(imageId: string): Observable<ImageDto> {
    this.watermarkCalls.push(imageId);
    return this.watermarkResponse$;
  }

  createAdminImageTag(request: CreateAdminImageTagRequest): Observable<ImageTagDto> {
    this.createTagCalls.push(request);
    return this.createTagResponse$;
  }

  updateAdminImagesBulkMetadata(request: AdminImageBulkMetadataUpdate): Observable<AdminImageBulkMetadataResult> {
    this.bulkCalls.push(request);
    return this.bulkResponse$;
  }

  deleteImage(imageId: string): Observable<boolean> {
    this.deleteCalls.push(imageId);
    return this.deleteResponse$;
  }
}

describe('AdminSiteStateFacade', () => {
  let facade: AdminSiteStateFacade;
  let port: FakeImagesPort;

  beforeEach(() => {
    port = new FakeImagesPort();

    TestBed.configureTestingModule({
      providers: [
        AdminSiteStateFacade,
        { provide: ADMIN_SITE_STATE_IMAGES_API_SERVICE_PORT, useValue: port },
      ],
    });

    facade = TestBed.inject(AdminSiteStateFacade);
  });

  it('loads images, tags and selects the first image', () => {
    facade.reload();

    expect(facade.state().kind).toBe('ready');
    expect(facade.images().map((image: ImageDto) => image.id)).toEqual(['image-1', 'image-2']);
    expect(facade.tags().map((tag: ImageTagDto) => tag.id)).toEqual(['tag-1']);
    expect(facade.selectedImage()?.id).toBe('image-1');
  });

  it('saves selected image metadata with normalized owner and geo fields', () => {
    facade.reload();
    facade.updateSelectedImage({
      ownerType: ImageOwnerType.NONE,
      ownerId: undefined,
      geoLocation: { latitude: Number.NaN, longitude: 2 },
      tagIds: ['tag-1'],
      isPublished: false,
    });

    facade.saveSelectedImage();

    expect(port.updateCalls.length).toBe(1);
    expect(port.updateCalls[0].id).toBe('image-1');
    expect(port.updateCalls[0].request.ownerType).toBe(ImageOwnerType.NONE);
    expect(port.updateCalls[0].request.ownerId).toBeNull();
    expect(port.updateCalls[0].request.geoLocation).toBeNull();
    expect(port.updateCalls[0].request.tagIds).toEqual(['tag-1']);
    expect(port.updateCalls[0].request.isPublished).toBeFalse();
  });

  it('applies watermark only when the selected image is eligible', () => {
    facade.reload();

    facade.applyWatermarkToSelectedImage();

    expect(port.watermarkCalls).toEqual(['image-1']);

    facade.selectImage(createImage('logo-1', { category: ImageCategory.LOGO }));
    facade.applyWatermarkToSelectedImage();

    expect(port.watermarkCalls).toEqual(['image-1']);
  });

  it('creates image tags with a normalized slug', () => {
    facade.reload();

    expect(facade.createTag('  HERO-TAG  ')).toBeTrue();
    expect(facade.createTag('   ')).toBeFalse();

    expect(port.createTagCalls.length).toBe(1);
    expect(port.createTagCalls[0].slug).toBe('hero-tag');
    expect(port.createTagCalls[0].labels).toEqual([{ languageCode: 'fr', value: 'hero-tag' }]);
  });

  it('applies bulk metadata to selected images', () => {
    facade.reload();
    facade.toggleImageSelection('image-1', true);
    facade.toggleImageSelection('image-2', true);

    facade.applyBulkMetadata({ isPublished: false });

    expect(port.bulkCalls).toEqual([{ imageIds: ['image-1', 'image-2'], isPublished: false }]);
  });

  it('deletes unique image ids and reloads the page', () => {
    facade.reload();

    facade.deleteImages(['image-1', 'image-1', ' image-2 ', '']);

    expect(port.deleteCalls).toEqual(['image-1', 'image-2']);
    expect(port.queryCalls.length).toBe(2);
  });

  it('keeps current data visible when a delete action fails', () => {
    facade.reload();
    port.deleteResponse$ = throwError(() => new Error('network'));

    facade.deleteImages(['image-1']);

    expect(facade.state().kind).toBe('ready');
    expect(facade.images().map((image: ImageDto) => image.id)).toEqual(['image-1', 'image-2']);
    expect(facade.operationErrorKey()).toBe('common.errorMessage');
  });
});

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
