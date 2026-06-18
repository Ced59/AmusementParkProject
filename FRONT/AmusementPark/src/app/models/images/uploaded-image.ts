export interface UploadedImage {
  id: string;
  path?: string;
  sourceUrl?: string | null;
  // ... tout ce que renvoie ton /images/upload
}
