import type { MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';

import { AdminImageSearchQuery } from '@app/models/images/admin-image-search-query';
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
  AdminPhotoBatchParksPort,
} from './admin-photo-batch-state-data.ports';
import { AdminPhotoBatchStateFacade } from './admin-photo-batch-state.facade';

type UpdateAdminImageRequest = Parameters<
  AdminPhotoBatchImagesPort['updateAdminImage']
>[1];

class FakeImagesPort implements AdminPhotoBatchImagesPort {
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

class FakeParksPort implements AdminPhotoBatchParksPort {
  getParksPaginated(): Observable<ParksApiResponse> {
    return of({
      data: [
        {
          id: 'park-1',
          name: 'Demo Park',
          latitude: 1,
          longitude: 2,
          descriptions: [],
          isVisible: true,
        },
      ],
      pagination: createPagination(1),
    });
  }

  searchParks(): Observable<ParksApiResponse> {
    return this.getParksPaginated();
  }
}

class FakeParkItemsPort implements AdminPhotoBatchParkItemsPort {
  public responsesByPage: Record<number, ApiResponse<ParkItemAdminRow>> = {};
  public readonly calls: Array<{
    page: number;
    size: number;
    parkId: string | null | undefined;
  }> = [];

  getParkItemsPaginated(
    page: number,
    size: number,
    parkId?: string | null,
  ): Observable<ApiResponse<ParkItemAdminRow>> {
    this.calls.push({ page, size, parkId });
    return of(
      this.responsesByPage[page] ?? {
        data: [createParkItemRow('item-1', 'Demo Coaster')],
        pagination: createPagination(1),
      },
    );
  }
}

describe('AdminPhotoBatchStateFacade', () => {
  let facade: AdminPhotoBatchStateFacade;
  let imagesPort: FakeImagesPort;
  let parkItemsPort: FakeParkItemsPort;
  let metadataReader: MockedObject<AdminContextualPhotoMetadataReaderService>;
  let imageUploadSecurityService: MockedObject<ImageUploadSecurityService>;
  let toastMessageService: MockedObject<ToastMessageService>;

  beforeEach(() => {
    imagesPort = new FakeImagesPort();
    parkItemsPort = new FakeParkItemsPort();
    metadataReader = {
      readFile: vi
        .fn()
        .mockName('AdminContextualPhotoMetadataReaderService.readFile'),
    } as unknown as MockedObject<AdminContextualPhotoMetadataReaderService>;
    imageUploadSecurityService = {
      filterValidImageFiles: vi
        .fn()
        .mockName('ImageUploadSecurityService.filterValidImageFiles'),
    } as unknown as MockedObject<ImageUploadSecurityService>;
    toastMessageService = {
      add: vi.fn().mockName('ToastMessageService.add'),
    } as unknown as MockedObject<ToastMessageService>;
    metadataReader.readFile.mockResolvedValue(
      createMetadata({ latitude: 50.1, longitude: 3.2 }),
    );
    imageUploadSecurityService.filterValidImageFiles.mockImplementation(
      (files: File[]) => files,
    );

    TestBed.configureTestingModule({
      providers: [
        AdminPhotoBatchStateFacade,
        { provide: ADMIN_PHOTO_BATCH_IMAGES_PORT, useValue: imagesPort },
        { provide: ADMIN_PHOTO_BATCH_PARKS_PORT, useClass: FakeParksPort },
        { provide: ADMIN_PHOTO_BATCH_PARK_ITEMS_PORT, useValue: parkItemsPort },
        {
          provide: AdminContextualPhotoMetadataReaderService,
          useValue: metadataReader,
        },
        {
          provide: ImageUploadSecurityService,
          useValue: imageUploadSecurityService,
        },
        { provide: ToastMessageService, useValue: toastMessageService },
        {
          provide: TranslateService,
          useValue: { instant: (key: string) => key },
        },
      ],
    });

    facade = TestBed.inject(AdminPhotoBatchStateFacade);
  });

  afterEach(() => {
    facade.ngOnDestroy();
  });

  it('uploads selected files as unpublished park drafts with local GPS metadata', async () => {
    await prepareSelectedParkAsync(facade);
    facade.selectFiles(
      createFileInputEvent(
        new File(['image'], 'entrance.jpg', { type: 'image/jpeg' }),
      ),
    );
    await flushAsyncWork();
    imagesPort.parkImagesPage$ = of(
      createPagedResult<ImageDto>([
        createImage('image-1', { isPublished: false }),
      ]),
    );

    await facade.uploadSelectedFiles();

    expect(imagesPort.uploadCalls.length).toBe(1);
    expect(imagesPort.updateCalls.length).toBe(1);
    expect(imagesPort.updateCalls[0].request.ownerType).toBe(
      ImageOwnerType.PARK,
    );
    expect(imagesPort.updateCalls[0].request.category).toBe(ImageCategory.PARK);
    expect(imagesPort.updateCalls[0].request.ownerId).toBe('park-1');
    expect(imagesPort.updateCalls[0].request.isPublished).toBe(false);
    expect(imagesPort.updateCalls[0].request.geoLocation).toEqual({
      latitude: 50.1,
      longitude: 3.2,
    });
    expect(facade.uncategorizedPhotos().map((photo) => photo.id)).toEqual([
      'image-1',
    ]);
    expect(imagesPort.adminImageQueries.length).toBe(1);
  });

  it('uploads selected files one at a time', async () => {
    const firstUpload: Subject<UploadedImage> = new Subject<UploadedImage>();
    imagesPort.uploadResponsesByFileName['first.jpg'] =
      firstUpload.asObservable();
    await prepareSelectedParkAsync(facade);
    facade.selectFiles(
      createFileInputEvent(
        new File(['first'], 'first.jpg', { type: 'image/jpeg' }),
        new File(['second'], 'second.jpg', { type: 'image/jpeg' }),
      ),
    );
    await flushAsyncWork();

    const uploadPromise: Promise<void> = facade.uploadSelectedFiles();
    await flushAsyncWork();

    expect(imagesPort.uploadCalls.map((file: File) => file.name)).toEqual([
      'first.jpg',
    ]);

    firstUpload.next({ id: 'uploaded-first' });
    firstUpload.complete();
    await flushAsyncWork();

    expect(imagesPort.uploadCalls.map((file: File) => file.name)).toEqual([
      'first.jpg',
      'second.jpg',
    ]);
    await uploadPromise;
  });

  it('loads the first image pages and loads more on demand', async () => {
    parkItemsPort.responsesByPage = {
      1: {
        data: [createParkItemRow('item-1', 'Demo Coaster')],
        pagination: createPagination(2, 1, 2, 100),
      },
      2: {
        data: [createParkItemRow('item-2', 'Demo Show')],
        pagination: createPagination(2, 2, 2, 100),
      },
    };
    imagesPort.parkImagesPages = {
      1: createPagedResult<ImageDto>(
        [createImage('uncategorized-1', { isPublished: false, tagIds: [] })],
        createPagination(2, 1, 2, 100),
      ),
      2: createPagedResult<ImageDto>(
        [
          createImage('park-1-photo', {
            isPublished: false,
            tagIds: [`${PARK_PHOTO_CATEGORY_OPTIONS[0].slug}-tag`],
          }),
        ],
        createPagination(2, 2, 2, 100),
      ),
    };
    imagesPort.parkItemImagesPages = {
      1: createPagedResult<ParkItemImageDto>(
        [createParkItemImage('item-photo-1', 'item-1', 'Demo Coaster')],
        createPagination(2, 1, 2, 100),
      ),
      2: createPagedResult<ParkItemImageDto>(
        [createParkItemImage('item-photo-2', 'item-2', 'Demo Show')],
        createPagination(2, 2, 2, 100),
      ),
    };

    await prepareSelectedParkAsync(facade);

    expect(
      parkItemsPort.calls.map((call) => ({ page: call.page, size: call.size })),
    ).toEqual([
      { page: 1, size: 100 },
      { page: 2, size: 100 },
    ]);
    expect(
      imagesPort.adminImageQueries.map((query) => ({
        page: query.page,
        size: query.size,
      })),
    ).toEqual([{ page: 1, size: 100 }]);
    expect(imagesPort.parkItemImageCalls).toEqual([
      { parkId: 'park-1', page: 1, size: 100 },
    ]);
    expect(facade.parkItems().map((item) => item.id)).toEqual([
      'item-1',
      'item-2',
    ]);
    expect(facade.uncategorizedPhotos().map((photo) => photo.id)).toEqual([
      'uncategorized-1',
    ]);
    expect(facade.parkPhotos().map((photo) => photo.id)).toEqual([]);
    expect(facade.parkItemPhotos().map((photo) => photo.id)).toEqual([
      'item-photo-1',
    ]);
    expect(facade.canLoadMoreParkPhotos()).toBe(true);
    expect(facade.canLoadMoreParkItemPhotos()).toBe(true);

    await facade.loadMoreParkPhotos();
    await facade.loadMoreParkItemPhotos();

    expect(
      imagesPort.adminImageQueries.map((query) => ({
        page: query.page,
        size: query.size,
      })),
    ).toEqual([
      { page: 1, size: 100 },
      { page: 2, size: 100 },
    ]);
    expect(imagesPort.parkItemImageCalls).toEqual([
      { parkId: 'park-1', page: 1, size: 100 },
      { parkId: 'park-1', page: 2, size: 100 },
    ]);
    expect(facade.parkPhotos().map((photo) => photo.id)).toEqual([
      'park-1-photo',
    ]);
    expect(facade.parkItemPhotos().map((photo) => photo.id)).toEqual([
      'item-photo-1',
      'item-photo-2',
    ]);
    expect(facade.canLoadMoreParkPhotos()).toBe(false);
    expect(facade.canLoadMoreParkItemPhotos()).toBe(false);
  });

  it('categorizes a draft photo as a park item photo with the selected category tag', async () => {
    imagesPort.parkImagesPage$ = of(
      createPagedResult<ImageDto>([
        createImage('image-1', { isPublished: false, tagIds: [] }),
      ]),
    );
    await prepareSelectedParkAsync(facade);
    facade.setPhotoDraftOwnerKind('image-1', 'parkItem');
    facade.setPhotoDraftParkItemId('image-1', 'item-1');
    facade.setPhotoDraftCategorySlug(
      'image-1',
      PARK_ITEM_PHOTO_CATEGORY_OPTIONS[0].slug,
    );
    await facade.savePhotoCategorization('image-1');

    const request: UpdateAdminImageRequest = imagesPort.updateCalls[0].request;
    expect(request.ownerType).toBe(ImageOwnerType.PARK_ITEM);
    expect(request.category).toBe(ImageCategory.PARK_ITEM);
    expect(request.ownerId).toBe('item-1');
    expect(request.tagIds).toContain(
      `${PARK_ITEM_PHOTO_CATEGORY_OPTIONS[0].slug}-tag`,
    );
    expect(facade.parkItemPhotos().map((photo) => photo.id)).toEqual([
      'image-1',
    ]);
    expect(facade.parkItemPhotos()[0].parkItemName).toBe('Demo Coaster');
    expect(imagesPort.adminImageQueries.length).toBe(1);
  });

  it('classifies selected photos in one action and assigns the chosen current photo without reloading', async () => {
    imagesPort.parkImagesPage$ = of(
      createPagedResult<ImageDto>([
        createImage('image-1', { tagIds: [] }),
        createImage('image-2', { tagIds: [] }),
      ]),
    );
    await prepareSelectedParkAsync(facade);

    facade.setPhotoSelected('image-1', true);
    facade.setPhotoSelected('image-2', true);
    facade.setBulkOwnerKind('parkItem');
    facade.setBulkParkItemId('item-1');
    facade.setBulkCategorySlug(PARK_ITEM_PHOTO_CATEGORY_OPTIONS[0].slug);
    facade.setBulkCurrentImageId('image-2');

    await facade.applyBulkCategorization();

    expect(imagesPort.updateCalls.map((call) => call.id)).toEqual([
      'image-2',
      'image-1',
    ]);
    expect(
      imagesPort.updateCalls.every(
        (call) => call.request.ownerType === ImageOwnerType.PARK_ITEM,
      ),
    ).toBe(true);
    expect(
      imagesPort.updateCalls.every((call) => call.request.ownerId === 'item-1'),
    ).toBe(true);
    expect(
      imagesPort.updateCalls.find((call) => call.id === 'image-2')?.request
        .isCurrent,
    ).toBe(true);
    expect(
      facade
        .parkItemPhotos()
        .map((photo) => photo.id)
        .sort(),
    ).toEqual(['image-1', 'image-2']);
    expect(
      facade.parkItemPhotos().find((photo) => photo.id === 'image-2')?.image
        .isCurrent,
    ).toBe(true);
    expect(facade.selectedPhotoCount()).toBe(0);
    expect(imagesPort.adminImageQueries.length).toBe(1);
  });

  it('sets a classified photo as current and updates the local scope without reloading', async () => {
    const categoryTagId: string = `${PARK_PHOTO_CATEGORY_OPTIONS[0].slug}-tag`;
    imagesPort.parkImagesPage$ = of(
      createPagedResult<ImageDto>([
        createImage('image-1', {
          isCurrent: true,
          tagIds: [categoryTagId],
        }),
        createImage('image-2', {
          isCurrent: false,
          tagIds: [categoryTagId],
        }),
      ]),
    );
    imagesPort.currentResponse$ = of(
      createImage('image-2', {
        isCurrent: true,
        tagIds: [categoryTagId],
      }),
    );
    await prepareSelectedParkAsync(facade);

    await facade.setPhotoAsCurrent('image-2');

    expect(imagesPort.currentCalls).toEqual(['image-2']);
    expect(
      facade.parkPhotos().find((photo) => photo.id === 'image-1')?.image
        .isCurrent,
    ).toBe(false);
    expect(
      facade.parkPhotos().find((photo) => photo.id === 'image-2')?.image
        .isCurrent,
    ).toBe(true);
    expect(imagesPort.adminImageQueries.length).toBe(1);
  });

  it('keeps failed bulk photos selected and editable after successful photos have moved', async () => {
    imagesPort.parkImagesPage$ = of(
      createPagedResult<ImageDto>([
        createImage('image-1', { tagIds: [] }),
        createImage('image-2', { tagIds: [] }),
      ]),
    );
    imagesPort.updateErrorsById.add('image-2');
    await prepareSelectedParkAsync(facade);

    facade.setPhotoSelected('image-1', true);
    facade.setPhotoSelected('image-2', true);
    await facade.applyBulkCategorization();

    expect(facade.parkPhotos().map((photo) => photo.id)).toEqual(['image-1']);
    expect(facade.uncategorizedPhotos().map((photo) => photo.id)).toEqual([
      'image-2',
    ]);
    expect(facade.selectedPhotoCount()).toBe(1);
    expect(facade.isPhotoSelected('image-2')).toBe(true);
    expect(facade.uncategorizedPhotos()[0].isSaving).toBe(false);
    expect(imagesPort.adminImageQueries.length).toBe(1);
  });

  it('toggles public visibility through image metadata updates', async () => {
    imagesPort.parkImagesPage$ = of(
      createPagedResult<ImageDto>([
        createImage('image-1', {
          isPublished: false,
          tagIds: [`${PARK_PHOTO_CATEGORY_OPTIONS[0].slug}-tag`],
        }),
      ]),
    );
    await prepareSelectedParkAsync(facade);

    await facade.togglePublished('image-1');

    expect(imagesPort.updateCalls[0].request.isPublished).toBe(true);
  });

  it('keeps uncategorized draft photos hidden until they are classified', async () => {
    imagesPort.parkImagesPage$ = of(
      createPagedResult<ImageDto>([
        createImage('image-1', { isPublished: false, tagIds: [] }),
      ]),
    );
    await prepareSelectedParkAsync(facade);

    await facade.togglePublished('image-1');

    expect(imagesPort.updateCalls).toEqual([]);
    expect(toastMessageService.add).toHaveBeenCalledWith(
      'warn',
      'common.warning',
      'admin.images.batch.toasts.visibilityNeedsCategory',
    );
  });

  it('deletes a loaded photo from the batch workspace', async () => {
    imagesPort.parkImagesPage$ = of(
      createPagedResult<ImageDto>([
        createImage('image-1', { isPublished: false, tagIds: [] }),
      ]),
    );
    await prepareSelectedParkAsync(facade);
    imagesPort.parkImagesPage$ = of(createPagedResult<ImageDto>([]));

    await facade.deletePhoto('image-1');

    expect(imagesPort.deleteCalls).toEqual(['image-1']);
    expect(facade.uncategorizedPhotos()).toEqual([]);
    expect(toastMessageService.add).toHaveBeenCalledWith(
      'success',
      'admin.images.batch.toasts.deleteSummary',
      'admin.images.batch.toasts.deleteDetail',
    );
  });

  it('resets workspace loaders when category tag setup fails', async () => {
    imagesPort.tagsResponse$ = throwError(() => new Error('tags unavailable'));

    facade.loadParks();
    await flushAsyncWork();
    facade.selectPark('park-1');
    await flushAsyncWork();

    expect(facade.parkItemsLoading()).toBe(false);
    expect(facade.photosLoading()).toBe(false);
    expect(toastMessageService.add).toHaveBeenCalledWith(
      'error',
      'common.errorTitle',
      'admin.images.batch.toasts.workspaceLoadError',
    );
  });

  it('resets upload state and keeps selected files when category tag setup fails', async () => {
    imagesPort.tagsResponse$ = throwError(() => new Error('tags unavailable'));

    facade.loadParks();
    await flushAsyncWork();
    facade.selectPark('park-1');
    await flushAsyncWork();
    facade.selectFiles(
      createFileInputEvent(
        new File(['image'], 'entrance.jpg', { type: 'image/jpeg' }),
      ),
    );
    await flushAsyncWork();

    await facade.uploadSelectedFiles();

    expect(facade.uploading()).toBe(false);
    expect(facade.uploadProgress()).toBeNull();
    expect(facade.selectedFileCount()).toBe(1);
    expect(imagesPort.uploadCalls).toEqual([]);
    expect(toastMessageService.add).toHaveBeenCalledWith(
      'error',
      'common.errorTitle',
      'shared.imageUpload.uploadError',
    );
  });

  it('keeps pagination complete after a local deletion without refreshing the workspace', async () => {
    imagesPort.parkImagesPages = {
      1: createPagedResult<ImageDto>(
        [createImage('image-1', { isPublished: false, tagIds: [] })],
        createPagination(2, 1, 2, 100),
      ),
      2: createPagedResult<ImageDto>(
        [createImage('image-3', { isPublished: false, tagIds: [] })],
        createPagination(2, 2, 2, 100),
      ),
    };
    await prepareSelectedParkAsync(facade);
    imagesPort.parkImagesPages[1] = createPagedResult<ImageDto>(
      [createImage('image-2', { isPublished: false, tagIds: [] })],
      createPagination(2, 1, 2, 100),
    );

    await facade.deletePhoto('image-1');

    expect(imagesPort.adminImageQueries.map((query) => query.page)).toEqual([
      1,
    ]);
    expect(facade.uncategorizedPhotos()).toEqual([]);

    await facade.loadMoreParkPhotos();
    await facade.loadMoreParkPhotos();

    expect(imagesPort.adminImageQueries.map((query) => query.page)).toEqual([
      1, 1, 2,
    ]);
    expect(facade.uncategorizedPhotos().map((photo) => photo.id)).toEqual([
      'image-2',
      'image-3',
    ]);
  });

  it('ignores an in-flight park photo page after a deletion rewinds pagination', async () => {
    const staleSecondPage: Subject<PagedResult<ImageDto>> =
      new Subject<PagedResult<ImageDto>>();
    imagesPort.parkImagesPages = {
      1: createPagedResult<ImageDto>(
        [createImage('image-1', { tagIds: [] })],
        createPagination(2, 1, 2, 100),
      ),
    };
    imagesPort.parkImageResponsesByPage[2] = staleSecondPage.asObservable();
    await prepareSelectedParkAsync(facade);

    const staleLoadPromise: Promise<void> = facade.loadMoreParkPhotos();
    await flushAsyncWork();
    imagesPort.parkImagesPages[1] = createPagedResult<ImageDto>(
      [createImage('image-2', { tagIds: [] })],
      createPagination(1, 1, 1, 100),
    );

    await facade.deletePhoto('image-1');
    staleSecondPage.next(
      createPagedResult<ImageDto>(
        [createImage('stale-image', { tagIds: [] })],
        createPagination(2, 2, 2, 100),
      ),
    );
    staleSecondPage.complete();
    await staleLoadPromise;

    expect(facade.uncategorizedPhotos()).toEqual([]);

    await facade.loadMoreParkPhotos();

    expect(imagesPort.adminImageQueries.map((query) => query.page)).toEqual([
      1, 2, 1,
    ]);
    expect(facade.uncategorizedPhotos().map((photo) => photo.id)).toEqual([
      'image-2',
    ]);
  });

  it('ignores an in-flight park item photo page after a deletion rewinds pagination', async () => {
    const staleSecondPage: Subject<PagedResult<ParkItemImageDto>> =
      new Subject<PagedResult<ParkItemImageDto>>();
    imagesPort.parkItemImagesPages = {
      1: createPagedResult<ParkItemImageDto>(
        [createParkItemImage('image-1', 'item-1', 'Demo Coaster')],
        createPagination(2, 1, 2, 100),
      ),
    };
    imagesPort.parkItemImageResponsesByPage[2] =
      staleSecondPage.asObservable();
    await prepareSelectedParkAsync(facade);

    const staleLoadPromise: Promise<void> = facade.loadMoreParkItemPhotos();
    await flushAsyncWork();
    imagesPort.parkItemImagesPages[1] = createPagedResult<ParkItemImageDto>(
      [createParkItemImage('image-2', 'item-1', 'Demo Coaster')],
      createPagination(1, 1, 1, 100),
    );

    await facade.deletePhoto('image-1');
    staleSecondPage.next(
      createPagedResult<ParkItemImageDto>(
        [createParkItemImage('stale-image', 'item-1', 'Demo Coaster')],
        createPagination(2, 2, 2, 100),
      ),
    );
    staleSecondPage.complete();
    await staleLoadPromise;

    expect(facade.parkItemPhotos()).toEqual([]);

    await facade.loadMoreParkItemPhotos();

    expect(imagesPort.parkItemImageCalls.map((call) => call.page)).toEqual([
      1, 2, 1,
    ]);
    expect(facade.parkItemPhotos().map((photo) => photo.id)).toEqual([
      'image-2',
    ]);
  });
});

async function prepareSelectedParkAsync(
  facade: AdminPhotoBatchStateFacade,
): Promise<void> {
  facade.loadInitialData();
  await flushAsyncWork();
  facade.selectPark('park-1');
  await flushAsyncWork();
}

function createFileInputEvent(...files: File[]): Event {
  const input: HTMLInputElement = document.createElement('input');
  Object.defineProperty(input, 'files', {
    value: files,
  });

  return { target: input } as unknown as Event;
}

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

function createParkItemImage(
  imageId: string,
  itemId: string,
  itemName: string,
): ParkItemImageDto {
  return {
    image: createImage(imageId, {
      category: ImageCategory.PARK_ITEM,
      ownerType: ImageOwnerType.PARK_ITEM,
      ownerId: itemId,
      tagIds: [`${PARK_ITEM_PHOTO_CATEGORY_OPTIONS[0].slug}-tag`],
    }),
    item: {
      id: itemId,
      parkId: 'park-1',
      name: itemName,
      category: 'Attraction',
      type: 'RollerCoaster',
      latitude: null,
      longitude: null,
    },
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
    adminReviewStatus: 'Validated',
  };
}

function createPagination(
  totalItems: number,
  currentPage: number = 1,
  totalPages: number = 1,
  itemsPerPage: number = Math.max(totalItems, 1),
) {
  return {
    totalItems,
    totalPages,
    currentPage,
    itemsPerPage,
  };
}

function createMetadata(
  geoLocation: ImageGeoLocation | null,
): AdminContextualPhotoMetadataPreview {
  return {
    sourceKind: 'file',
    fileName: 'entrance.jpg',
    contentType: 'image/jpeg',
    sizeInBytes: 2048,
    width: 1200,
    height: 800,
    geoLocation,
    geoStatus: geoLocation ? 'detected' : 'missing',
  };
}

function flushAsyncWork(): Promise<void> {
  return new Promise((resolve: () => void) => setTimeout(resolve, 0));
}
