import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { environment } from '../../../environments/environment';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ImagesApiService } from './images-api.service';

describe('ImagesApiService', () => {
  let service: ImagesApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(ImagesApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('uploads images through form data with mapped category and optional description', () => {
    const file: File = new File(['content'], 'park.png', { type: 'image/png' });

    service.uploadImage(file, ImageCategory.PARK_LOGO, false, 'Logo').subscribe();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}images`);
    expect(request.request.method).toBe('POST');
    const formData: FormData = request.request.body as FormData;
    expect(formData.get('File')).toBe(file);
    expect(formData.get('Category')).toBe('1');
    expect(formData.get('WithWatermark')).toBe('false');
    expect(formData.get('Description')).toBe('Logo');
    request.flush({ id: 'img-1' });
  });

  it('links images with mapped owner type', () => {
    service.linkImage({ imageId: 'img-1', ownerType: ImageOwnerType.PARK, ownerId: 'park-1', category: ImageCategory.PARK_LOGO } as never).subscribe();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}images/links`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body.ownerType).toBe(1);
    request.flush({ id: 'img-1' });
  });

  it('gets owner images with mapped owner and category params and unwraps paged responses', () => {
    service.getImages(ImageOwnerType.PARK, 'park-1', ImageCategory.PARK, 2, 20).subscribe((images) => {
      expect(images.length).toBe(1);
    });

    const request = httpTestingController.expectOne((candidate) => candidate.url === `${environment.apiBaseUrl}images/1/park-1/2`);
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('size')).toBe('20');
    request.flush({ data: [{ id: 'img-1' }] });
  });

  it('builds image urls from ids and normalizes supported image paths', () => {
    expect(service.buildImageUrl('img-1')).toBe(`${environment.imagesBaseUrl}/img-1`);
    expect(service.buildImageUrl('img-1', { width: 640 })).toBe(`${environment.imagesBaseUrl}/img-1?width=640&v=2`);
    expect(service.resolveImageUrl('/images/img-1')).toBe(`${environment.imagesBaseUrl}/img-1`);
    expect(service.resolveImageUrl('images/img-2')).toBe(`${environment.imagesBaseUrl}/img-2`);
    expect(service.resolveImageUrl('img-3')).toBe(`${environment.imagesBaseUrl}/img-3`);
    expect(service.resolveImageUrl('assets/img.png')).toBe(`${environment.apiBaseUrl}assets/img.png`);
  });

  it('builds responsive srcset entries only for API image ids', () => {
    expect(service.buildImageSrcSet('img-1', [640, 320, 640])).toBe(
      `${environment.imagesBaseUrl}/img-1?width=320&v=2 320w, ${environment.imagesBaseUrl}/img-1?width=640&v=2 640w`
    );
    expect(service.buildImageSrcSet('/images/img-2', [960])).toBe(`${environment.imagesBaseUrl}/img-2?width=960&v=2 960w`);
    expect(service.buildImageSrcSet('https://example.com/img.png', [640])).toBeNull();
    expect(service.buildImageSrcSet('assets/img.png', [640])).toBeNull();
  });

  it('uses finer default responsive widths for mobile image selection', () => {
    expect(service.buildImageSrcSet('img-1')).toBe(
      [
        `${environment.imagesBaseUrl}/img-1?width=320&v=2 320w`,
        `${environment.imagesBaseUrl}/img-1?width=480&v=2 480w`,
        `${environment.imagesBaseUrl}/img-1?width=640&v=2 640w`,
        `${environment.imagesBaseUrl}/img-1?width=800&v=2 800w`,
        `${environment.imagesBaseUrl}/img-1?width=960&v=2 960w`,
        `${environment.imagesBaseUrl}/img-1?width=1280&v=2 1280w`,
        `${environment.imagesBaseUrl}/img-1?width=1600&v=2 1600w`,
        `${environment.imagesBaseUrl}/img-1?width=1920&v=2 1920w`
      ].join(', ')
    );
  });

  it('returns null for empty or unsafe image paths and preserves safe absolute paths', () => {
    expect(service.resolveImageUrl('')).toBeNull();
    expect(service.resolveImageUrl('javascript:alert(1)')).toBeNull();
    expect(service.resolveImageUrl('https://example.com/img.png')).toBe('https://example.com/img.png');
  });

  it('builds a fallback avatar data URL when no avatar image can be resolved', () => {
    const fallback: string = service.resolveAvatarUrl(null);

    expect(fallback).toContain('data:image/svg+xml;utf8,');
    expect(decodeURIComponent(fallback)).toContain('<svg');
  });

  it('gets admin images with optional search params including false booleans', () => {
    service.getAdminImages({ page: 3, size: 10, search: ' park ', isPublished: false, hasOwner: true, hasGeoLocation: null }).subscribe();

    const request = httpTestingController.expectOne((candidate) => candidate.url === `${environment.apiBaseUrl}images`);
    expect(request.request.params.get('page')).toBe('3');
    expect(request.request.params.get('size')).toBe('10');
    expect(request.request.params.get('search')).toBe(' park ');
    expect(request.request.params.get('isPublished')).toBe('false');
    expect(request.request.params.get('hasOwner')).toBe('true');
    expect(request.request.params.has('hasGeoLocation')).toBeFalse();
    request.flush({ data: [] });
  });

  it('updates admin image metadata and image tags', () => {
    service.updateAdminImage('img-1', { altTexts: [], captions: [], credits: [], tagIds: [], isPublished: true }).subscribe();
    service.createAdminImageTag({ slug: 'tag', labels: [], descriptions: [] }).subscribe();
    service.updateAdminImageTag('tag-1', { slug: 'tag', labels: [], descriptions: [], isActive: true }).subscribe();

    const imageRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}images/img-1/metadata`);
    expect(imageRequest.request.method).toBe('PUT');
    imageRequest.flush({ id: 'img-1' });

    const createTagRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}images/tags`);
    expect(createTagRequest.request.method).toBe('POST');
    createTagRequest.flush({ id: 'tag-1' });

    const updateTagRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}images/tags/tag-1`);
    expect(updateTagRequest.request.method).toBe('PUT');
    updateTagRequest.flush({ id: 'tag-1' });
  });
});
