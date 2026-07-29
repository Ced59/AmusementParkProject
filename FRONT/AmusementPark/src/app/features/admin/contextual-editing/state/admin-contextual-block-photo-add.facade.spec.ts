import type { MockedObject } from 'vitest';
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
  AdminContextualPhotoMetadataReaderService,
} from '../services/admin-contextual-photo-metadata-reader.service';
import { AdminContextualBlockRefreshEvents } from './admin-contextual-block-refresh-events.service';
import {
  ADMIN_CONTEXTUAL_BLOCK_PHOTO_ADD_IMAGES_PORT,
  AdminContextualBlockPhotoAddImagesPort,
} from './admin-contextual-block-photo-add-data.ports';
import { AdminContextualBlockPhotoAddFacade } from './admin-contextual-block-photo-add.facade';

describe('AdminContextualBlockPhotoAddFacade', () => {
  let facade: AdminContextualBlockPhotoAddFacade;
  let imagesPort: MockedObject<AdminContextualBlockPhotoAddImagesPort>;
  let metadataReader: MockedObject<AdminContextualPhotoMetadataReaderService>;
  let refreshEvents: MockedObject<AdminContextualBlockRefreshEvents>;
  let imageUploadSecurityService: MockedObject<ImageUploadSecurityService>;

  beforeEach(() => {
    imagesPort = {
      uploadImage: vi
        .fn()
        .mockName('AdminContextualBlockPhotoAddImagesPort.uploadImage'),
      linkImage: vi
        .fn()
        .mockName('AdminContextualBlockPhotoAddImagesPort.linkImage'),
      importRemoteImage: vi
        .fn()
        .mockName('AdminContextualBlockPhotoAddImagesPort.importRemoteImage'),
      getAdminImageTags: vi
        .fn()
        .mockName('AdminContextualBlockPhotoAddImagesPort.getAdminImageTags'),
      createAdminImageTag: vi
        .fn()
        .mockName('AdminContextualBlockPhotoAddImagesPort.createAdminImageTag'),
      updateAdminImage: vi
        .fn()
        .mockName('AdminContextualBlockPhotoAddImagesPort.updateAdminImage'),
    } as unknown as MockedObject<AdminContextualBlockPhotoAddImagesPort>;
    metadataReader = {
      readFile: vi
        .fn()
        .mockName('AdminContextualPhotoMetadataReaderService.readFile'),
      readRemoteUrl: vi
        .fn()
        .mockName('AdminContextualPhotoMetadataReaderService.readRemoteUrl'),
    } as unknown as MockedObject<AdminContextualPhotoMetadataReaderService>;
    refreshEvents = {
      notifyBlockApplied: vi
        .fn()
        .mockName('AdminContextualBlockRefreshEvents.notifyBlockApplied'),
    } as unknown as MockedObject<AdminContextualBlockRefreshEvents>;
    imageUploadSecurityService = {
      validateImageFile: vi
        .fn()
        .mockName('ImageUploadSecurityService.validateImageFile'),
    } as unknown as MockedObject<ImageUploadSecurityService>;

    imagesPort.getAdminImageTags.mockReturnValue(of(createTags()));
    imagesPort.createAdminImageTag.mockImplementation((request) =>
      of({
        id: `${request.slug}-tag`,
        slug: request.slug,
        labels: request.labels,
        descriptions: request.descriptions,
        isActive: true,
        createdAt: '2026-06-21T00:00:00Z',
        updatedAt: '2026-06-21T00:00:00Z',
      }),
    );
    imageUploadSecurityService.validateImageFile.mockReturnValue({
      isValid: true,
      errorKey: null,
    });
    metadataReader.readFile.mockReturnValue(
      Promise.resolve(createMetadataPreview()),
    );
    metadataReader.readRemoteUrl.mockReturnValue(
      Promise.resolve({
        ...createMetadataPreview(),
        sourceKind: 'remote',
        fileName: null,
        contentType: null,
        sizeInBytes: null,
        geoLocation: null,
        geoStatus: 'unavailable',
      }),
    );

    TestBed.configureTestingModule({
      providers: [
        AdminContextualBlockPhotoAddFacade,
        {
          provide: ADMIN_CONTEXTUAL_BLOCK_PHOTO_ADD_IMAGES_PORT,
          useValue: imagesPort,
        },
        {
          provide: AdminContextualPhotoMetadataReaderService,
          useValue: metadataReader,
        },
        {
          provide: ImageUploadSecurityService,
          useValue: imageUploadSecurityService,
        },
        {
          provide: AdminContextualBlockRefreshEvents,
          useValue: refreshEvents,
        },
        {
          provide: TranslateService,
          useValue: {
            currentLang: 'fr',
            instant: (key: string) => key,
          },
        },
      ],
    });

    facade = TestBed.inject(AdminContextualBlockPhotoAddFacade);
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:preview');
    vi.spyOn(URL, 'revokeObjectURL');
  });

  it('uploads a selected park photo with category, tags, metadata preview and refresh notification', async () => {
    const block: AdminContextualBlockInstance = createParkImagesBlock();
    const file: File = new File(['image'], 'entrance.jpg', {
      type: 'image/jpeg',
    });
    const linkedImage: ImageDto = createImageDto({
      id: 'image-1',
      tagIds: ['existing-tag'],
    });
    const updatedImage: ImageDto = createImageDto({
      id: 'image-1',
      tagIds: ['existing-tag', 'park-gallery-tag', 'extra-tag'],
    });
    imagesPort.uploadImage.mockReturnValue(of({ id: 'uploaded-1' }));
    imagesPort.linkImage.mockReturnValue(of(linkedImage));
    imagesPort.updateAdminImage.mockReturnValue(of(updatedImage));

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
    expect(facade.successKey()).toBe(
      'admin.contextualBlocks.drawer.photoUploadSucceeded',
    );
    expect(facade.metadataRows().length).toBe(0);
    expect(imagesPort.uploadImage).toHaveBeenCalledTimes(1);
    expect(imagesPort.uploadImage).toHaveBeenCalledWith(
      file,
      ImageCategory.PARK,
      true,
      'Entrance view',
    );
    expect(imagesPort.linkImage).toHaveBeenCalledTimes(1);
    expect(imagesPort.linkImage).toHaveBeenCalledWith({
      imageId: 'uploaded-1',
      ownerType: ImageOwnerType.PARK,
      ownerId: 'park-1',
      description: 'Entrance view',
      setAsCurrent: true,
    });
    expect(imagesPort.updateAdminImage).toHaveBeenCalledTimes(1);
    expect(imagesPort.updateAdminImage).toHaveBeenCalledWith(
      'image-1',
      expect.objectContaining({
        tagIds: ['existing-tag', 'park-gallery-tag', 'extra-tag'],
        isPublished: true,
      }),
    );
    expect(refreshEvents.notifyBlockApplied).toHaveBeenCalledTimes(1);
    expect(refreshEvents.notifyBlockApplied).toHaveBeenCalledWith(
      expect.objectContaining({
        blockType: 'park.images',
        entityType: 'Park',
        entityId: 'park-1',
      }),
    );
  });

  it('imports a remote park item photo through the selected contextual block', async () => {
    const block: AdminContextualBlockInstance = createParkItemImagesBlock();
    const importedImage: ImageDto = createImageDto({
      id: 'image-2',
      category: ImageCategory.PARK_ITEM,
      ownerType: ImageOwnerType.PARK_ITEM,
      ownerId: 'item-1',
    });
    imagesPort.importRemoteImage.mockReturnValue(of(importedImage));
    imagesPort.updateAdminImage.mockReturnValue(of(importedImage));

    facade.resetForBlock(block);
    await flushPromises();
    facade.updateRemoteSourceUrl('https://example.test/photo.webp');
    facade.previewRemoteSourceUrl();
    await flushPromises();
    facade.updateDescription('Queue line');
    facade.updateWithWatermark(false);
    facade.updateIsPublished(false);
    facade.uploadPhoto(block);
    await flushPromises();

    expect(imagesPort.importRemoteImage).toHaveBeenCalledTimes(1);

    expect(imagesPort.importRemoteImage).toHaveBeenCalledWith({
      sourceUrl: 'https://example.test/photo.webp',
      category: ImageCategory.PARK_ITEM,
      ownerType: ImageOwnerType.PARK_ITEM,
      ownerId: 'item-1',
      description: 'Queue line',
      withWatermark: false,
      setAsCurrent: false,
    });
    expect(imagesPort.updateAdminImage).toHaveBeenCalledTimes(1);
    expect(imagesPort.updateAdminImage).toHaveBeenCalledWith(
      'image-2',
      expect.objectContaining({
        isPublished: false,
      }),
    );
    expect(refreshEvents.notifyBlockApplied).toHaveBeenCalledTimes(1);
    expect(refreshEvents.notifyBlockApplied).toHaveBeenCalledWith(
      expect.objectContaining({
        blockType: 'parkItem.images',
        entityType: 'ParkItem',
        entityId: 'item-1',
      }),
    );
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
      updatedAt: '2026-06-21T00:00:00Z',
    },
    {
      id: 'park-item-gallery-tag',
      slug: 'park-item-gallery',
      labels: [{ languageCode: 'fr', value: 'Galerie item' }],
      descriptions: [],
      isActive: true,
      createdAt: '2026-06-21T00:00:00Z',
      updatedAt: '2026-06-21T00:00:00Z',
    },
    {
      id: 'extra-tag',
      slug: 'night',
      labels: [{ languageCode: 'fr', value: 'Nuit' }],
      descriptions: [],
      isActive: true,
      createdAt: '2026-06-21T00:00:00Z',
      updatedAt: '2026-06-21T00:00:00Z',
    },
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
    geoStatus: 'detected',
  };
}

function createImageDto(partial: Partial<ImageDto>): ImageDto {
  return {
    id: partial.id ?? 'image-1',
    category: partial.category ?? ImageCategory.PARK,
    ownerType: partial.ownerType ?? ImageOwnerType.PARK,
    ownerId: partial.ownerId ?? 'park-1',
    isCurrent: partial.isCurrent ?? false,
    isWatermarked: partial.isWatermarked ?? false,
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
    updatedAt: partial.updatedAt ?? '2026-06-21T00:00:00Z',
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
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1'],
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
    adminRoute: [
      '/',
      'fr',
      'admin',
      'parks',
      'edit',
      'park-1',
      'items',
      'item-1',
    ],
  };
}
