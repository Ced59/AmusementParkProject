export interface PublicHtmlSitemapNode {
  readonly id: string;
  readonly label: string;
  readonly relativeUrl: string | null;
  readonly hasChildren: boolean;
  readonly children?: readonly PublicHtmlSitemapNode[];
}
