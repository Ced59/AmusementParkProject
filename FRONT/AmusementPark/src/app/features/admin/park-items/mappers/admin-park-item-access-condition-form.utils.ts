import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';

import { LANGUAGES } from '@shared/models/localization';
import { AttractionAccessCondition } from '@app/models/parks/attraction-access-condition';
import { AttractionAccessConditionType } from '@app/models/parks/attraction-access-condition-type';
import { AttractionAccessConditionUnit } from '@app/models/parks/attraction-access-condition-unit';
import { LocalizedItem } from '@app/models/shared/localized-item';


export type AdminParkItemHeightRequirementKey = 'minHeightCm' | 'minHeightAccompaniedCm' | 'maxHeightCm';

export interface AdminParkItemHeightRequirementField {
  key: AdminParkItemHeightRequirementKey;
  conditionType: AttractionAccessConditionType;
  labelKey: string;
  helpKey: string;
  iconClass: string;
}

export interface AdminParkItemAccessConditionEntry {
  index: number;
  group: FormGroup;
}

export const ADMIN_PARK_ITEM_HEIGHT_REQUIREMENT_FIELDS: AdminParkItemHeightRequirementField[] = [
  {
    key: 'minHeightCm',
    conditionType: 'MinHeight',
    labelKey: 'admin.parks.items.heightRequirements.fields.minHeightCm.label',
    helpKey: 'admin.parks.items.heightRequirements.fields.minHeightCm.help',
    iconClass: 'pi pi-user'
  },
  {
    key: 'minHeightAccompaniedCm',
    conditionType: 'MinHeightAccompanied',
    labelKey: 'admin.parks.items.heightRequirements.fields.minHeightAccompaniedCm.label',
    helpKey: 'admin.parks.items.heightRequirements.fields.minHeightAccompaniedCm.help',
    iconClass: 'pi pi-users'
  },
  {
    key: 'maxHeightCm',
    conditionType: 'MaxHeight',
    labelKey: 'admin.parks.items.heightRequirements.fields.maxHeightCm.label',
    helpKey: 'admin.parks.items.heightRequirements.fields.maxHeightCm.help',
    iconClass: 'pi pi-ban'
  }
];

const ACCESS_CONDITION_DEFAULT_LABELS: Record<AttractionAccessConditionType, Record<string, string>> = {
  MinHeight: {
    en: 'Minimum height',
    fr: 'Taille minimale',
    es: 'Altura mínima',
    de: 'Mindestgröße',
    it: 'Altezza minima',
    pl: 'Minimalny wzrost',
    nl: 'Minimumlengte',
    pt: 'Altura mínima'
  },
  MinHeightAccompanied: {
    en: 'Minimum height with accompaniment',
    fr: 'Taille minimale accompagné',
    es: 'Altura mínima acompañado',
    de: 'Mindestgröße in Begleitung',
    it: 'Altezza minima con accompagnatore',
    pl: 'Minimalny wzrost z opiekunem',
    nl: 'Minimumlengte met begeleiding',
    pt: 'Altura mínima acompanhado'
  },
  MaxHeight: {
    en: 'Maximum height',
    fr: 'Taille maximale',
    es: 'Altura máxima',
    de: 'Maximalgröße',
    it: 'Altezza massima',
    pl: 'Maksymalny wzrost',
    nl: 'Maximumlengte',
    pt: 'Altura máxima'
  },
  MinAge: {
    en: 'Minimum age',
    fr: 'Âge minimum',
    es: 'Edad mínima',
    de: 'Mindestalter',
    it: 'Età minima',
    pl: 'Minimalny wiek',
    nl: 'Minimumleeftijd',
    pt: 'Idade mínima'
  },
  MinAgeAccompanied: {
    en: 'Minimum age with accompaniment',
    fr: 'Âge minimum accompagné',
    es: 'Edad mínima acompañado',
    de: 'Mindestalter in Begleitung',
    it: 'Età minima con accompagnatore',
    pl: 'Minimalny wiek z opiekunem',
    nl: 'Minimumleeftijd met begeleiding',
    pt: 'Idade mínima acompanhado'
  },
  PregnancyRestriction: {
    en: 'Pregnancy restriction',
    fr: 'Restriction grossesse',
    es: 'Restricción embarazo',
    de: 'Einschränkung Schwangerschaft',
    it: 'Restrizione gravidanza',
    pl: 'Ograniczenie ciąży',
    nl: 'Zwangerschapsbeperking',
    pt: 'Restrição gravidez'
  },
  HeartRestriction: {
    en: 'Heart condition restriction',
    fr: 'Restriction cardiaque',
    es: 'Restricción cardíaca',
    de: 'Einschränkung Herzprobleme',
    it: 'Restrizione cardiaca',
    pl: 'Ograniczenie choroby serca',
    nl: 'Hartbeperking',
    pt: 'Restrição cardíaca'
  },
  BackNeckRestriction: {
    en: 'Back or neck restriction',
    fr: 'Restriction dos / cou',
    es: 'Restricción espalda / cuello',
    de: 'Einschränkung Rücken / Nacken',
    it: 'Restrizione schiena / collo',
    pl: 'Ograniczenie plecy / szyja',
    nl: 'Rug / nek beperking',
    pt: 'Restrição costas / pescoço'
  },
  WheelchairTransferRequired: {
    en: 'Wheelchair transfer required',
    fr: 'Transfert fauteuil requis',
    es: 'Transferencia de silla requerida',
    de: 'Rollstuhltransfer erforderlich',
    it: 'Trasferimento carrozzina richiesto',
    pl: 'Wymagany transfer z wózka',
    nl: 'Rolstoeltransfer vereist',
    pt: 'Transferência de cadeira necessária'
  },
  AccessPassRequired: {
    en: 'Access pass required',
    fr: 'Access pass requis',
    es: 'Access pass requerido',
    de: 'Access Pass erforderlich',
    it: 'Access pass richiesto',
    pl: 'Wymagany access pass',
    nl: 'Access pass vereist',
    pt: 'Access pass obrigatório'
  },
  Custom: {
    en: '',
    fr: '',
    es: '',
    de: '',
    it: '',
    pl: '',
    nl: '',
    pt: ''
  }
};

export function createAdminParkItemAccessConditionGroup(
  formBuilder: FormBuilder,
  condition?: AttractionAccessCondition
): FormGroup {
  return formBuilder.group({
    type: [condition?.type ?? 'Custom', Validators.required],
    isCustom: [condition?.isCustom ?? (condition?.type === 'Custom')],
    value: [condition?.value ?? null],
    unit: [condition?.unit ?? null],
    requiresAccompaniment: [condition?.requiresAccompaniment ?? false],
    minimumCompanionAge: [condition?.minimumCompanionAge ?? null],
    label: [condition?.label ?? []],
    description: [condition?.description ?? []],
    displayOrder: [condition?.displayOrder ?? null]
  });
}

export function setAdminParkItemAccessConditions(
  formBuilder: FormBuilder,
  accessConditions: FormArray,
  conditions: AttractionAccessCondition[] | null | undefined
): void {
  while (accessConditions.length > 0) {
    accessConditions.removeAt(0);
  }

  for (const condition of conditions ?? []) {
    accessConditions.push(createAdminParkItemAccessConditionGroup(formBuilder, condition));
  }

  syncAdminParkItemAccessConditionDisplayOrders(accessConditions);
}

export function addAdminParkItemAccessCondition(
  formBuilder: FormBuilder,
  accessConditions: FormArray,
  type: AttractionAccessConditionType
): void {
  const condition: AttractionAccessCondition = buildAdminParkItemDefaultAccessCondition(type, accessConditions.length + 1);
  accessConditions.push(createAdminParkItemAccessConditionGroup(formBuilder, condition));
  syncAdminParkItemAccessConditionDisplayOrders(accessConditions);
}

export function removeAdminParkItemAccessCondition(accessConditions: FormArray, index: number): void {
  accessConditions.removeAt(index);
  syncAdminParkItemAccessConditionDisplayOrders(accessConditions);
}

export function moveAdminParkItemAccessConditionUp(accessConditions: FormArray, index: number): void {
  if (index <= 0) {
    return;
  }

  const control = accessConditions.at(index);
  accessConditions.removeAt(index);
  accessConditions.insert(index - 1, control);
  syncAdminParkItemAccessConditionDisplayOrders(accessConditions);
}

export function moveAdminParkItemAccessConditionDown(accessConditions: FormArray, index: number): void {
  if (index >= accessConditions.length - 1) {
    return;
  }

  const control = accessConditions.at(index);
  accessConditions.removeAt(index);
  accessConditions.insert(index + 1, control);
  syncAdminParkItemAccessConditionDisplayOrders(accessConditions);
}

export function updateAdminParkItemAccessConditionType(accessConditions: FormArray, index: number): void {
  const group: FormGroup = accessConditions.at(index) as FormGroup;
  const type: AttractionAccessConditionType = group.get('type')?.value as AttractionAccessConditionType;
  const defaultCondition: AttractionAccessCondition = buildAdminParkItemDefaultAccessCondition(type, index + 1);
  const currentLabel: LocalizedItem<string>[] | null = toLocalizedItems(group.get('label')?.value);
  const shouldReplaceLabel: boolean = !hasLocalizedValues(currentLabel);

  group.patchValue({
    isCustom: type === 'Custom',
    unit: defaultCondition.unit ?? null,
    requiresAccompaniment: defaultCondition.requiresAccompaniment ?? false,
    minimumCompanionAge: defaultCondition.minimumCompanionAge ?? null
  });

  if (shouldReplaceLabel) {
    group.get('label')?.setValue(defaultCondition.label ?? []);
  }
}

export function getAdminParkItemAccessConditionLabelKey(type: AttractionAccessConditionType): string {
  switch (type) {
    case 'MinHeight':
      return 'admin.parks.items.accessConditionTypes.minHeight';
    case 'MinHeightAccompanied':
      return 'admin.parks.items.accessConditionTypes.minHeightAccompanied';
    case 'MaxHeight':
      return 'admin.parks.items.accessConditionTypes.maxHeight';
    case 'MinAge':
      return 'admin.parks.items.accessConditionTypes.minAge';
    case 'MinAgeAccompanied':
      return 'admin.parks.items.accessConditionTypes.minAgeAccompanied';
    case 'PregnancyRestriction':
      return 'admin.parks.items.accessConditionTypes.pregnancyRestriction';
    case 'HeartRestriction':
      return 'admin.parks.items.accessConditionTypes.heartRestriction';
    case 'BackNeckRestriction':
      return 'admin.parks.items.accessConditionTypes.backNeckRestriction';
    case 'WheelchairTransferRequired':
      return 'admin.parks.items.accessConditionTypes.wheelchairTransferRequired';
    case 'AccessPassRequired':
      return 'admin.parks.items.accessConditionTypes.accessPassRequired';
    case 'Custom':
    default:
      return 'admin.parks.items.accessConditionTypes.custom';
  }
}

export function hasLocalizedValues(items: LocalizedItem<string>[] | null | undefined): boolean {
  return (items ?? []).some((item: LocalizedItem<string>) => !!item.value && item.value.trim().length > 0);
}

export function toLocalizedItems(value: unknown): LocalizedItem<string>[] | null {
  if (!Array.isArray(value)) {
    return null;
  }

  const normalized: LocalizedItem<string>[] = value
    .filter((item: LocalizedItem<string>) => !!item && typeof item.languageCode === 'string')
    .map((item: LocalizedItem<string>) => ({
      languageCode: item.languageCode.trim().toLowerCase(),
      value: String(item.value ?? '').trim()
    }))
    .filter((item: LocalizedItem<string>) => item.languageCode.length > 0 && item.value.length > 0);

  return normalized.length > 0 ? normalized : null;
}


export function isAdminParkItemHeightAccessConditionType(type: AttractionAccessConditionType | string | null | undefined): boolean {
  return type === 'MinHeight' || type === 'MinHeightAccompanied' || type === 'MaxHeight';
}

export function getAdminParkItemStandaloneAccessConditionEntries(accessConditions: FormArray): AdminParkItemAccessConditionEntry[] {
  const entries: AdminParkItemAccessConditionEntry[] = [];

  for (let index: number = 0; index < accessConditions.length; index++) {
    const group: FormGroup = accessConditions.at(index) as FormGroup;
    const type: AttractionAccessConditionType | null = group.get('type')?.value as AttractionAccessConditionType | null;

    if (!isAdminParkItemHeightAccessConditionType(type)) {
      entries.push({ index, group });
    }
  }

  return entries;
}

export function getAdminParkItemHeightRequirementValue(
  accessConditions: FormArray,
  key: AdminParkItemHeightRequirementKey
): number | null {
  const field: AdminParkItemHeightRequirementField | undefined = ADMIN_PARK_ITEM_HEIGHT_REQUIREMENT_FIELDS
    .find((candidate: AdminParkItemHeightRequirementField) => candidate.key === key);

  if (!field) {
    return null;
  }

  const group: FormGroup | null = findAdminParkItemAccessConditionGroup(accessConditions, field.conditionType);
  const value: unknown = group?.get('value')?.value;
  const parsedValue: number = Number(value);

  return Number.isFinite(parsedValue) && parsedValue >= 0 ? parsedValue : null;
}

export function setAdminParkItemHeightRequirementValue(
  formBuilder: FormBuilder,
  accessConditions: FormArray,
  key: AdminParkItemHeightRequirementKey,
  value: unknown
): void {
  const field: AdminParkItemHeightRequirementField | undefined = ADMIN_PARK_ITEM_HEIGHT_REQUIREMENT_FIELDS
    .find((candidate: AdminParkItemHeightRequirementField) => candidate.key === key);

  if (!field) {
    return;
  }

  const normalizedValue: number | null = normalizeHeightRequirementValue(value);
  const existingIndex: number = findAdminParkItemAccessConditionIndex(accessConditions, field.conditionType);

  if (normalizedValue === null) {
    if (existingIndex >= 0) {
      accessConditions.removeAt(existingIndex);
      syncAdminParkItemAccessConditionDisplayOrders(accessConditions);
    }

    accessConditions.markAsDirty();
    return;
  }

  if (existingIndex >= 0) {
    const group: FormGroup = accessConditions.at(existingIndex) as FormGroup;
    const currentLabel: LocalizedItem<string>[] | null = toLocalizedItems(group.get('label')?.value);
    const defaultCondition: AttractionAccessCondition = buildAdminParkItemDefaultAccessCondition(field.conditionType, existingIndex + 1);

    group.patchValue({
      type: field.conditionType,
      isCustom: false,
      value: normalizedValue,
      unit: 'Centimeter',
      requiresAccompaniment: field.conditionType === 'MinHeightAccompanied',
      minimumCompanionAge: null,
      displayOrder: existingIndex + 1
    });

    if (!hasLocalizedValues(currentLabel)) {
      group.get('label')?.setValue(defaultCondition.label ?? []);
    }
  } else {
    const condition: AttractionAccessCondition = buildAdminParkItemDefaultAccessCondition(field.conditionType, accessConditions.length + 1);
    condition.value = normalizedValue;
    condition.unit = 'Centimeter';
    condition.requiresAccompaniment = field.conditionType === 'MinHeightAccompanied';
    accessConditions.push(createAdminParkItemAccessConditionGroup(formBuilder, condition));
  }

  syncAdminParkItemAccessConditionDisplayOrders(accessConditions);
  accessConditions.markAsDirty();
}

function findAdminParkItemAccessConditionGroup(
  accessConditions: FormArray,
  type: AttractionAccessConditionType
): FormGroup | null {
  const index: number = findAdminParkItemAccessConditionIndex(accessConditions, type);
  return index >= 0 ? accessConditions.at(index) as FormGroup : null;
}

function findAdminParkItemAccessConditionIndex(accessConditions: FormArray, type: AttractionAccessConditionType): number {
  for (let index: number = 0; index < accessConditions.length; index++) {
    const group: FormGroup = accessConditions.at(index) as FormGroup;

    if (group.get('type')?.value === type) {
      return index;
    }
  }

  return -1;
}

function normalizeHeightRequirementValue(value: unknown): number | null {
  if (value === null || value === undefined || value === '') {
    return null;
  }

  const parsedValue: number = Number(value);

  if (!Number.isFinite(parsedValue) || parsedValue < 0) {
    return null;
  }

  return Math.trunc(parsedValue);
}

function syncAdminParkItemAccessConditionDisplayOrders(accessConditions: FormArray): void {
  for (let index: number = 0; index < accessConditions.length; index++) {
    (accessConditions.at(index) as FormGroup).get('displayOrder')?.setValue(index + 1, { emitEvent: false });
  }
}

function buildAdminParkItemDefaultAccessCondition(
  type: AttractionAccessConditionType,
  displayOrder: number
): AttractionAccessCondition {
  return {
    type,
    isCustom: type === 'Custom',
    value: null,
    unit: getDefaultUnit(type),
    requiresAccompaniment: type === 'MinHeightAccompanied' || type === 'MinAgeAccompanied',
    minimumCompanionAge: null,
    label: buildDefaultLocalizedLabel(type),
    description: [],
    displayOrder
  };
}

function buildDefaultLocalizedLabel(type: AttractionAccessConditionType): LocalizedItem<string>[] {
  return LANGUAGES
    .map((language) => ({
      languageCode: language.value,
      value: ACCESS_CONDITION_DEFAULT_LABELS[type][language.value] ?? ''
    }))
    .filter((item: LocalizedItem<string>) => item.value.trim().length > 0);
}

function getDefaultUnit(type: AttractionAccessConditionType): AttractionAccessConditionUnit | null {
  switch (type) {
    case 'MinHeight':
    case 'MinHeightAccompanied':
    case 'MaxHeight':
      return 'Centimeter';
    case 'MinAge':
    case 'MinAgeAccompanied':
      return 'Year';
    default:
      return null;
  }
}
