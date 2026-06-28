import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageGeoLocation } from '@app/models/images/image-geo-location';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { ParkItemImageDto } from '@app/models/images/park-item-image-dto';
import { UploadedImage } from '@app/models/images/uploaded-image';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ApiResponse } from '@app/models/shared/api_reponse';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { AdminContextualPhotoMetadataPreview } from '@features/admin/contextual-editing/services/admin-contextual-photo-metadata-reader.service';
import { AdminContextualPhotoMetadataReaderService } from '@features/admin/contextual-editing/services/admin-contextual-photo-metadata-reader.service';
import { PARK_ITEM_PHOTO_CATEGORY_OPTIONS } from '@features/admin/park-items/models/admin-park-item-edit.model';
import { PARK_PHOTO_CATEGORY_OPTIONS } from '@features/admin/parks/models/admin-park-edit.model';
import { PagedResult } from '@shared/models/contracts';
import { createPagedResult } from '@shared/utils/mapping';
import { ImageUploadSecurityService } from '@shared/utils/security';

import {
  ADMIN_PHOTO_BATCH_IMAGES_PORT,
  ADMIN_PHOTO_BATCH_PARK_ITEMS_PORT,
  ADMIN_PHOTO_BATCH_PARKS_PORT,
  AdminPhotoBatchImagesPort,
  AdminPhotoBatchParkItemsPort,
  AdminPhotoBatchParksPort
} from './admin-photo-batch-state-data.ports';
import { AdminPhotoBatchStateFacade } from './admin-photo-batch-state.facade';

type UpdateAdminImageRequest = Parameters<AdminPhotoBatchImagesPort['updateAdminImage']>[1];

class FakeImagesPort implements AdminPhotoBatchImagesPort {
  public parkImagesPage$: Observable<PagedResult<ImageDto>> = of(createPagedResult<ImageDto>([]));
  public parkItemImagesPage$: Observable<PagedResult<ParkItemImageDto>> = of(createPagedResult<ParkItemImageDto>([]));
  public uploadResponse$: Observable<UploadedImage> = of({ id: 'uploaded-1' });
  public linkResponse$: Observable<ImageDto> = of(createImage('image-1', { isPublished: true }));
  public tagsResponse$: Observable<ImageTagDto[]> = of(createCategoryTags());

  public readonly uploadCalls: File[] = [];
  public readonly updateCalls: Array<{ id: string; request: UpdateAdminImageRequest }> = [];

  uploadImage(file: File): Observable<UploadedImage> {
    this.uploadCalls.push(file);
    return this.uploadResponse$;
  }

  linkImage(): Observable<ImageDto> {
    return this.linkResponse$;
  }

  updateAdminImage(id: string, request: UpdateAdminImageRequest): Observable<ImageDto> {
    this.updateCalls.push({ id, request });
    return of(createImage(id, {
      category: request.category ?? ImageCategory.PARK,
      ownerType: request.ownerType ?? ImageOwnerType.PARK,
      ownerId: request.ownerId ?? 'park-1',
      isCurrent: request.isCurrent ?? false,
      isPublished: request.isPublished,
      geoLocation: request.geoLocation ?? null,
      tagIds: request.tagIds ?? []
    }));
  }

  getAdminImages(): Observable<PagedResult<ImageDto>> {
    return this.parkImagesPage$;
  }

  getParkItemImagesByPark(): Observable<PagedResult<ParkItemImageDto>> {
    return this.parkItemImagesPage$;
  }

  getAdminImageTags(): Observable<ImageTagDto[]> {
    return this.tagsResponse$;
  }

  createAdminImageTag(request: { slug: string }): Observable<ImageTagDto> {
    return of(createTag(`${request.slug}-tag`, request.slug));
  }
}

class FakeParksPort implements AdminPhotoBatchParksPort {
  getParksPaginated(): Observable<ParksApiResponse> {
    return of({
      data: [{
        id: 'park-1',
        name: 'Demo Park',
        latitude: 1,
        longitude: 2,
        descriptions: [],
        isVisible: true
      }],
      pagination: createPagination(1)
    });
  }

  searchParks(): Observable<ParksApiResponse> {
    return this.getParksPaginated();
  }
}

class FakeParkItemsPort implements AdminPhotoBatchParkItemsPort {
  getParkItemsPaginated(): Observable<ApiResponse<ParkItemAdminRow>> {
    return of({
      data: [createParkItemRow('item-1', 'Demo Coaster')],
      pagination: createPagination(1)
    });
  }
}

describe('AdminPhotoBatchStateFacade', () => {
  let facade: AdminPhotoBatchStateFacade;
  let imagesPort: FakeImagesPort;
  let metadataReader: jasmine.SpyObj<AdminContextualPhotoMetadataReaderService>;
  let imageUploadSecurityService: jasmine.SpyObj<ImageUploadSecurityService>;
  let toastMessageService: jasmine.SpyObj<ToastMessageService>;

  beforeEach(() => {
    imagesPort = new FakeImagesPort();
    metadataReader = jasmine.createSpyObj<AdminContextualPhotoMetadataReaderService>('AdminContextualPhotoMetadataReaderService', ['readFile']);
    imageUploadSecurityService = jasmine.createSpyObj<ImageUploadSecurityService>('ImageUploadSecurityService', ['filterValidImageFiles']);
    toastMessageService = jasmine.createSpyObj<ToastMessageService>('ToastMessageService', ['add']);
    metadataReader.readFile.and.resolveTo(createMetadata({ latitude: 50.1, longitude: 3.2 }));
    imageUploadSecurityService.filterValidImageFiles.and.callFake((files: File[]) => files);

    TestBed.configureTestingModule({
      providers: [
        AdminPhotoBatchStateFacade,
        { provide: ADMIN_PHOTO_BATCH_IMAGES_PORT, useValue: imagesPort },
        { provide: ADMIN_PHOTO_BATCH_PARKS_PORT, useClass: FakeParksPort },
        { provide: ADMIN_PHOTO_BATCH_PARK_ITEMS_PORT, useClass: FakeParkItemsPort },
        { provide: AdminContextualPhotoMetadataReaderService, useValue: metadataReader },
        { provide: ImageUploadSecurityService, useValue: imageUploadSecurityService },
        { provide: ToastMessageService, useValue: toastMessageService },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    });

    facade = TestBed.inject(AdminPhotoBatchStateFacade);
  });

  afterEach(() => {
    facade.ngOnDestroy();
  });

  it('uploads selected files as unpublished park drafts with local GPS metadata', async () => {
    await prepareSelectedParkAsync(facade);
    facade.selectFiles(createFileInputEvent(new File(['image'], 'entrance.jpg', { type: 'image/jpeg' })));
    await flushAsyncWork();

    await facade.uploadSelectedFiles();

    expect(imagesPort.uploadCalls.length).toBe(1);
    expect(imagesPort.updateCalls.length).toBe(1);
    expect(imagesPort.updateCalls[0].request.ownerType).toBe(ImageOwnerType.PARK);
    expect(imagesPort.updateCalls[0].request.category).toBe(ImageCategory.PARK);
    expect(imagesPort.updateCalls[0].request.ownerId).toBe('park-1');
    expect(imagesPort.updateCalls[0].request.isPublished).toBeFalse();
    expect(imagesPort.updateCalls[0].request.geoLocation).toEqual({ latitude: 50.1, longitude: 3.2 });
    expect(facade.uncategorizedPhotos().map((photo) => photo.id)).toEqual(['image-1']);
  });

  it('categorizes a draft photo as a park item photo with the selected category tag', async () => {
    imagesPort.parkImagesPage$ = of(createPagedResult<ImageDto>([
      createImage('image-1', { isPublished: false, tagIds: [] })
    ]));
    await prepareSelectedParkAsync(facade);

    facade.setPhotoDraftOwnerKind('image-1', 'parkItem');
    facade.setPhotoDraftParkItemId('image-1', 'item-1');
    facade.setPhotoDraftCategorySlug('image-1', PARK_ITEM_PHOTO_CATEGORY_OPTIONS[0].slug);
    await facade.savePhotoCategorization('image-1');

    const request: UpdateAdminImageRequest = imagesPort.updateCalls[0].request;
    expect(request.ownerType).toBe(ImageOwnerType.PARK_ITEM);
    expect(request.category).toBe(ImageCategory.PARK_ITEM);
    expect(request.ownerId).toBe('item-1');
    expect(request.tagIds).toContain(`${PARK_ITEM_PHOTO_CATEGORY_OPTIONS[0].slug}-tag`);
    expect(facade.parkItemPhotos().map((photo) => photo.id)).toEqual(['image-1']);
  });

  it('toggles public visibility through image metadata updates', async () => {
    imagesPort.parkImagesPage$ = of(createPagedResult<ImageDto>([
      createImage('image-1', {
        isPublished: false,
        tagIds: [`${PARK_PHOTO_CATEGORY_OPTIONS[0].slug}-tag`]
      })
    ]));
    await prepareSelectedParkAsync(facade);

    await facade.togglePublished('image-1');

    expect(imagesPort.updateCalls[0].request.isPublished).toBeTrue();
  });

  it('keeps uncategorized draft photos hidden until they are classified', async () => {
    imagesPort.parkImagesPage$ = of(createPagedResult<ImageDto>([
      createImage('image-1', { isPublished: false, tagIds: [] })
    ]));
    await prepareSelectedParkAsync(facade);

    await facade.togglePublished('image-1');

    expect(imagesPort.updateCalls).toEqual([]);
    expect(toastMessageService.add).toHaveBeenCalledWith(
      'warn',
      'common.warning',
      'admin.images.batch.toasts.visibilityNeedsCategory'
    );
  });
});

async function prepareSelectedParkAsync(facade: AdminPhotoBatchStateFacade): Promise<void> {
  facade.loadInitialData();
  await flushAsyncWork();
  facade.selectPark('park-1');
  await flushAsyncWork();
}

function createFileInputEvent(file: File): Event {
  const input: HTMLInputElement = document.createElement('input');
  Object.defineProperty(input, 'files', {
    value: [file]
  });

  return { target: input } as unknown as Event;
}

function createCategoryTags(): ImageTagDto[] {
  return [
    ...PARK_PHOTO_CATEGORY_OPTIONS.map((option) => createTag(`${option.slug}-tag`, option.slug)),
    ...PARK_ITEM_PHOTO_CATEGORY_OPTIONS.map((option) => createTag(`${option.slug}-tag`, option.slug))
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
    updatedAt: '2026-01-02T00:00:00Z'
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
    ...partial
  };
}

function createParkItemRow(id: string, name: string): ParkItemAdminRow {
  return {
    id,
    parkId: 'park-1',
    parkName: 'Demo Park',
    name,
    category: 'Attraction',
    type: 'RollerCoaster',
    isVisible: true,
    adminReviewStatus: 'Validated'
  };
}

function createPagination(totalItems: number) {
  return {
    totalItems,
    totalPages: 1,
    currentPage: 1,
    itemsPerPage: Math.max(totalItems, 1)
  };
}

function createMetadata(geoLocation: ImageGeoLocation | null): AdminContextualPhotoMetadataPreview {
  return {
    sourceKind: 'file',
    fileName: 'entrance.jpg',
    contentType: 'image/jpeg',
    sizeInBytes: 2048,
    width: 1200,
    height: 800,
    geoLocation,
    geoStatus: geoLocation ? 'detected' : 'missing'
  };
}

function flushAsyncWork(): Promise<void> {
  return new Promise((resolve: () => void) => setTimeout(resolve, 0));
}
