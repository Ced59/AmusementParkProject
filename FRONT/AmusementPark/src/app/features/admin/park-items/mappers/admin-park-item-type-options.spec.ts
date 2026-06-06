import { FormBuilder, FormGroup } from '@angular/forms';

import { ParkItemType } from '@app/models/parks/park-item-type';
import {
  applyAdminParkItemCategorySelection,
  getAdminParkItemCategoryOptions,
  getAdminParkItemTypeOptionsForCategory
} from './admin-park-item-type-options';

describe('admin park item type options', () => {
  let formBuilder: FormBuilder;

  beforeEach(() => {
    formBuilder = new FormBuilder();
  });

  it('returns defensive copies of category and type options', () => {
    const categoryOptions = getAdminParkItemCategoryOptions();
    const attractionOptions = getAdminParkItemTypeOptionsForCategory('Attraction');
    const secondAttractionOptions = getAdminParkItemTypeOptionsForCategory('Attraction');

    categoryOptions.pop();
    attractionOptions.pop();

    expect(getAdminParkItemCategoryOptions().length).toBeGreaterThan(categoryOptions.length);
    expect(secondAttractionOptions.length).toBeGreaterThan(attractionOptions.length);
  });

  it('uses attraction type options for attractions', () => {
    const values: ParkItemType[] = getAdminParkItemTypeOptionsForCategory('Attraction')
      .map((option) => option.value);

    expect(values).toContain('RollerCoaster');
    expect(values).toContain('DarkRide');
  });

  it('uses category-specific non-attraction type options', () => {
    expect(getAdminParkItemTypeOptionsForCategory('Restaurant').map((option) => option.value)).toEqual(['Restaurant', 'Snack']);
    expect(getAdminParkItemTypeOptionsForCategory('Service').map((option) => option.value)).toContain('Toilets');
  });

  it('falls back to the generic other type list for unknown categories', () => {
    const values: ParkItemType[] = getAdminParkItemTypeOptionsForCategory('Unexpected' as never)
      .map((option) => option.value);

    expect(values).toEqual(['Other']);
  });

  it('preserves the current type when it remains allowed by the selected category', () => {
    const form: FormGroup = formBuilder.group({ type: ['Snack'] });

    const options = applyAdminParkItemCategorySelection(form, 'Restaurant');

    expect(form.get('type')?.value).toBe('Snack');
    expect(options.map((option) => option.value)).toContain('Snack');
  });

  it('resets the type to the first allowed option when category selection invalidates it', () => {
    const form: FormGroup = formBuilder.group({ type: ['RollerCoaster'] });

    applyAdminParkItemCategorySelection(form, 'Restaurant');

    expect(form.get('type')?.value).toBe('Restaurant');
  });
});
