import type { MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { ImageUploadSecurityService } from '@shared/utils/security';
import { PhotoGpsMetadataService } from '@shared/utils/images/photo-gps-metadata.service';
import { TranslateService } from '@ngx-translate/core';

import {
  ADMIN_FIELD_MODE_GEOLOCATION_PORT,
  ADMIN_FIELD_MODE_IMAGES_API_SERVICE_PORT,
  ADMIN_FIELD_MODE_PARK_ITEMS_API_SERVICE_PORT,
  AdminFieldModeGeolocationPort,
  AdminFieldModeImagesApiServicePort,
  AdminFieldModeParkItemsApiServicePort,
} from './admin-field-mode-data.ports';
import { AdminFieldModeActionsFacade } from './admin-field-mode-actions.facade';

describe('AdminFieldModeActionsFacade', () => {
  let facade: AdminFieldModeActionsFacade;
  let imagesPort: MockedObject<AdminFieldModeImagesApiServicePort>;
  let positionPort: MockedObject<AdminFieldModeGeolocationPort>;
  let photoGpsService: MockedObject<PhotoGpsMetadataService>;
  let imageUploadSecurityService: MockedObject<ImageUploadSecurityService>;
  let toastMessageService: MockedObject<ToastMessageService>;
  let translateService: MockedObject<TranslateService> & {
    currentLang: string;
  };

  beforeEach(() => {
    imagesPort = {
      getImagesPage: vi
        .fn()
        .mockName('AdminFieldModeImagesApiServicePort.getImagesPage'),
      uploadImage: vi
        .fn()
        .mockName('AdminFieldModeImagesApiServicePort.uploadImage'),
      linkImage: vi
        .fn()
        .mockName('AdminFieldModeImagesApiServicePort.linkImage'),
      updateAdminImage: vi
        .fn()
        .mockName('AdminFieldModeImagesApiServicePort.updateAdminImage'),
      getAdminImageTags: vi
        .fn()
        .mockName('AdminFieldModeImagesApiServicePort.getAdminImageTags'),
      createAdminImageTag: vi
        .fn()
        .mockName('AdminFieldModeImagesApiServicePort.createAdminImageTag'),
    } as unknown as MockedObject<AdminFieldModeImagesApiServicePort>;
    const parkItemsPort = {
      getParkItemsByParkId: vi
        .fn()
        .mockName('AdminFieldModeParkItemsApiServicePort.getParkItemsByParkId'),
      getParkItemsByParkIdPage: vi
        .fn()
        .mockName(
          'AdminFieldModeParkItemsApiServicePort.getParkItemsByParkIdPage',
        ),
      getParkItemsPaginated: vi
        .fn()
        .mockName(
          'AdminFieldModeParkItemsApiServicePort.getParkItemsPaginated',
        ),
      getParkItemById: vi
        .fn()
        .mockName('AdminFieldModeParkItemsApiServicePort.getParkItemById'),
      updateParkItem: vi
        .fn()
        .mockName('AdminFieldModeParkItemsApiServicePort.updateParkItem'),
    };
    positionPort = {
      getCurrentPosition: vi
        .fn()
        .mockName('AdminFieldModeGeolocationPort.getCurrentPosition'),
      getPermissionState: vi
        .fn()
        .mockName('AdminFieldModeGeolocationPort.getPermissionState'),
      watchPosition: vi
        .fn()
        .mockName('AdminFieldModeGeolocationPort.watchPosition'),
      clearWatch: vi.fn().mockName('AdminFieldModeGeolocationPort.clearWatch'),
    } as unknown as MockedObject<AdminFieldModeGeolocationPort>;
    photoGpsService = {
      readPosition: vi.fn().mockName('PhotoGpsMetadataService.readPosition'),
    } as unknown as MockedObject<PhotoGpsMetadataService>;
    imageUploadSecurityService = {
      validateImageFile: vi
        .fn()
        .mockName('ImageUploadSecurityService.validateImageFile'),
    } as unknown as MockedObject<ImageUploadSecurityService>;
    toastMessageService = {
      add: vi.fn().mockName('ToastMessageService.add'),
    } as unknown as MockedObject<ToastMessageService>;
    translateService = {
      instant: vi.fn().mockName('TranslateService.instant'),
      currentLang: 'fr',
    } as MockedObject<TranslateService> & {
      currentLang: string;
    };

    imageUploadSecurityService.validateImageFile.mockReturnValue({
      isValid: true,
      errorKey: null,
    });
    positionPort.getPermissionState.mockReturnValue(Promise.resolve('granted'));
    positionPort.watchPosition.mockImplementation(
      (successCallback: PositionCallback) => {
        queueMicrotask(() =>
          successCallback(createGeolocationPosition(50.1, 3.2, 5)),
        );
        return 7;
      },
    );
    photoGpsService.readPosition.mockReturnValue(
      Promise.resolve(createFieldPosition()),
    );
    translateService.instant.mockImplementation(
      (key: string | string[]) => key,
    );
    imagesPort.getAdminImageTags.mockReturnValue(
      of([
        {
          id: 'tag-gallery',
          slug: 'park-item-gallery',
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
    imagesPort.uploadImage.mockReturnValue(of({ id: 'uploaded-1' }));
    imagesPort.linkImage.mockReturnValue(of(createImageDto()));
    imagesPort.updateAdminImage.mockReturnValue(of(createImageDto()));

    TestBed.configureTestingModule({
      providers: [
        AdminFieldModeActionsFacade,
        {
          provide: ADMIN_FIELD_MODE_IMAGES_API_SERVICE_PORT,
          useValue: imagesPort,
        },
        {
          provide: ADMIN_FIELD_MODE_PARK_ITEMS_API_SERVICE_PORT,
          useValue: parkItemsPort,
        },
        { provide: ADMIN_FIELD_MODE_GEOLOCATION_PORT, useValue: positionPort },
        {
          provide: ImageUploadSecurityService,
          useValue: imageUploadSecurityService,
        },
        { provide: PhotoGpsMetadataService, useValue: photoGpsService },
        { provide: ToastMessageService, useValue: toastMessageService },
        { provide: TranslateService, useValue: translateService },
      ],
    });

    facade = TestBed.inject(AdminFieldModeActionsFacade);
  });

  it('rejects a selected photo when no image gps metadata is found', async () => {
    const file: File = new File(['image'], 'photo.jpg', { type: 'image/jpeg' });
    photoGpsService.readPosition.mockReturnValue(Promise.resolve(null));

    await facade.selectFile(createFileInputEvent(file));

    expect(facade.selectedFile()).toBeNull();
    expect(facade.statusMessageKey()).toBe(
      'admin.fieldMode.messages.photoMissingGps',
    );
  });

  it('keeps a selected photo when image gps metadata is found', async () => {
    const file: File = new File(['image'], 'photo.jpg', { type: 'image/jpeg' });

    await facade.selectFile(createFileInputEvent(file));

    expect(facade.selectedFile()).toBe(file);
    expect(facade.readyForPhoto()).toBe(true);
    expect(facade.statusMessageKey()).toBe(
      'admin.fieldMode.messages.photoGpsReady',
    );
  });

  it('uploads and stores photo geolocation from image metadata', async () => {
    const file: File = new File(['image'], 'photo.jpg', { type: 'image/jpeg' });

    await facade.selectFile(createFileInputEvent(file));
    const uploaded: boolean = facade.addPhoto(
      {
        id: 'item-1',
        parkId: 'park-1',
        name: 'Ride',
        category: 'Attraction',
        type: 'FlatRide',
        latitude: null,
        longitude: null,
      },
      true,
    );
    await flushAsyncWork();

    expect(uploaded).toBe(true);
    expect(positionPort.getCurrentPosition).not.toHaveBeenCalled();
    expect(imagesPort.updateAdminImage).toHaveBeenCalledTimes(1);
    expect(imagesPort.updateAdminImage).toHaveBeenCalledWith(
      'image-1',
      expect.objectContaining({
        geoLocation: { latitude: 50.1, longitude: 3.2 },
        tagIds: ['tag-gallery'],
      }),
    );
  });

  it('keeps multiple selected photos with image gps metadata', async () => {
    const firstFile: File = new File(['image-1'], 'photo-1.jpg', {
      type: 'image/jpeg',
    });
    const secondFile: File = new File(['image-2'], 'photo-2.jpg', {
      type: 'image/jpeg',
    });

    await facade.selectFiles(createFileInputEvent(firstFile, secondFile));

    expect(facade.selectedPhotos().map((selection) => selection.file)).toEqual([
      firstFile,
      secondFile,
    ]);
    expect(facade.readyForPhoto()).toBe(true);
  });

  it('stops location capture when browser permission is denied', async () => {
    positionPort.getPermissionState.mockReturnValue(Promise.resolve('denied'));

    await expect(facade.refreshPosition()).rejects.toThrow();

    expect(positionPort.getCurrentPosition).not.toHaveBeenCalled();
    expect(facade.statusMessageKey()).toBe(
      'admin.fieldMode.messages.positionDenied',
    );
  });

  it('stops location capture when browser policy blocks geolocation', async () => {
    positionPort.getPermissionState.mockReturnValue(
      Promise.resolve('blocked-by-policy'),
    );

    await expect(facade.refreshPosition()).rejects.toThrow();

    expect(positionPort.getCurrentPosition).not.toHaveBeenCalled();
    expect(facade.statusMessageKey()).toBe(
      'admin.fieldMode.messages.positionBlockedByPolicy',
    );
  });
});

function createFileInputEvent(...files: File[]): Event {
  return { target: { files, value: '' } } as unknown as Event;
}

function createFieldPosition() {
  return {
    latitude: 50.1,
    longitude: 3.2,
    accuracy: null,
    capturedAt: Date.now(),
  };
}

function createGeolocationPosition(
  latitude: number,
  longitude: number,
  accuracy: number,
): GeolocationPosition {
  return {
    coords: {
      latitude,
      longitude,
      accuracy,
      altitude: null,
      altitudeAccuracy: null,
      heading: null,
      speed: null,
      toJSON: () => ({ latitude, longitude, accuracy }),
    },
    timestamp: Date.now(),
    toJSON: () => ({ latitude, longitude, accuracy }),
  };
}

async function flushAsyncWork(): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 0));
}

function createImageDto(): ImageDto {
  return {
    id: 'image-1',
    category: ImageCategory.PARK_ITEM,
    ownerType: ImageOwnerType.PARK_ITEM,
    ownerId: 'item-1',
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
  };
}
