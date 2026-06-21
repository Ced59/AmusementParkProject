import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { ImageUploadSecurityService } from '@shared/utils/security';
import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import {
  AdminContextualPhotoMetadataPreview,
  AdminContextualPhotoMetadataReaderService
} from '../services/admin-contextual-photo-metadata-reader.service';
import { AdminContextualBlockRefreshEvents } from './admin-contextual-block-refresh-events.service';
import {
  ADMIN_CONTEXTUAL_BLOCK_PHOTO_ADD_IMAGES_PORT,
  AdminContextualBlockPhotoAddImagesPort
} from './admin-contextual-block-photo-add-data.ports';
import { AdminContextualBlockPhotoAddFacade } from './admin-contextual-block-photo-add.facade';

describe('AdminContextualBlockPhotoAddFacade', () => {
  let facade: AdminContextualBlockPhotoAddFacade;
  let imagesPort: jasmine.SpyObj<AdminContextualBlockPhotoAddImagesPort>;
  let metadataReader: jasmine.SpyObj<AdminContextualPhotoMetadataReaderService>;
  let refreshEvents: jasmine.SpyObj<AdminContextualBlockRefreshEvents>;
  let imageUploadSecurityService: jasmine.SpyObj<ImageUploadSecurityService>;

  beforeEach(() => {
    imagesPort = jasmine.createSpyObj<AdminContextualBlockPhotoAddImagesPort>('AdminContextualBlockPhotoAddImagesPort', [
      'uploadImage',
      'linkImage',
      'importRemoteImage',
      'getAdminImageTags',
      'createAdminImageTag',
      'updateAdminImage'
    ]);
    metadataReader = jasmine.createSpyObj<AdminContextualPhotoMetadataReaderService>('AdminContextualPhotoMetadataReaderService', ['readFile', 'readRemoteUrl']);
    refreshEvents = jasmine.createSpyObj<AdminContextualBlockRefreshEvents>('AdminContextualBlockRefreshEvents', ['notifyBlockApplied']);
    imageUploadSecurityService = jasmine.createSpyObj<ImageUploadSecurityService>('ImageUploadSecurityService', ['validateImageFile']);

    imagesPort.getAdminImageTags.and.returnValue(of(createTags()));
    imagesPort.createAdminImageTag.and.callFake((request) => of({
      id: `${request.slug}-tag`,
      slug: request.slug,
      labels: request.labels,
      descriptions: request.descriptions,
      isActive: true,
      createdAt: '2026-06-21T00:00:00Z',
      updatedAt: '2026-06-21T00:00:00Z'
    }));
    imageUploadSecurityService.validateImageFile.and.returnValue({ isValid: true, errorKey: null });
    metadataReader.readFile.and.returnValue(Promise.resolve(createMetadataPreview()));
    metadataReader.readRemoteUrl.and.returnValue(Promise.resolve({
      ...createMetadataPreview(),
      sourceKind: 'remote',
      fileName: null,
      contentType: null,
      sizeInBytes: null,
      geoLocation: null,
      geoStatus: 'unavailable'
    }));

    TestBed.configureTestingModule({
      providers: [
        AdminContextualBlockPhotoAddFacade,
        {
          provide: ADMIN_CONTEXTUAL_BLOCK_PHOTO_ADD_IMAGES_PORT,
          useValue: imagesPort
        },
        {
          provide: AdminContextualPhotoMetadataReaderService,
          useValue: metadataReader
        },
        {
          provide: ImageUploadSecurityService,
          useValue: imageUploadSecurityService
        },
        {
          provide: AdminContextualBlockRefreshEvents,
          useValue: refreshEvents
        },
        {
          provide: TranslateService,
          useValue: {
            currentLang: 'fr',
            instant: (key: string) => key
          }
        }
      ]
    });

    facade = TestBed.inject(AdminContextualBlockPhotoAddFacade);
    spyOn(URL, 'createObjectURL').and.returnValue('blob:preview');
    spyOn(URL, 'revokeObjectURL');
  });

  it('uploads a selected park photo with category, tags, metadata preview and refresh notification', async () => {
    const block: AdminContextualBlockInstance = createParkImagesBlock();
    const file: File = new File(['image'], 'entrance.jpg', { type: 'image/jpeg' });
    const linkedImage: ImageDto = createImageDto({
      id: 'image-1',
      tagIds: ['existing-tag']
    });
    const updatedImage: ImageDto = createImageDto({
      id: 'image-1',
      tagIds: ['existing-tag', 'park-gallery-tag', 'extra-tag']
    });
    imagesPort.uploadImage.and.returnValue(of({ id: 'uploaded-1' }));
    imagesPort.linkImage.and.returnValue(of(linkedImage));
    imagesPort.updateAdminImage.and.returnValue(of(updatedImage));

    facade.resetForBlock(block);
    await flushPromises();
    facade.selectFile(file);
    await flushPromises();
    facade.updateDescription('Entrance view');
    facade.updateSelectedCategorySlug('park-gallery');
    facade.toggleTag('extra-tag', true);
    facade.updateSetAsCurrent(true);
    facade.uploadPhoto(block);
    await flushPromises();

    expect(facade.previewUrl()).toBeNull();
    expect(facade.successKey()).toBe('admin.contextualBlocks.drawer.photoUploadSucceeded');
    expect(facade.metadataRows().length).toBe(0);
    expect(imagesPort.uploadImage).toHaveBeenCalledOnceWith(file, ImageCategory.PARK, false, 'Entrance view');
    expect(imagesPort.linkImage).toHaveBeenCalledOnceWith({
      imageId: 'uploaded-1',
      ownerType: ImageOwnerType.PARK,
      ownerId: 'park-1',
      description: 'Entrance view',
      setAsCurrent: true
    });
    expect(imagesPort.updateAdminImage).toHaveBeenCalledOnceWith('image-1', jasmine.objectContaining({
      tagIds: ['existing-tag', 'park-gallery-tag', 'extra-tag'],
      isPublished: true
    }));
    expect(refreshEvents.notifyBlockApplied).toHaveBeenCalledOnceWith(jasmine.objectContaining({
      blockType: 'park.images',
      entityType: 'Park',
      entityId: 'park-1'
    }));
  });

  it('imports a remote park item photo through the selected contextual block', async () => {
    const block: AdminContextualBlockInstance = createParkItemImagesBlock();
    const importedImage: ImageDto = createImageDto({
      id: 'image-2',
      category: ImageCategory.PARK_ITEM,
      ownerType: ImageOwnerType.PARK_ITEM,
      ownerId: 'item-1'
    });
    imagesPort.importRemoteImage.and.returnValue(of(importedImage));
    imagesPort.updateAdminImage.and.returnValue(of(importedImage));

    facade.resetForBlock(block);
    await flushPromises();
    facade.updateRemoteSourceUrl('https://example.test/photo.webp');
    facade.previewRemoteSourceUrl();
    await flushPromises();
    facade.updateDescription('Queue line');
    facade.updateIsPublished(false);
    facade.uploadPhoto(block);
    await flushPromises();

    expect(imagesPort.importRemoteImage).toHaveBeenCalledOnceWith({
      sourceUrl: 'https://example.test/photo.webp',
      category: ImageCategory.PARK_ITEM,
      ownerType: ImageOwnerType.PARK_ITEM,
      ownerId: 'item-1',
      description: 'Queue line',
      withWatermark: false,
      setAsCurrent: false
    });
    expect(imagesPort.updateAdminImage).toHaveBeenCalledOnceWith('image-2', jasmine.objectContaining({
      isPublished: false
    }));
    expect(refreshEvents.notifyBlockApplied).toHaveBeenCalledOnceWith(jasmine.objectContaining({
      blockType: 'parkItem.images',
      entityType: 'ParkItem',
      entityId: 'item-1'
    }));
  });
});

function flushPromises(): Promise<void> {
  return new Promise((resolve: () => void): void => {
    setTimeout(resolve, 0);
  });
}

function createTags(): ImageTagDto[] {
  return [
    {
      id: 'park-gallery-tag',
      slug: 'park-gallery',
      labels: [{ languageCode: 'fr', value: 'Galerie' }],
      descriptions: [],
      isActive: true,
      createdAt: '2026-06-21T00:00:00Z',
      updatedAt: '2026-06-21T00:00:00Z'
    },
    {
      id: 'park-item-gallery-tag',
      slug: 'park-item-gallery',
      labels: [{ languageCode: 'fr', value: 'Galerie item' }],
      descriptions: [],
      isActive: true,
      createdAt: '2026-06-21T00:00:00Z',
      updatedAt: '2026-06-21T00:00:00Z'
    },
    {
      id: 'extra-tag',
      slug: 'night',
      labels: [{ languageCode: 'fr', value: 'Nuit' }],
      descriptions: [],
      isActive: true,
      createdAt: '2026-06-21T00:00:00Z',
      updatedAt: '2026-06-21T00:00:00Z'
    }
  ];
}

function createMetadataPreview(): AdminContextualPhotoMetadataPreview {
  return {
    sourceKind: 'file',
    fileName: 'entrance.jpg',
    contentType: 'image/jpeg',
    sizeInBytes: 1200,
    width: 1024,
    height: 768,
    geoLocation: { latitude: 50.1, longitude: 3.2 },
    geoStatus: 'detected'
  };
}

function createImageDto(partial: Partial<ImageDto>): ImageDto {
  return {
    id: partial.id ?? 'image-1',
    category: partial.category ?? ImageCategory.PARK,
    ownerType: partial.ownerType ?? ImageOwnerType.PARK,
    ownerId: partial.ownerId ?? 'park-1',
    isCurrent: partial.isCurrent ?? false,
    isPublished: partial.isPublished ?? true,
    width: partial.width ?? 1024,
    height: partial.height ?? 768,
    sizeInBytes: partial.sizeInBytes ?? 1200,
    originalFileName: partial.originalFileName ?? 'entrance.jpg',
    contentType: partial.contentType ?? 'image/jpeg',
    sourceUrl: partial.sourceUrl ?? null,
    geoLocation: partial.geoLocation ?? null,
    exifMetadata: partial.exifMetadata ?? null,
    altTexts: partial.altTexts ?? [],
    captions: partial.captions ?? [],
    credits: partial.credits ?? [],
    tagIds: partial.tagIds ?? [],
    createdAt: partial.createdAt ?? '2026-06-21T00:00:00Z',
    updatedAt: partial.updatedAt ?? '2026-06-21T00:00:00Z'
  };
}

function createParkImagesBlock(): AdminContextualBlockInstance {
  return {
    id: 'park.images:park-1',
    type: 'park.images',
    entityType: 'Park',
    entityId: 'park-1',
    contextLabel: 'Phantasialand',
    ids: { parkId: 'park-1' },
    labelKey: 'admin.contextualBlocks.blocks.parkImages.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkImages.description',
    iconClass: 'pi pi-images',
    capabilities: ['fullAdminEdit', 'contextualPhotoAdd'],
    jsonScope: ['park.id', 'image.file'],
    localizedLanguageCodes: [],
    locationFallbackCenter: null,
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1']
  };
}

function createParkItemImagesBlock(): AdminContextualBlockInstance {
  return {
    id: 'parkItem.images:item-1',
    type: 'parkItem.images',
    entityType: 'ParkItem',
    entityId: 'item-1',
    contextLabel: 'Wakala',
    ids: { parkId: 'park-1', parkItemId: 'item-1' },
    labelKey: 'admin.contextualBlocks.blocks.parkItemImages.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkItemImages.description',
    iconClass: 'pi pi-images',
    capabilities: ['fullAdminEdit', 'contextualPhotoAdd'],
    jsonScope: ['parkItem.id', 'image.file'],
    localizedLanguageCodes: [],
    locationFallbackCenter: null,
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1', 'items', 'item-1']
  };
}
