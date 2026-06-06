import { FormArray, FormBuilder, FormGroup } from '@angular/forms';

import { AttractionAccessCondition } from '@app/models/parks/attraction-access-condition';
import {
  addAdminParkItemAccessCondition,
  createAdminParkItemAccessConditionGroup,
  getAdminParkItemAccessConditionLabelKey,
  getAdminParkItemHeightRequirementValue,
  getAdminParkItemStandaloneAccessConditionEntries,
  hasLocalizedValues,
  isAdminParkItemHeightAccessConditionType,
  moveAdminParkItemAccessConditionDown,
  moveAdminParkItemAccessConditionUp,
  removeAdminParkItemAccessCondition,
  setAdminParkItemAccessConditions,
  setAdminParkItemHeightRequirementValue,
  toAdminParkItemAccessConditionTypeKey,
  toLocalizedItems,
  updateAdminParkItemAccessConditionType
} from './admin-park-item-access-condition-form.utils';

describe('admin park item access condition form utils', () => {
  let formBuilder: FormBuilder;

  beforeEach(() => {
    formBuilder = new FormBuilder();
  });

  function createArray(conditions: AttractionAccessCondition[] = []): FormArray {
    return formBuilder.array(conditions.map((condition: AttractionAccessCondition): FormGroup => {
      return createAdminParkItemAccessConditionGroup(formBuilder, condition);
    }));
  }

  it('creates a custom access condition group with safe defaults', () => {
    const group: FormGroup = createAdminParkItemAccessConditionGroup(formBuilder);

    expect(group.get('type')?.value).toBe('Custom');
    expect(group.get('typeKey')?.value).toBe('custom');
    expect(group.get('isCustom')?.value).toBeFalse();
    expect(group.get('label')?.value).toEqual([]);
  });

  it('replaces all access conditions and resynchronizes display orders', () => {
    const accessConditions: FormArray = createArray([
      { type: 'MinAge', typeKey: 'min-age', value: 8, unit: 'Year', displayOrder: 99 } as AttractionAccessCondition
    ]);

    setAdminParkItemAccessConditions(formBuilder, accessConditions, [
      { type: 'HeartRestriction', typeKey: 'heart-restriction', displayOrder: 42 } as AttractionAccessCondition,
      { type: 'PregnancyRestriction', typeKey: 'pregnancy-restriction', displayOrder: 77 } as AttractionAccessCondition
    ]);

    expect(accessConditions.length).toBe(2);
    expect(accessConditions.at(0).get('displayOrder')?.value).toBe(1);
    expect(accessConditions.at(1).get('displayOrder')?.value).toBe(2);
  });

  it('adds a predefined access condition using the selected option labels', () => {
    const accessConditions: FormArray = createArray();

    addAdminParkItemAccessCondition(formBuilder, accessConditions, 'min-age', [
      {
        labelKey: 'age',
        value: 'min-age',
        legacyType: 'MinAge',
        labels: [{ languageCode: 'fr', value: 'Âge minimum' }]
      }
    ]);

    const group: FormGroup = accessConditions.at(0) as FormGroup;
    expect(group.get('type')?.value).toBe('MinAge');
    expect(group.get('unit')?.value).toBe('Year');
    expect(group.get('label')?.value).toEqual([{ languageCode: 'fr', value: 'Âge minimum' }]);
  });

  it('falls back to custom option when the requested type key is unknown', () => {
    const accessConditions: FormArray = createArray();

    addAdminParkItemAccessCondition(formBuilder, accessConditions, 'unknown', [
      { labelKey: 'custom', value: 'custom', legacyType: 'Custom' }
    ]);

    expect(accessConditions.at(0).get('type')?.value).toBe('Custom');
    expect(accessConditions.at(0).get('typeKey')?.value).toBe('custom');
  });

  it('removes and moves conditions without breaking sequential display orders', () => {
    const accessConditions: FormArray = createArray([
      { type: 'MinAge', typeKey: 'min-age', displayOrder: 1 } as AttractionAccessCondition,
      { type: 'HeartRestriction', typeKey: 'heart-restriction', displayOrder: 2 } as AttractionAccessCondition,
      { type: 'Custom', typeKey: 'custom', displayOrder: 3 } as AttractionAccessCondition
    ]);

    moveAdminParkItemAccessConditionDown(accessConditions, 0);
    expect(accessConditions.at(0).get('type')?.value).toBe('HeartRestriction');
    expect(accessConditions.at(1).get('displayOrder')?.value).toBe(2);

    moveAdminParkItemAccessConditionUp(accessConditions, 2);
    expect(accessConditions.at(1).get('type')?.value).toBe('Custom');

    removeAdminParkItemAccessCondition(accessConditions, 1);
    expect(accessConditions.length).toBe(2);
    expect(accessConditions.at(1).get('displayOrder')?.value).toBe(2);
  });

  it('ignores move requests outside useful boundaries', () => {
    const accessConditions: FormArray = createArray([
      { type: 'MinAge', typeKey: 'min-age', displayOrder: 1 } as AttractionAccessCondition,
      { type: 'Custom', typeKey: 'custom', displayOrder: 2 } as AttractionAccessCondition
    ]);

    moveAdminParkItemAccessConditionUp(accessConditions, 0);
    moveAdminParkItemAccessConditionDown(accessConditions, 1);

    expect(accessConditions.at(0).get('type')?.value).toBe('MinAge');
    expect(accessConditions.at(1).get('type')?.value).toBe('Custom');
  });

  it('updates the selected type while preserving an existing manual label', () => {
    const accessConditions: FormArray = createArray([
      {
        type: 'Custom',
        typeKey: 'custom',
        label: [{ languageCode: 'fr', value: 'Libellé éditorial' }]
      } as AttractionAccessCondition
    ]);
    accessConditions.at(0).get('typeKey')?.setValue('min-height-accompanied');

    updateAdminParkItemAccessConditionType(accessConditions, 0, [
      { labelKey: 'height', value: 'min-height-accompanied', legacyType: 'MinHeightAccompanied' }
    ]);

    const group: FormGroup = accessConditions.at(0) as FormGroup;
    expect(group.get('type')?.value).toBe('MinHeightAccompanied');
    expect(group.get('unit')?.value).toBe('Centimeter');
    expect(group.get('requiresAccompaniment')?.value).toBeTrue();
    expect(group.get('label')?.value).toEqual([{ languageCode: 'fr', value: 'Libellé éditorial' }]);
  });

  it('detects and filters standalone conditions outside height requirements', () => {
    const accessConditions: FormArray = createArray([
      { type: 'MinHeight', value: 120 } as AttractionAccessCondition,
      { type: 'PregnancyRestriction' } as AttractionAccessCondition,
      { type: 'MaxHeight', value: 200 } as AttractionAccessCondition
    ]);

    const entries = getAdminParkItemStandaloneAccessConditionEntries(accessConditions);

    expect(entries.length).toBe(1);
    expect(entries[0].index).toBe(1);
    expect(entries[0].group.get('type')?.value).toBe('PregnancyRestriction');
  });

  it('creates, updates and removes height requirement conditions from normalized values', () => {
    const accessConditions: FormArray = createArray();

    setAdminParkItemHeightRequirementValue(formBuilder, accessConditions, 'minHeightCm', '120.9');
    expect(getAdminParkItemHeightRequirementValue(accessConditions, 'minHeightCm')).toBe(120);
    expect(accessConditions.at(0).get('unit')?.value).toBe('Centimeter');

    setAdminParkItemHeightRequirementValue(formBuilder, accessConditions, 'minHeightCm', 125);
    expect(accessConditions.length).toBe(1);
    expect(getAdminParkItemHeightRequirementValue(accessConditions, 'minHeightCm')).toBe(125);

    setAdminParkItemHeightRequirementValue(formBuilder, accessConditions, 'minHeightCm', -1);
    expect(accessConditions.length).toBe(0);
  });

  it('returns null for missing or invalid height values', () => {
    const accessConditions: FormArray = createArray([
      { type: 'MinHeight', value: 'not-a-number' } as unknown as AttractionAccessCondition,
      { type: 'MaxHeight', value: -10 } as AttractionAccessCondition
    ]);

    expect(getAdminParkItemHeightRequirementValue(accessConditions, 'minHeightCm')).toBeNull();
    expect(getAdminParkItemHeightRequirementValue(accessConditions, 'maxHeightCm')).toBeNull();
  });

  it('normalizes localized items and rejects empty or malformed values', () => {
    const localizedItems = toLocalizedItems([
      { languageCode: ' FR ', value: ' Libellé ' },
      { languageCode: '', value: 'No language' },
      { languageCode: 'en', value: '   ' },
      null
    ]);

    expect(localizedItems).toEqual([{ languageCode: 'fr', value: 'Libellé' }]);
    expect(toLocalizedItems('not-array')).toBeNull();
    expect(hasLocalizedValues([{ languageCode: 'fr', value: '   ' }])).toBeFalse();
  });

  it('maps access condition type keys and label keys with safe fallbacks', () => {
    expect(toAdminParkItemAccessConditionTypeKey('BackNeckRestriction')).toBe('back-neck-restriction');
    expect(toAdminParkItemAccessConditionTypeKey(null)).toBe('custom');
    expect(getAdminParkItemAccessConditionLabelKey('AccessPassRequired')).toBe('admin.parks.items.accessConditionTypes.accessPassRequired');
    expect(getAdminParkItemAccessConditionLabelKey('Custom')).toBe('admin.parks.items.accessConditionTypes.custom');
    expect(isAdminParkItemHeightAccessConditionType('MaxHeight')).toBeTrue();
    expect(isAdminParkItemHeightAccessConditionType('MinAge')).toBeFalse();
  });
});
