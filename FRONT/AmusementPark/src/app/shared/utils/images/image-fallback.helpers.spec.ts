import { resolveImageFallbackIconClass } from './image-fallback.helpers';

describe('resolveImageFallbackIconClass', () => {
  it('returns dedicated icons for known fallback kinds', () => {
    expect(resolveImageFallbackIconClass('park')).toBe('pi pi-map-marker');
    expect(resolveImageFallbackIconClass('parkItem')).toBe('pi pi-star');
    expect(resolveImageFallbackIconClass('map')).toBe('pi pi-map');
  });

  it('falls back to the generic image icon for photo, generic and nullish values', () => {
    expect(resolveImageFallbackIconClass('photo')).toBe('pi pi-image');
    expect(resolveImageFallbackIconClass('generic')).toBe('pi pi-image');
    expect(resolveImageFallbackIconClass(null)).toBe('pi pi-image');
    expect(resolveImageFallbackIconClass(undefined)).toBe('pi pi-image');
  });
});
