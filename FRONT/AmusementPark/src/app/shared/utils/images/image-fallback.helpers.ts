export type ImageFallbackKind = 'generic' | 'park' | 'parkItem' | 'photo' | 'map';

export function resolveImageFallbackIconClass(kind: ImageFallbackKind | null | undefined): string {
  switch (kind) {
    case 'park':
      return 'pi pi-map-marker';
    case 'parkItem':
      return 'pi pi-star';
    case 'map':
      return 'pi pi-map';
    case 'photo':
    case 'generic':
    default:
      return 'pi pi-image';
  }
}
