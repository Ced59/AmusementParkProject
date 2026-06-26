import { readAdminFieldModeSelectedParkId, writeAdminFieldModeSelectedParkId } from './admin-field-mode-storage';

describe('admin field mode storage', () => {
  const key = 'admin.fieldMode.test.selectedParkId';

  afterEach(() => {
    localStorage.removeItem(key);
  });

  it('persists the selected park id', () => {
    writeAdminFieldModeSelectedParkId(key, 'park-1');

    expect(readAdminFieldModeSelectedParkId(key)).toBe('park-1');
  });

  it('removes the selected park id when cleared', () => {
    localStorage.setItem(key, 'park-1');

    writeAdminFieldModeSelectedParkId(key, null);

    expect(readAdminFieldModeSelectedParkId(key)).toBeNull();
  });
});
