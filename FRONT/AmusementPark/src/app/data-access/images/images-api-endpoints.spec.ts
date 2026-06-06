import { IMAGES_API_ENDPOINTS } from './images-api-endpoints';

describe('IMAGES_API_ENDPOINTS', () => {
  it('builds owner image urls', () => {
    expect(IMAGES_API_ENDPOINTS.getImages('park', 'p1', 'logo')).toBe('images/park/p1/logo');
    expect(IMAGES_API_ENDPOINTS.getCurrentImage('park', 'p1', 'logo')).toBe('images/park/p1/logo/current');
  });

  it('builds image mutation urls', () => {
    expect(IMAGES_API_ENDPOINTS.setCurrentImage('img1')).toBe('images/img1/current');
    expect(IMAGES_API_ENDPOINTS.deleteImage('img1')).toBe('images/img1');
    expect(IMAGES_API_ENDPOINTS.updateAdminImage('img1')).toBe('images/img1/metadata');
  });

  it('exposes collection endpoints', () => {
    expect(IMAGES_API_ENDPOINTS.uploadImage).toBe('images');
    expect(IMAGES_API_ENDPOINTS.linkImage).toBe('images/links');
    expect(IMAGES_API_ENDPOINTS.getAdminImageTags).toBe('images/tags');
    expect(IMAGES_API_ENDPOINTS.createAdminImageTag).toBe('images/tags');
  });
});
