export class FakeImagesApiService {
  readonly buildImageUrlCalls: Array<{ imageId: string; width: number | undefined }> = [];

  buildImageUrl(imageId: string, options: { width?: number } = {}): string {
    this.buildImageUrlCalls.push({ imageId, width: options.width });
    return `/api/images/binary/${imageId}?width=${options.width ?? 0}`;
  }

  resolveImageUrl(imagePathOrUrl: string): string {
    return imagePathOrUrl;
  }

  buildImageSrcSet(): string | null {
    return null;
  }
}
