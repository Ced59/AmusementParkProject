export type AdminContextualBlockType =
  | 'park.hero'
  | 'park.description'
  | 'park.location'
  | 'park.practical'
  | 'parkItem.description'
  | 'parkItem.location';

export type AdminContextualBlockEntityType = 'Park' | 'ParkItem';

export type AdminContextualBlockCapability =
  | 'fullAdminEdit'
  | 'boundedJsonExport'
  | 'boundedJsonPreview'
  | 'boundedJsonApply'
  | 'contextualFormEdit'
  | 'targetedChildAdd'
  | 'boundedJsonExportPlanned'
  | 'boundedJsonUpsertPlanned'
  | 'formEditPlanned';

export interface AdminContextualBlockDefinition {
  readonly type: AdminContextualBlockType;
  readonly labelKey: string;
  readonly descriptionKey: string;
  readonly iconClass: string;
  readonly capabilities: readonly AdminContextualBlockCapability[];
  readonly jsonScope: readonly string[];
  readonly localizedLanguageCodes: readonly string[];
}

export interface AdminContextualBlockInstance extends AdminContextualBlockDefinition {
  readonly id: string;
  readonly entityType: AdminContextualBlockEntityType;
  readonly entityId: string;
  readonly contextLabel: string;
  readonly ids: Readonly<Record<string, string>>;
  readonly locationFallbackCenter: readonly [number, number] | null;
  readonly adminRoute: string[] | null;
}
