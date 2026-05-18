export type ParkReferenceKind = 'founder' | 'operator';

export interface ParkReferenceDetailViewModel {
  id: string | null;
  kind: ParkReferenceKind;
  name: string;
  richDescription: string | null;
  badgeKey: string;
  titleKey: string;
  descriptionTitleKey: string;
  emptyDescriptionKey: string;
}
