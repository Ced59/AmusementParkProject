export interface ContextualBlockExportTarget {
  readonly entityType: string;
  readonly entityId: string;
}

export interface ContextualBlockExportMetadata {
  readonly source: string;
  readonly exportedAtUtc: string;
}

export interface ContextualBlockLocalizedText {
  readonly languageCode: string;
  readonly value: string | null;
}

export interface ContextualParkDescriptionBlock {
  readonly parkId: string;
  readonly descriptions: readonly ContextualBlockLocalizedText[];
}

export interface ContextualParkLocationBlock {
  readonly parkId: string;
  readonly latitude: number | null;
  readonly longitude: number | null;
}

export interface ContextualParkItemDescriptionBlock {
  readonly parkId: string;
  readonly parkItemId: string;
  readonly zoneId: string | null;
  readonly descriptions: readonly ContextualBlockLocalizedText[];
}

export interface ContextualParkItemLocationBlock {
  readonly parkId: string;
  readonly parkItemId: string;
  readonly zoneId: string | null;
  readonly latitude: number | null;
  readonly longitude: number | null;
}

export type ContextualLocalizedDescriptionBlock = ContextualParkDescriptionBlock | ContextualParkItemDescriptionBlock;

export type ContextualLocationBlock = ContextualParkLocationBlock | ContextualParkItemLocationBlock;

export interface ContextualBlockExportDocument<TBlock = unknown> {
  readonly documentType: string;
  readonly schemaVersion: string;
  readonly blockType: string;
  readonly target: ContextualBlockExportTarget;
  readonly ids: Readonly<Record<string, string>>;
  readonly block: TBlock | null;
  readonly metadata: ContextualBlockExportMetadata;
}
