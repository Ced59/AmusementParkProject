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
  AdminFieldModeParkItemsApiServicePort
} from './admin-field-mode-data.ports';
import { AdminFieldModeActionsFacade } from './admin-field-mode-actions.facade';

describe('AdminFieldModeActionsFacade', () => {
  let facade: AdminFieldModeActionsFacade;
  let imagesPort: jasmine.SpyObj<AdminFieldModeImagesApiServicePort>;
  let positionPort: jasmine.SpyObj<AdminFieldModeGeolocationPort>;
  let photoGpsService: jasmine.SpyObj<PhotoGpsMetadataService>;
  let imageUploadSecurityService: jasmine.SpyObj<ImageUploadSecurityService>;
  let toastMessageService: jasmine.SpyObj<ToastMessageService>;
  let translateService: jasmine.SpyObj<TranslateService> & { currentLang: string };

  beforeEach(() => {
    imagesPort = jasmine.createSpyObj<AdminFieldModeImagesApiServicePort>('AdminFieldModeImagesApiServicePort', ['getImagesPage', 'uploadImage', 'linkImage', 'updateAdminImage', 'getAdminImageTags', 'createAdminImageTag']);
    const parkItemsPort = jasmine.createSpyObj<AdminFieldModeParkItemsApiServicePort>('AdminFieldModeParkItemsApiServicePort', ['getParkItemsByParkId', 'getParkItemsByParkIdPage', 'getParkItemsPaginated', 'getParkItemById', 'updateParkItem']);
    positionPort = jasmine.createSpyObj<AdminFieldModeGeolocationPort>('AdminFieldModeGeolocationPort', ['getCurrentPosition', 'getPermissionState', 'watchPosition', 'clearWatch']);
    photoGpsService = jasmine.createSpyObj<PhotoGpsMetadataService>('PhotoGpsMetadataService', ['readPosition']);
    imageUploadSecurityService = jasmine.createSpyObj<ImageUploadSecurityService>('ImageUploadSecurityService', ['validateImageFile']);
    toastMessageService = jasmine.createSpyObj<ToastMessageService>('ToastMessageService', ['add']);
    translateService = jasmine.createSpyObj<TranslateService>('TranslateService', ['instant'], { currentLang: 'fr' }) as jasmine.SpyObj<TranslateService> & { currentLang: string };

    imageUploadSecurityService.validateImageFile.and.returnValue({ isValid: true, errorKey: null });
    positionPort.getPermissionState.and.returnValue(Promise.resolve('granted'));
    positionPort.watchPosition.and.callFake((successCallback: PositionCallback) => {
      queueMicrotask(() => successCallback(createGeolocationPosition(50.1, 3.2, 5)));
      return 7;
    });
    photoGpsService.readPosition.and.returnValue(Promise.resolve(createFieldPosition()));
    translateService.instant.and.callFake((key: string) => key);
    imagesPort.getAdminImageTags.and.returnValue(of([{ id: 'tag-gallery', slug: 'park-item-gallery', labels: [], descriptions: [], isActive: true, createdAt: '', updatedAt: '' }]));
    imagesPort.createAdminImageTag.and.callFake((request) => of({ id: `${request.slug}-tag`, slug: request.slug, labels: request.labels, descriptions: request.descriptions, isActive: true, createdAt: '', updatedAt: '' }));
    imagesPort.uploadImage.and.returnValue(of({ id: 'uploaded-1' }));
    imagesPort.linkImage.and.returnValue(of(createImageDto()));
    imagesPort.updateAdminImage.and.returnValue(of(createImageDto()));

    TestBed.configureTestingModule({
      providers: [
        AdminFieldModeActionsFacade,
        { provide: ADMIN_FIELD_MODE_IMAGES_API_SERVICE_PORT, useValue: imagesPort },
        { provide: ADMIN_FIELD_MODE_PARK_ITEMS_API_SERVICE_PORT, useValue: parkItemsPort },
        { provide: ADMIN_FIELD_MODE_GEOLOCATION_PORT, useValue: positionPort },
        { provide: ImageUploadSecurityService, useValue: imageUploadSecurityService },
        { provide: PhotoGpsMetadataService, useValue: photoGpsService },
        { provide: ToastMessageService, useValue: toastMessageService },
        { provide: TranslateService, useValue: translateService }
      ]
    });

    facade = TestBed.inject(AdminFieldModeActionsFacade);
  });

  it('rejects a selected photo when no image gps metadata is found', async () => {
    const file: File = new File(['image'], 'photo.jpg', { type: 'image/jpeg' });
    photoGpsService.readPosition.and.returnValue(Promise.resolve(null));

    await facade.selectFile(createFileInputEvent(file));

    expect(facade.selectedFile()).toBeNull();
    expect(facade.statusMessageKey()).toBe('admin.fieldMode.messages.photoMissingGps');
  });

  it('keeps a selected photo when image gps metadata is found', async () => {
    const file: File = new File(['image'], 'photo.jpg', { type: 'image/jpeg' });

    await facade.selectFile(createFileInputEvent(file));

    expect(facade.selectedFile()).toBe(file);
    expect(facade.readyForPhoto()).toBeTrue();
    expect(facade.statusMessageKey()).toBe('admin.fieldMode.messages.photoGpsReady');
  });

  it('uploads and stores photo geolocation from image metadata', async () => {
    const file: File = new File(['image'], 'photo.jpg', { type: 'image/jpeg' });

    await facade.selectFile(createFileInputEvent(file));
    const uploaded: boolean = facade.addPhoto({ id: 'item-1', parkId: 'park-1', name: 'Ride', category: 'Attraction', type: 'FlatRide', latitude: null, longitude: null }, true);
    await flushAsyncWork();

    expect(uploaded).toBeTrue();
    expect(positionPort.getCurrentPosition).not.toHaveBeenCalled();
    expect(imagesPort.updateAdminImage).toHaveBeenCalledOnceWith('image-1', jasmine.objectContaining({ geoLocation: { latitude: 50.1, longitude: 3.2 }, tagIds: ['tag-gallery'] }));
  });

  it('keeps multiple selected photos with image gps metadata', async () => {
    const firstFile: File = new File(['image-1'], 'photo-1.jpg', { type: 'image/jpeg' });
    const secondFile: File = new File(['image-2'], 'photo-2.jpg', { type: 'image/jpeg' });

    await facade.selectFiles(createFileInputEvent(firstFile, secondFile));

    expect(facade.selectedPhotos().map((selection) => selection.file)).toEqual([firstFile, secondFile]);
    expect(facade.readyForPhoto()).toBeTrue();
  });

  it('stops location capture when browser permission is denied', async () => {
    positionPort.getPermissionState.and.returnValue(Promise.resolve('denied'));

    await expectAsync(facade.refreshPosition()).toBeRejected();

    expect(positionPort.getCurrentPosition).not.toHaveBeenCalled();
    expect(facade.statusMessageKey()).toBe('admin.fieldMode.messages.positionDenied');
  });

  it('stops location capture when browser policy blocks geolocation', async () => {
    positionPort.getPermissionState.and.returnValue(Promise.resolve('blocked-by-policy'));

    await expectAsync(facade.refreshPosition()).toBeRejected();

    expect(positionPort.getCurrentPosition).not.toHaveBeenCalled();
    expect(facade.statusMessageKey()).toBe('admin.fieldMode.messages.positionBlockedByPolicy');
  });
});

function createFileInputEvent(...files: File[]): Event {
  return { target: { files, value: '' } } as unknown as Event;
}

function createFieldPosition() {
  return { latitude: 50.1, longitude: 3.2, accuracy: null, capturedAt: Date.now() };
}

function createGeolocationPosition(latitude: number, longitude: number, accuracy: number): GeolocationPosition {
  return {
    coords: {
      latitude,
      longitude,
      accuracy,
      altitude: null,
      altitudeAccuracy: null,
      heading: null,
      speed: null,
      toJSON: () => ({ latitude, longitude, accuracy })
    },
    timestamp: Date.now(),
    toJSON: () => ({ latitude, longitude, accuracy })
  };
}

async function flushAsyncWork(): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 0));
}

function createImageDto(): ImageDto {
  return {
    id: 'image-1', category: ImageCategory.PARK_ITEM, ownerType: ImageOwnerType.PARK_ITEM, ownerId: 'item-1', isCurrent: false, isPublished: true, isWatermarked: true,
    width: 100, height: 100, sizeInBytes: 10, sourceUrl: null, geoLocation: null, exifMetadata: null, altTexts: [], captions: [], credits: [], tagIds: [], createdAt: '', updatedAt: ''
  };
}
