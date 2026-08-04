import { UiPrimitiveTone } from '@ui/primitives/models/ui-primitive-variant.model';

export interface UiSearchResultCardModel {
  title: string;
  description: string | null;
  logoImageId: string | null;
  iconClass: string;
  tone: UiPrimitiveTone;
  categoryLabelKey: string;
  statusLabelKey: string | null;
  statusIconClass: string | null;
  statusTone: UiPrimitiveTone | null;
  metaParts: string[];
  detailLink: string[] | null;
  actionLabelKey: string;
}
