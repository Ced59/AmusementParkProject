export function readAdminFieldModeSelectedParkId(key: string): string | null {
  return typeof localStorage === 'undefined' ? null : localStorage.getItem(key);
}

export function writeAdminFieldModeSelectedParkId(key: string, parkId: string | null): void {
  if (typeof localStorage === 'undefined') {
    return;
  }

  if (!parkId) {
    localStorage.removeItem(key);
    return;
  }

  localStorage.setItem(key, parkId);
}
