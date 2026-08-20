export const MINIMUM_INDEXABLE_IMAGE_GALLERY_ENTRIES: number = 3;
export const MINIMUM_INDEXABLE_COLLECTION_ENTRIES: number = 2;

export function isImageGalleryIndexable(entryCount: number): boolean {
  return entryCount >= MINIMUM_INDEXABLE_IMAGE_GALLERY_ENTRIES;
}

export function isCollectionIndexable(entryCount: number): boolean {
  return entryCount >= MINIMUM_INDEXABLE_COLLECTION_ENTRIES;
}
