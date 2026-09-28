export const HISTORICAL_LANGUAGE_CODES: readonly string[] = ['fr', 'en', 'de', 'nl', 'it', 'es', 'pl', 'pt'];

export const HISTORICAL_FACT_TYPES: readonly string[] = [
  'Opening', 'Closure', 'Reopening', 'Renaming', 'OperatorChange', 'OwnerChange',
  'Extension', 'Reduction', 'ZoneCreation', 'ZoneRenaming', 'ZoneRemoval', 'MajorEvent',
  'PositioningChange', 'Announcement', 'Construction', 'TemporaryClosure',
  'DefinitiveClosure', 'Dismantling', 'Relocation', 'Retheming', 'ManufacturerChange',
  'TechnicalModification', 'Replacement', 'ZoneMove', 'LogoChange', 'Other'
];

export const HISTORICAL_RELATION_TYPES: readonly string[] = [
  'RenamedTo', 'ReplacedBy', 'MovedTo', 'RethemedAs', 'SuccessorOf',
  'SamePhysicalAssetAs', 'SharesLocationWith', 'OperatedByDuring', 'LocatedInZoneDuring'
];

export const HISTORICAL_SOURCE_TYPES: readonly string[] = [
  'OfficialWebsite', 'OfficialPublication', 'Press', 'Book', 'Academic', 'Archive', 'Database', 'Other'
];

export const HISTORICAL_SOURCE_SCOPES: readonly string[] = [
  'SubjectIdentity', 'FactType', 'Period', 'StructuredValue', 'HistoricalLabel',
  'Narrative', 'SequenceWithinDate', 'RelationSourceIdentity', 'RelationTargetIdentity', 'RelationType'
];

export const HISTORICAL_WORKFLOW_STAGES: readonly string[] = [
  'Draft', 'SourcesAttached', 'EditorialReview', 'StructuredValidation', 'Published'
];
