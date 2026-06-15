import { FormArray, FormBuilder, FormGroup } from '@angular/forms';

import { ParkItem } from '@app/models/parks/park-item';
import {
  buildAdminParkItemEditSnapshot,
  createAdminParkItemEditForm,
  getAdminParkItemFirstInvalidTabIndex,
  mapAdminParkItemEditFormToParkItem,
  patchAdminParkItemEditForm
} from './admin-park-item-edit-form.mapper';
import { createAdminParkItemAccessConditionGroup } from './admin-park-item-access-condition-form.utils';

describe('admin park item edit form mapper', () => {
  let formBuilder: FormBuilder;

  beforeEach(() => {
    formBuilder = new FormBuilder();
  });

  function getAccessConditions(form: FormGroup): FormArray {
    return form.get(['attractionDetails', 'accessConditions']) as FormArray;
  }

  it('creates a park item form with attraction-oriented defaults', () => {
    const form: FormGroup = createAdminParkItemEditForm(formBuilder, 'park-1');

    expect(form.get('parkId')?.value).toBe('park-1');
    expect(form.get('category')?.value).toBe('Attraction');
    expect(form.get('type')?.value).toBe('Attraction');
    expect(form.get('latitude')?.value).toBe(48.8566);
    expect(form.get('longitude')?.value).toBe(2.3522);
    expect(form.get('isVisible')?.value).toBeFalse();
    expect(form.get('adminReviewStatus')?.value).toBe('ToReview');
    expect(getAccessConditions(form).length).toBe(0);
  });

  it('patches attraction details, normalized dates and location points', () => {
    const form: FormGroup = createAdminParkItemEditForm(formBuilder, 'park-1');
    const item: ParkItem = {
      parkId: 'park-2',
      zoneId: 'zone-1',
      name: 'Taron',
      category: 'Attraction',
      type: 'RollerCoaster',
      subtype: null,
      latitude: 50.801,
      longitude: 6.879,
      isVisible: false,
      adminReviewStatus: 'ToReview',
      attractionDetails: {
        manufacturerId: 'intamin',
        model: 'LSM Launch Coaster',
        status: 'Operating',
        openingDate: '2016-06-30T00:00:00Z',
        closingDate: 'invalid-date',
        accessConditions: [
          { type: 'MinHeight', typeKey: 'min-height', value: 130, unit: 'Centimeter' }
        ]
      },
      attractionLocations: {
        entrance: { latitude: 50.1, longitude: 6.1 },
        exit: null,
        fastPassEntrance: { latitude: 50.2, longitude: 6.2 },
        reducedMobilityEntrance: null
      }
    } as ParkItem;

    patchAdminParkItemEditForm(formBuilder, form, item);

    expect(form.get('parkId')?.value).toBe('park-2');
    expect(form.get('zoneId')?.value).toBe('zone-1');
    expect(form.get(['attractionDetails', 'openingDate'])?.value).toBe('2016-06-30');
    expect(form.get(['attractionDetails', 'closingDate'])?.value).toBe('');
    expect(form.get(['attractionLocations', 'entrance', 'latitude'])?.value).toBe(50.1);
    expect(form.get(['attractionLocations', 'exit', 'latitude'])?.value).toBeNull();
    expect(getAccessConditions(form).length).toBe(1);
  });

  it('maps non-attraction items without attraction details or locations', () => {
    const form: FormGroup = createAdminParkItemEditForm(formBuilder, 'park-1');
    form.patchValue({
      name: 'Burger Place',
      category: 'Restaurant',
      type: 'RollerCoaster',
      zoneId: '',
      latitude: '50.5',
      longitude: '3.5'
    });
    form.get(['attractionDetails', 'model'])?.setValue('Should be ignored');

    const item: ParkItem = mapAdminParkItemEditFormToParkItem(form);

    expect(item.category).toBe('Restaurant');
    expect(item.type).toBe('Restaurant');
    expect(item.zoneId).toBeNull();
    expect(item.latitude).toBe(50.5);
    expect(item.longitude).toBe(3.5);
    expect(item.attractionDetails).toBeNull();
    expect(item.attractionLocations).toBeNull();
  });

  it('maps attraction details only when at least one meaningful value exists', () => {
    const form: FormGroup = createAdminParkItemEditForm(formBuilder, 'park-1');
    form.patchValue({ name: 'Attraction', category: 'Attraction', type: 'RollerCoaster' });

    expect(mapAdminParkItemEditFormToParkItem(form).attractionDetails).toBeNull();

    form.get(['attractionDetails', 'hasFastPass'])?.setValue(true);
    form.get(['attractionDetails', 'durationInSeconds'])?.setValue('123.8');
    form.get(['attractionDetails', 'waterExposureLevel'])?.setValue('unknown');

    const item: ParkItem = mapAdminParkItemEditFormToParkItem(form);

    expect(item.attractionDetails?.hasFastPass).toBeTrue();
    expect(item.attractionDetails?.durationInSeconds).toBe(123);
    expect(item.attractionDetails?.waterExposureLevel).toBeNull();
  });

  it('normalizes status aliases and preserves unknown statuses for editorial review', () => {
    const form: FormGroup = createAdminParkItemEditForm(formBuilder, 'park-1');
    form.patchValue({ name: 'Attraction', category: 'Attraction', type: 'Attraction' });

    form.get(['attractionDetails', 'status'])?.setValue('permanently closed');
    expect(mapAdminParkItemEditFormToParkItem(form).attractionDetails?.status).toBe('ClosedDefinitively');

    form.get(['attractionDetails', 'status'])?.setValue('Soft opening');
    expect(mapAdminParkItemEditFormToParkItem(form).attractionDetails?.status).toBe('Soft opening');
  });

  it('maps access conditions by removing empty custom rows and normalizing localized values', () => {
    const form: FormGroup = createAdminParkItemEditForm(formBuilder, 'park-1');
    form.patchValue({ name: 'Attraction', category: 'Attraction', type: 'Attraction' });
    getAccessConditions(form).push(createAdminParkItemAccessConditionGroup(formBuilder, {
      type: 'Custom',
      typeKey: null,
      label: [{ languageCode: 'fr', value: '   ' }]
    } as never));
    getAccessConditions(form).push(createAdminParkItemAccessConditionGroup(formBuilder, {
      type: 'Custom',
      typeKey: null,
      customTypeLabel: [{ languageCode: ' FR ', value: '  Accompagnant obligatoire ' }],
      description: [{ languageCode: 'en', value: '  Ask staff ' }],
      value: '1.9',
      unit: 'Year',
      minimumCompanionAge: '16.8'
    } as never));

    const item: ParkItem = mapAdminParkItemEditFormToParkItem(form);

    expect(item.attractionDetails?.accessConditions?.length).toBe(1);
    expect(item.attractionDetails?.accessConditions?.[0].displayOrder).toBe(1);
    expect(item.attractionDetails?.accessConditions?.[0].customTypeLabel).toEqual([
      { languageCode: 'fr', value: 'Accompagnant obligatoire' }
    ]);
    expect(item.attractionDetails?.accessConditions?.[0].description).toEqual([
      { languageCode: 'en', value: 'Ask staff' }
    ]);
    expect(item.attractionDetails?.accessConditions?.[0].value).toBe(1.9);
    expect(item.attractionDetails?.accessConditions?.[0].minimumCompanionAge).toBe(16);
  });

  it('maps location points only when latitude and longitude are both finite', () => {
    const form: FormGroup = createAdminParkItemEditForm(formBuilder, 'park-1');
    form.patchValue({ name: 'Attraction', category: 'Attraction', type: 'Attraction' });
    form.get(['attractionLocations', 'entrance'])?.patchValue({ latitude: '50.1', longitude: '' });
    form.get(['attractionLocations', 'exit'])?.patchValue({ latitude: '51.1', longitude: '3.2' });

    const item: ParkItem = mapAdminParkItemEditFormToParkItem(form);

    expect(item.attractionLocations?.entrance).toBeNull();
    expect(item.attractionLocations?.exit).toEqual({ latitude: 51.1, longitude: 3.2 });
  });

  it('falls back to zero for invalid required coordinates', () => {
    const form: FormGroup = createAdminParkItemEditForm(formBuilder, 'park-1');
    form.patchValue({ name: 'Attraction', latitude: 'bad', longitude: null });

    const item: ParkItem = mapAdminParkItemEditFormToParkItem(form);

    expect(item.latitude).toBe(0);
    expect(item.longitude).toBe(0);
  });

  it('detects the first invalid edition tab depending on item category', () => {
    const form: FormGroup = createAdminParkItemEditForm(formBuilder, 'park-1');

    form.get('name')?.setValue('');
    expect(getAdminParkItemFirstInvalidTabIndex(form)).toBe(0);

    form.get('name')?.setValue('Attraction');
    getAccessConditions(form).push(createAdminParkItemAccessConditionGroup(formBuilder, {
      type: null,
      typeKey: null
    } as never));
    getAccessConditions(form).at(0).get('type')?.setValue(null);
    getAccessConditions(form).at(0).get('type')?.markAsTouched();
    expect(getAdminParkItemFirstInvalidTabIndex(form)).toBe(2);

    form.get('category')?.setValue('Restaurant');
    expect(getAdminParkItemFirstInvalidTabIndex(form)).toBe(0);
  });

  it('builds a deterministic snapshot from normalized park item values', () => {
    const form: FormGroup = createAdminParkItemEditForm(formBuilder, 'park-1');
    form.patchValue({ name: '  Attraction  ', category: 'Restaurant', type: 'RollerCoaster' });

    const snapshot: string = buildAdminParkItemEditSnapshot(form);

    expect(snapshot).toContain('"category":"Restaurant"');
    expect(snapshot).toContain('"type":"Restaurant"');
  });
});
