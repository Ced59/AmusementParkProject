import { ImageDto } from '@app/models/images/image-dto';
import { ImageGeoLocation } from '@app/models/images/image-geo-location';
import { AdminParkPhotoCategoryOption } from '@features/admin/parks/models/admin-park-edit.model';
import { AdminParkItemPhotoCategoryOption } from '@features/admin/park-items/models/admin-park-item-edit.model';

export type AdminPhotoBatchOwnerKind = 'park' | 'parkItem';
export type AdminPhotoBatchSection = 'uncategorized' | 'park' | 'parkItem';
export type AdminPhotoBatchMetadataStatus = 'pending' | 'ready' | 'failed';

export interface AdminPhotoBatchParkOption {
  id: string;
  name: string;
}

export interface AdminPhotoBatchParkItemOption {
  id: string;
  name: string;
}

export interface AdminPhotoBatchUploadSelection {
  id: string;
  file: File;
  previewUrl: string;
  metadataStatus: AdminPhotoBatchMetadataStatus;
  fileName: string;
  contentType: string | null;
  sizeInBytes: number;
  width: number | null;
  height: number | null;
  geoLocation: ImageGeoLocation | null;
}

export interface AdminPhotoBatchUploadProgress {
  completed: number;
  total: number;
  activeIndex: number;
  currentFileName: string | null;
}

export interface AdminPhotoBatchPhoto {
  id: string;
  image: ImageDto;
  section: AdminPhotoBatchSection;
  categorySlug: string | null;
  categoryLabelKey: string | null;
  parkItemId: string | null;
  parkItemName: string | null;
  draftOwnerKind: AdminPhotoBatchOwnerKind;
  draftParkItemId: string | null;
  draftCategorySlug: string;
  isSaving: boolean;
}

export interface AdminPhotoBatchCategorySets {
  park: readonly AdminParkPhotoCategoryOption[];
  parkItem: readonly AdminParkItemPhotoCategoryOption[];
}
