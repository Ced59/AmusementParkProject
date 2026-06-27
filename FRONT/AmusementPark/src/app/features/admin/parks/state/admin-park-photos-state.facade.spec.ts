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
  AdminParkPhotosStateImagesApiServicePort
} from './admin-park-photos-state-data.ports';
import { AdminParkPhotosStateFacade } from './admin-park-photos-state.facade';

describe('AdminParkPhotosStateFacade', () => {
  let facade: AdminParkPhotosStateFacade;
  let imagesPort: jasmine.SpyObj<AdminParkPhotosStateImagesApiServicePort>;
  let toastMessageService: jasmine.SpyObj<ToastMessageService>;
  let imageUploadSecurityService: jasmine.SpyObj<ImageUploadSecurityService>;
  let translateService: jasmine.SpyObj<TranslateService>;

  beforeEach(() => {
    imagesPort = jasmine.createSpyObj<AdminParkPhotosStateImagesApiServicePort>('AdminParkPhotosStateImagesApiServicePort', [
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
    translateService = jasmine.createSpyObj<TranslateService>('TranslateService', ['instant']);

    imageUploadSecurityService.filterValidImageFiles.and.callFake((files: File[]) => files);
    translateService.instant.and.callFake((key: string) => key);
    imagesPort.getAdminImageTags.and.returnValue(of([{ id: 'tag-gallery', slug: 'park-gallery', labels: [], descriptions: [], isActive: true, createdAt: '', updatedAt: '' }]));
    imagesPort.createAdminImageTag.and.callFake((request) => of({ id: `${request.slug}-tag`, slug: request.slug, labels: request.labels, descriptions: request.descriptions, isActive: true, createdAt: '', updatedAt: '' }));
    imagesPort.linkImage.and.returnValue(of(createImageDto()));
    imagesPort.updateAdminImage.and.callFake((imageId, request) => of(createImageDto({ id: imageId, geoLocation: request.geoLocation })));

    TestBed.configureTestingModule({
      providers: [
        AdminParkPhotosStateFacade,
        { provide: ADMIN_PARK_PHOTOS_STATE_IMAGES_API_SERVICE_PORT, useValue: imagesPort },
        { provide: ToastMessageService, useValue: toastMessageService },
        { provide: ImageUploadSecurityService, useValue: imageUploadSecurityService },
        { provide: TranslateService, useValue: translateService },
        { provide: DestroyRef, useValue: { onDestroy: jasmine.createSpy('onDestroy') } }
      ]
    });

    facade = TestBed.inject(AdminParkPhotosStateFacade);
  });

  it('stores uploaded EXIF coordinates on park photos', async () => {
    imagesPort.uploadImage.and.returnValue(of({ id: 'uploaded-1', latitude: 50.1, longitude: 3.2 }));

    facade.selectPhotoFiles(createFileInputEvent(new File(['image'], 'photo.jpg', { type: 'image/jpeg' })));
    await facade.uploadSelectedPhotos('park-1', 'Park');

    expect(imagesPort.updateAdminImage).toHaveBeenCalledWith('image-1', jasmine.objectContaining({
      geoLocation: { latitude: 50.1, longitude: 3.2 },
      tagIds: ['tag-gallery']
    }));
    expect(toastMessageService.add.calls.allArgs().some((args: unknown[]) => args[0] === 'warn')).toBeFalse();
  });

  it('warns without blocking when a park photo has no GPS coordinates', async () => {
    imagesPort.uploadImage.and.returnValue(of({ id: 'uploaded-1' }));

    facade.selectPhotoFiles(createFileInputEvent(new File(['image'], 'photo.jpg', { type: 'image/jpeg' })));
    await facade.uploadSelectedPhotos('park-1', 'Park');

    expect(imagesPort.linkImage).toHaveBeenCalled();
    expect(imagesPort.updateAdminImage).toHaveBeenCalledWith('image-1', jasmine.objectContaining({ geoLocation: null }));
    expect(toastMessageService.add).toHaveBeenCalledWith(
      'warn',
      'common.warning',
      'admin.contextualBlocks.drawer.photoMetadataGeoMissing'
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
    ...partial
  };
}
