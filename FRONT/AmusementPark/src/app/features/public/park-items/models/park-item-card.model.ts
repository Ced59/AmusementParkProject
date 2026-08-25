export interface ParkItemCardViewModel {
  id: string | null;
  name: string;
  subtitle: string | null;
  description: string | null;
  categoryLabelKey: string;
  typeLabelKey: string;
  typeIconClass: string;
  zoneName: string | null;
  imageUrl: string | null;
  imageSrcSet: string | null;
  lifecycleStatus: ParkItemCardLifecycleStatusViewModel | null;
  highlights: string[];
  itemLink: string[] | null;
}

export interface ParkItemCardLifecycleStatusViewModel {
  labelKey: string | null;
  label: string | null;
  tone: 'sky' | 'rose' | 'gold' | 'soft';
  iconClass: string;
}
