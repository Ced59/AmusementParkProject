export interface HomeLatestArticleCardModel {
  id: string | null;
  title: string;
  description: string | null;
  contextLabel: string | null;
  mainImageId: string | null;
  detailLink: string[] | null;
  tone: 'primary' | 'sky' | 'purple';
}
