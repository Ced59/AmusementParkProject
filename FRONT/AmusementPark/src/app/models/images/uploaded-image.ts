export interface UploadedImage {
  id: string;
  path?: string;
  sourceUrl?: string | null;
  category?: string;
  latitude?: number | null;
  longitude?: number | null;
  width?: number;
  height?: number;
  sizeInBytes?: number;
  isWatermarked?: boolean;
  savedListFile?: string[];
}
