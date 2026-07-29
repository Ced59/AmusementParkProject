import type { MockedObject } from 'vitest';
import { DestroyRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { ImageUploadSecurityService } from '@shared/utils/security';

import {
  ADMIN_PARK_PHOTOS_STATE_IMAGES_API_SERVICE_PORT,
  AdminParkPhotosStateImagesApiServicePort,
} from './admin-park-photos-state-data.ports';
import { AdminParkPhotosStateFacade } from './admin-park-photos-state.facade';

describe('AdminParkPhotosStateFacade', () => {
  let facade: AdminParkPhotosStateFacade;
  let imagesPort: MockedObject<AdminParkPhotosStateImagesApiServicePort>;
  let toastMessageService: MockedObject<ToastMessageService>;
  let imageUploadSecurityService: MockedObject<ImageUploadSecurityService>;
  let translateService: MockedObject<TranslateService>;

  beforeEach(() => {
    imagesPort = {
      createAdminImageTag: vi
        .fn()
        .mockName(
          'AdminParkPhotosStateImagesApiServicePort.createAdminImageTag',
        ),
      deleteImage: vi
        .fn()
        .mockName('AdminParkPhotosStateImagesApiServicePort.deleteImage'),
      getAdminImageTags: vi
        .fn()
        .mockName('AdminParkPhotosStateImagesApiServicePort.getAdminImageTags'),
      getImages: vi
        .fn()
        .mockName('AdminParkPhotosStateImagesApiServicePort.getImages'),
      importRemoteImage: vi
        .fn()
        .mockName('AdminParkPhotosStateImagesApiServicePort.importRemoteImage'),
      linkImage: vi
        .fn()
        .mockName('AdminParkPhotosStateImagesApiServicePort.linkImage'),
      setCurrentImage: vi
        .fn()
        .mockName('AdminParkPhotosStateImagesApiServicePort.setCurrentImage'),
      updateAdminImage: vi
        .fn()
        .mockName('AdminParkPhotosStateImagesApiServicePort.updateAdminImage'),
      uploadImage: vi
        .fn()
        .mockName('AdminParkPhotosStateImagesApiServicePort.uploadImage'),
    } as unknown as MockedObject<AdminParkPhotosStateImagesApiServicePort>;
    toastMessageService = {
      add: vi.fn().mockName('ToastMessageService.add'),
    } as unknown as MockedObject<ToastMessageService>;
    imageUploadSecurityService = {
      filterValidImageFiles: vi
        .fn()
        .mockName('ImageUploadSecurityService.filterValidImageFiles'),
    } as unknown as MockedObject<ImageUploadSecurityService>;
    translateService = {
      instant: vi.fn().mockName('TranslateService.instant'),
    } as unknown as MockedObject<TranslateService>;

    imageUploadSecurityService.filterValidImageFiles.mockImplementation(
      (files: File[]) => files,
    );
    translateService.instant.mockImplementation(
      (key: string | string[]) => key,
    );
    imagesPort.getAdminImageTags.mockReturnValue(
      of([
        {
          id: 'tag-gallery',
          slug: 'park-gallery',
          labels: [],
          descriptions: [],
          isActive: true,
          createdAt: '',
          updatedAt: '',
        },
      ]),
    );
    imagesPort.createAdminImageTag.mockImplementation((request) =>
      of({
        id: `${request.slug}-tag`,
        slug: request.slug,
        labels: request.labels,
        descriptions: request.descriptions,
        isActive: true,
        createdAt: '',
        updatedAt: '',
      }),
    );
    imagesPort.linkImage.mockReturnValue(of(createImageDto()));
    imagesPort.updateAdminImage.mockImplementation((imageId, request) =>
      of(createImageDto({ id: imageId, geoLocation: request.geoLocation })),
    );

    TestBed.configureTestingModule({
      providers: [
        AdminParkPhotosStateFacade,
        {
          provide: ADMIN_PARK_PHOTOS_STATE_IMAGES_API_SERVICE_PORT,
          useValue: imagesPort,
        },
        { provide: ToastMessageService, useValue: toastMessageService },
        {
          provide: ImageUploadSecurityService,
          useValue: imageUploadSecurityService,
        },
        { provide: TranslateService, useValue: translateService },
        { provide: DestroyRef, useValue: { onDestroy: vi.fn() } },
      ],
    });

    facade = TestBed.inject(AdminParkPhotosStateFacade);
  });

  it('stores uploaded EXIF coordinates on park photos', async () => {
    imagesPort.uploadImage.mockReturnValue(
      of({ id: 'uploaded-1', latitude: 50.1, longitude: 3.2 }),
    );

    facade.selectPhotoFiles(
      createFileInputEvent(
        new File(['image'], 'photo.jpg', { type: 'image/jpeg' }),
      ),
    );
    await facade.uploadSelectedPhotos('park-1', 'Park');

    expect(imagesPort.updateAdminImage).toHaveBeenCalledWith(
      'image-1',
      expect.objectContaining({
        geoLocation: { latitude: 50.1, longitude: 3.2 },
        tagIds: ['tag-gallery'],
      }),
    );
    expect(
      vi
        .mocked(toastMessageService.add)
        .mock.calls.some((args: unknown[]) => args[0] === 'warn'),
    ).toBe(false);
  });

  it('warns without blocking when a park photo has no GPS coordinates', async () => {
    imagesPort.uploadImage.mockReturnValue(of({ id: 'uploaded-1' }));

    facade.selectPhotoFiles(
      createFileInputEvent(
        new File(['image'], 'photo.jpg', { type: 'image/jpeg' }),
      ),
    );
    await facade.uploadSelectedPhotos('park-1', 'Park');

    expect(imagesPort.linkImage).toHaveBeenCalled();
    expect(imagesPort.updateAdminImage).toHaveBeenCalledWith(
      'image-1',
      expect.objectContaining({ geoLocation: null }),
    );
    expect(toastMessageService.add).toHaveBeenCalledWith(
      'warn',
      'common.warning',
      'admin.contextualBlocks.drawer.photoMetadataGeoMissing',
    );
  });
});

function createFileInputEvent(file: File): Event {
  return { target: { files: [file], value: '' } } as unknown as Event;
}

function createImageDto(partial: Partial<ImageDto> = {}): ImageDto {
  return {
    id: 'image-1',
    category: ImageCategory.PARK,
    ownerType: ImageOwnerType.PARK,
    ownerId: 'park-1',
    isCurrent: false,
    isPublished: true,
    isWatermarked: true,
    width: 100,
    height: 100,
    sizeInBytes: 10,
    sourceUrl: null,
    geoLocation: null,
    exifMetadata: null,
    altTexts: [],
    captions: [],
    credits: [],
    tagIds: [],
    createdAt: '',
    updatedAt: '',
    ...partial,
  };
}
