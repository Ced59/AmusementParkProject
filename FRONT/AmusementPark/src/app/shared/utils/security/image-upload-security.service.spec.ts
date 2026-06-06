import {
  ACCEPTED_IMAGE_MIME_TYPES,
  DEFAULT_MAX_IMAGE_UPLOAD_SIZE_BYTES,
  IMAGE_UPLOAD_ACCEPT_ATTRIBUTE,
  ImageUploadSecurityService
} from './image-upload-security.service';

describe('ImageUploadSecurityService', () => {
  let service: ImageUploadSecurityService;

  beforeEach(() => {
    service = new ImageUploadSecurityService();
  });

  function createFile(name: string, type: string, size: number): File {
    const content: ArrayBuffer = new ArrayBuffer(size);
    return new File([content], name, { type });
  }

  it('exposes an accept attribute matching accepted mime types', () => {
    expect(IMAGE_UPLOAD_ACCEPT_ATTRIBUTE).toBe(ACCEPTED_IMAGE_MIME_TYPES.join(','));
  });

  it('rejects missing files', () => {
    expect(service.validateImageFile(null)).toEqual({ isValid: false, errorKey: 'shared.imageUpload.noImageSelected' });
  });

  it('rejects unsupported mime types case-insensitively', () => {
    const file: File = createFile('document.pdf', 'application/pdf', 10);

    expect(service.validateImageFile(file)).toEqual({ isValid: false, errorKey: 'shared.imageUpload.invalidFile' });
  });

  it('accepts supported mime types regardless of case', () => {
    const file: File = createFile('photo.jpg', 'IMAGE/JPEG', 10);

    expect(service.validateImageFile(file)).toEqual({ isValid: true, errorKey: null });
  });

  it('rejects files bigger than the configured limit and accepts files exactly at the limit', () => {
    expect(service.validateImageFile(createFile('too-big.png', 'image/png', 11), 10).errorKey).toBe('shared.imageUpload.invalidSize');
    expect(service.validateImageFile(createFile('exact.png', 'image/png', 10), 10).isValid).toBeTrue();
    expect(DEFAULT_MAX_IMAGE_UPLOAD_SIZE_BYTES).toBe(10 * 1024 * 1024);
  });

  it('filters only valid image files', () => {
    const files: File[] = [
      createFile('ok.webp', 'image/webp', 5),
      createFile('no.txt', 'text/plain', 5),
      createFile('large.gif', 'image/gif', 15)
    ];

    expect(service.filterValidImageFiles(files, 10).map((file: File): string => file.name)).toEqual(['ok.webp']);
  });
});
