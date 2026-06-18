import { Injectable } from '@angular/core';

export interface ImageUploadValidationResult {
  isValid: boolean;
  errorKey: string | null;
}

export const ACCEPTED_IMAGE_MIME_TYPES: readonly string[] = [
  'image/apng',
  'image/avif',
  'image/bmp',
  'image/jpeg',
  'image/png',
  'image/tiff',
  'image/vnd.microsoft.icon',
  'image/webp',
  'image/gif',
  'image/x-icon'
] as const;

export const IMAGE_UPLOAD_ACCEPT_ATTRIBUTE: string = ACCEPTED_IMAGE_MIME_TYPES.join(',');
export const DEFAULT_MAX_IMAGE_UPLOAD_SIZE_BYTES: number = 10 * 1024 * 1024;

@Injectable({
  providedIn: 'root'
})
export class ImageUploadSecurityService {
  validateImageFile(file: File | null | undefined, maxFileSizeBytes: number = DEFAULT_MAX_IMAGE_UPLOAD_SIZE_BYTES): ImageUploadValidationResult {
    if (!file) {
      return {
        isValid: false,
        errorKey: 'shared.imageUpload.noImageSelected'
      };
    }

    if (!ACCEPTED_IMAGE_MIME_TYPES.includes(file.type.toLowerCase())) {
      return {
        isValid: false,
        errorKey: 'shared.imageUpload.invalidFile'
      };
    }

    if (file.size > maxFileSizeBytes) {
      return {
        isValid: false,
        errorKey: 'shared.imageUpload.invalidSize'
      };
    }

    return {
      isValid: true,
      errorKey: null
    };
  }

  filterValidImageFiles(files: File[], maxFileSizeBytes: number = DEFAULT_MAX_IMAGE_UPLOAD_SIZE_BYTES): File[] {
    return files.filter((file: File) => this.validateImageFile(file, maxFileSizeBytes).isValid);
  }
}
