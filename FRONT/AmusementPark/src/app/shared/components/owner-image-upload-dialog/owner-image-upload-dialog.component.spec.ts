import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ImageDto } from '@app/models/images/image-dto';
import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { UsersApiService } from '@data-access/users/users-api.service';
import { OwnerImageUploadDialogComponent } from './owner-image-upload-dialog.component';

describe('OwnerImageUploadDialogComponent', () => {
  let component: OwnerImageUploadDialogComponent;
  let fixture: ComponentFixture<OwnerImageUploadDialogComponent>;
  let imagesApiService: ImagesApiService;
  let usersApiService: UsersApiService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, OwnerImageUploadDialogComponent],
      providers: provideCommonTestDependencies()
    }).compileComponents();

    fixture = TestBed.createComponent(OwnerImageUploadDialogComponent);
    component = fixture.componentInstance;
    imagesApiService = TestBed.inject(ImagesApiService);
    usersApiService = TestBed.inject(UsersApiService);
  });

  it('uses the dedicated current-user endpoint without trusting an owner id', () => {
    const file: File = new File(['avatar'], 'avatar.png', { type: 'image/png' });
    const image: ImageDto = createImage();
    const uploadCurrentUserAvatar = vi.spyOn(usersApiService, 'uploadCurrentUserAvatar').mockReturnValue(of(image));
    const genericUpload = vi.spyOn(imagesApiService, 'uploadImage');
    const uploaded = vi.fn();
    component.uploadMode = 'current-user-avatar';
    component.ownerId = '';
    component.selectedFile = file;
    component.uploaded.subscribe(uploaded);

    component.upload();

    expect(uploadCurrentUserAvatar).toHaveBeenCalledOnce();
    expect(uploadCurrentUserAvatar).toHaveBeenCalledWith(file);
    expect(genericUpload).not.toHaveBeenCalled();
    expect(uploaded).toHaveBeenCalledOnce();
    expect(uploaded).toHaveBeenCalledWith(image);
  });
});

function createImage(): ImageDto {
  return {
    id: 'avatar-1',
    category: ImageCategory.AVATAR,
    ownerType: ImageOwnerType.USER,
    ownerId: 'server-derived-user',
    isCurrent: true,
    isPublished: true,
    isWatermarked: false,
    width: 120,
    height: 120,
    sizeInBytes: 42,
    altTexts: [],
    captions: [],
    credits: [],
    tagIds: [],
    createdAt: '2026-07-29T00:00:00Z',
    updatedAt: '2026-07-29T00:00:00Z'
  };
}
