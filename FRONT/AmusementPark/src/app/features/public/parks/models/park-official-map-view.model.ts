import { ParkOfficialMapFormat } from '@app/models/parks/park-map-items';

export type ParkMapPageTab = 'interactive' | 'official';

export interface ParkOfficialMapViewModel {
  id: string;
  year: number;
  format: ParkOfficialMapFormat;
  documentUrl: string;
  previewImageUrl: string | null;
  sourcePageUrl: string | null;
  languageCode: string | null;
  title: string | null;
  alternativeText: string;
  originalFileName: string | null;
  fileSizeLabel: string | null;
  isImage: boolean;
  isPdf: boolean;
}
