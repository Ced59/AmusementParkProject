import { FormBuilder, FormGroup } from '@angular/forms';

import { Park } from '@app/models/parks/park';
import {
  DEFAULT_ADMIN_PARK_COORDINATES,
  buildAdminParkEditSnapshot,
  createAdminParkEditForm,
  getAdminParkFirstInvalidTabIndex,
  mapAdminParkEditFormToPark,
  patchAdminParkEditForm
} from './admin-park-edit-form.mapper';

describe('admin park edit form mapper', () => {
  let formBuilder: FormBuilder;

  beforeEach(() => {
    formBuilder = new FormBuilder();
  });

  it('creates a park form with production-safe edition defaults', () => {
    const form: FormGroup = createAdminParkEditForm(formBuilder);

    expect(form.get('name')?.value).toBe('');
    expect(form.get('latitude')?.value).toBe(DEFAULT_ADMIN_PARK_COORDINATES[0]);
    expect(form.get('longitude')?.value).toBe(DEFAULT_ADMIN_PARK_COORDINATES[1]);
    expect(form.get('isVisible')?.value).toBeTrue();
    expect(form.get('adminReviewStatus')?.value).toBe('Validated');
  });

  it('patches nullable park fields without emitting user-edit defaults', () => {
    const form: FormGroup = createAdminParkEditForm(formBuilder);
    const park: Park = {
      id: 'park-1',
      name: 'Walibi',
      countryCode: null as unknown as string,
      type: 'ThemePark',
      latitude: 50.1,
      longitude: 3.2,
      isVisible: undefined,
      adminReviewStatus: null,
      descriptions: null,
      webSiteUrl: null,
      city: null
    } as unknown as Park;

    patchAdminParkEditForm(form, park);

    expect(form.get('id')?.value).toBe('park-1');
    expect(form.get('countryCode')?.value).toBe('');
    expect(form.get('isVisible')?.value).toBeTrue();
    expect(form.get('adminReviewStatus')?.value).toBe('Validated');
    expect(form.get('descriptions')?.value).toEqual([]);
    expect(form.get('websiteUrl')?.value).toBe('');
  });

  it('maps the form to a clean park payload with trimmed optional strings', () => {
    const form: FormGroup = createAdminParkEditForm(formBuilder);
    form.patchValue({
      id: 'park-1',
      name: '  Bellewaerde  ',
      countryCode: ' BE ',
      founderId: '  founder-1 ',
      operatorId: ' ',
      latitude: '50.846',
      longitude: '2.945',
      websiteUrl: ' https://example.test ',
      street: ' ',
      city: ' Ypres ',
      postalCode: ' 8900 ',
      isFeaturedOnHome: true,
      featuredHomeOrder: '3',
      isFeaturedOnHomeSponsored: true
    });

    const park: Park = mapAdminParkEditFormToPark(form);

    expect(park.name).toBe('  Bellewaerde  ');
    expect(park.countryCode).toBe('BE');
    expect(park.founderId).toBe('founder-1');
    expect(park.operatorId).toBeNull();
    expect(park.latitude).toBe(50.846);
    expect(park.longitude).toBe(2.945);
    expect(park.webSiteUrl).toBe('https://example.test');
    expect(park.street).toBeUndefined();
    expect(park.city).toBe('Ypres');
    expect(park.postalCode).toBe('8900');
    expect(park.featuredHomeOrder).toBe(3);
    expect(park.isFeaturedOnHomeSponsored).toBeTrue();
  });

  it('drops invalid featured order and sponsored flag when the park is not featured', () => {
    const form: FormGroup = createAdminParkEditForm(formBuilder);
    form.patchValue({
      name: 'Park',
      isFeaturedOnHome: false,
      featuredHomeOrder: '-1',
      isFeaturedOnHomeSponsored: true
    });

    const park: Park = mapAdminParkEditFormToPark(form);

    expect(park.featuredHomeOrder).toBeNull();
    expect(park.isFeaturedOnHomeSponsored).toBeFalse();
  });

  it('builds a deterministic snapshot from normalized form values', () => {
    const form: FormGroup = createAdminParkEditForm(formBuilder);
    form.patchValue({ name: 'Park', countryCode: ' FR ' });

    const snapshot: string = buildAdminParkEditSnapshot(form);

    expect(snapshot).toContain('"countryCode":"FR"');
    expect(snapshot).toContain('"name":"Park"');
  });

  it('returns the first invalid tab index for general and map fields', () => {
    const form: FormGroup = createAdminParkEditForm(formBuilder);

    form.get('name')?.setValue('');
    form.get('name')?.markAsTouched();
    expect(getAdminParkFirstInvalidTabIndex(form)).toBe(0);

    form.get('name')?.setValue('Park');
    form.get('latitude')?.setValue(null);
    expect(getAdminParkFirstInvalidTabIndex(form)).toBe(1);

    form.get('latitude')?.setValue(48);
    expect(getAdminParkFirstInvalidTabIndex(form)).toBe(0);
  });
});
