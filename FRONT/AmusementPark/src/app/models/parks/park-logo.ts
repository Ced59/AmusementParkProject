export interface ParkLogoDto {
  id: string;
  parkId: string;
  imageId: string;
  description?: string | null;
  isCurrent: boolean;
  createdAt: string; // ISO string
}
