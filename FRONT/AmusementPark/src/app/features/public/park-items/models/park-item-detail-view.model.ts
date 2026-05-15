export interface ParkItemDetailRowViewModel {
  labelKey: string;
  value: string;
}

export interface ParkItemDetailViewModel {
  name: string;
  categoryLabelKey: string;
  typeLabelKey: string;
  parkName: string | null;
  parkLink: string[] | null;
  itemsLink: string[] | null;
  description: string | null;
  manufacturerName: string | null;
  modelName: string | null;
  status: string | null;
  zoneName: string | null;
  sourceUrl: string | null;
  spotlightRows: ParkItemDetailRowViewModel[];
  secondaryRows: ParkItemDetailRowViewModel[];
}
