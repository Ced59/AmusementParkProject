export type AdminContextualBlockType =
  | 'park.hero'
  | 'park.description'
  | 'park.images'
  | 'park.location'
  | 'park.practical'
  | 'parkItem.description'
  | 'parkItem.images'
  | 'parkItem.location'
  | 'reference.manufacturer';

export type AdminContextualBlockEntityType = 'Park' | 'ParkItem' | 'AttractionManufacturer';

export type AdminContextualBlockCapability =
  | 'fullAdminEdit'
  | 'boundedJsonExport'
  | 'boundedJsonPreview'
  | 'boundedJsonApply'
  | 'contextualFormEdit'
  | 'contextualPhotoAdd'
  | 'parkGraphUpsertDraft'
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
  readonly parkGraphUpsertDraftJson?: string | null;
  readonly parkGraphUpsertFileName?: string | null;
  readonly parkGraphUpsertImportRoute?: string[] | null;
}
