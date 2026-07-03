import { DestroyRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { PhotoGpsMetadataService } from '@shared/utils/images/photo-gps-metadata.service';
import { ImageUploadSecurityService } from '@shared/utils/security';

import {
  ADMIN_PARK_ITEM_PHOTOS_STATE_IMAGES_API_SERVICE_PORT,
  AdminParkItemPhotosStateImagesApiServicePort
} from './admin-park-item-photos-state-data.ports';
import { AdminParkItemPhotosStateFacade } from './admin-park-item-photos-state.facade';

describe('AdminParkItemPhotosStateFacade', () => {
  let facade: AdminParkItemPhotosStateFacade;
  let imagesPort: jasmine.SpyObj<AdminParkItemPhotosStateImagesApiServicePort>;
  let toastMessageService: jasmine.SpyObj<ToastMessageService>;
  let imageUploadSecurityService: jasmine.SpyObj<ImageUploadSecurityService>;
  let photoGpsMetadataService: jasmine.SpyObj<PhotoGpsMetadataService>;
  let translateService: jasmine.SpyObj<TranslateService>;

  beforeEach(() => {
    imagesPort = jasmine.createSpyObj<AdminParkItemPhotosStateImagesApiServicePort>('AdminParkItemPhotosStateImagesApiServicePort', [
      'createAdminImageTag',
      'deleteImage',
      'getAdminImageTags',
      'getImages',
      'importRemoteImage',
      'linkImage',
      'setCurrentImage',
      'updateAdminImage',
      'uploadImage'
    ]);
    toastMessageService = jasmine.createSpyObj<ToastMessageService>('ToastMessageService', ['add']);
    imageUploadSecurityService = jasmine.createSpyObj<ImageUploadSecurityService>('ImageUploadSecurityService', ['filterValidImageFiles']);
    photoGpsMetadataService = jasmine.createSpyObj<PhotoGpsMetadataService>('PhotoGpsMetadataService', ['readPosition']);
    translateService = jasmine.createSpyObj<TranslateService>('TranslateService', ['instant']);

    imageUploadSecurityService.filterValidImageFiles.and.callFake((files: File[]) => files);
    photoGpsMetadataService.readPosition.and.returnValue(Promise.resolve(null));
    translateService.instant.and.callFake((key: string) => key);
    imagesPort.getAdminImageTags.and.returnValue(of([{ id: 'tag-gallery', slug: 'park-item-gallery', labels: [], descriptions: [], isActive: true, createdAt: '', updatedAt: '' }]));
    imagesPort.createAdminImageTag.and.callFake((request) => of({ id: `${request.slug}-tag`, slug: request.slug, labels: request.labels, descriptions: request.descriptions, isActive: true, createdAt: '', updatedAt: '' }));
    imagesPort.linkImage.and.returnValue(of(createImageDto()));
    imagesPort.setCurrentImage.and.callFake((imageId: string) => of(createImageDto({ id: imageId, isCurrent: true })));
    imagesPort.updateAdminImage.and.callFake((imageId, request) => of(createImageDto({ id: imageId, geoLocation: request.geoLocation })));

    TestBed.configureTestingModule({
      providers: [
        AdminParkItemPhotosStateFacade,
        { provide: ADMIN_PARK_ITEM_PHOTOS_STATE_IMAGES_API_SERVICE_PORT, useValue: imagesPort },
        { provide: ToastMessageService, useValue: toastMessageService },
        { provide: ImageUploadSecurityService, useValue: imageUploadSecurityService },
        { provide: PhotoGpsMetadataService, useValue: photoGpsMetadataService },
        { provide: TranslateService, useValue: translateService },
        { provide: DestroyRef, useValue: { onDestroy: jasmine.createSpy('onDestroy') } }
      ]
    });

    facade = TestBed.inject(AdminParkItemPhotosStateFacade);
  });

  it('stores uploaded EXIF coordinates on park item photos', async () => {
    imagesPort.uploadImage.and.returnValue(of({ id: 'uploaded-1', latitude: 50.1, longitude: 3.2 }));

    facade.selectPhotoFiles(createFileInputEvent(new File(['image'], 'photo.jpg', { type: 'image/jpeg' })));
    await flushAsyncWork();
    await facade.uploadSelectedPhotos('item-1', 'Ride');

    expect(imagesPort.updateAdminImage).toHaveBeenCalledWith('image-1', jasmine.objectContaining({
      geoLocation: { latitude: 50.1, longitude: 3.2 },
      tagIds: ['tag-gallery']
    }));
    expect(toastMessageService.add.calls.allArgs().some((args: unknown[]) => args[0] === 'warn')).toBeFalse();
  });

  it('warns without blocking when a park item photo has no GPS coordinates', async () => {
    imagesPort.uploadImage.and.returnValue(of({ id: 'uploaded-1' }));

    facade.selectPhotoFiles(createFileInputEvent(new File(['image'], 'photo.jpg', { type: 'image/jpeg' })));
    await flushAsyncWork();
    await facade.uploadSelectedPhotos('item-1', 'Ride');

    expect(imagesPort.linkImage).toHaveBeenCalled();
    expect(imagesPort.updateAdminImage).toHaveBeenCalledWith('image-1', jasmine.objectContaining({ geoLocation: null }));
    expect(toastMessageService.add).toHaveBeenCalledWith(
      'warn',
      'common.warning',
      'admin.contextualBlocks.drawer.photoMetadataGeoMissing'
    );
  });

  it('uses locally detected GPS coordinates when the upload response has none', async () => {
    photoGpsMetadataService.readPosition.and.returnValue(Promise.resolve({
      latitude: 49.2,
      longitude: 2.4,
      accuracy: null,
      capturedAt: Date.now()
    }));
    imagesPort.uploadImage.and.returnValue(of({ id: 'uploaded-1' }));

    facade.selectPhotoFiles(createFileInputEvent(new File(['image'], 'photo.jpg', { type: 'image/jpeg' })));
    await flushAsyncWork();
    await facade.uploadSelectedPhotos('item-1', 'Ride');

    expect(imagesPort.updateAdminImage).toHaveBeenCalledWith('image-1', jasmine.objectContaining({
      geoLocation: { latitude: 49.2, longitude: 2.4 }
    }));
    expect(toastMessageService.add.calls.allArgs().some((args: unknown[]) => args[0] === 'warn')).toBeFalse();
  });

  it('persists the first successful upload as current when the first selected file fails', async () => {
    imagesPort.uploadImage.and.returnValues(
      throwError(() => new Error('upload failed')),
      of({ id: 'uploaded-2' })
    );
    imagesPort.linkImage.and.returnValue(of(createImageDto({ id: 'image-2', isCurrent: false })));
    imagesPort.updateAdminImage.and.callFake((imageId, request) => of(createImageDto({
      id: imageId,
      geoLocation: request.geoLocation,
      isCurrent: false,
      tagIds: request.tagIds
    })));
    imagesPort.setCurrentImage.and.returnValue(of(createImageDto({ id: 'image-2', isCurrent: true })));

    facade.selectPhotoFiles(createFileInputEvent([
      new File(['broken'], 'broken.jpg', { type: 'image/jpeg' }),
      new File(['image'], 'photo.jpg', { type: 'image/jpeg' })
    ]));
    await flushAsyncWork();
    await facade.uploadSelectedPhotos('item-1', 'Ride');

    expect(imagesPort.linkImage).toHaveBeenCalledWith(jasmine.objectContaining({
      imageId: 'uploaded-2',
      setAsCurrent: false
    }));
    expect(imagesPort.setCurrentImage).toHaveBeenCalledWith('image-2');
    expect(facade.currentPhoto()?.id).toBe('image-2');
  });
});

function createFileInputEvent(file: File | File[]): Event {
  return { target: { files: Array.isArray(file) ? file : [file], value: '' } } as unknown as Event;
}

async function flushAsyncWork(): Promise<void> {
  await Promise.resolve();
  await Promise.resolve();
}

function createImageDto(partial: Partial<ImageDto> = {}): ImageDto {
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
    ...partial
  };
}
