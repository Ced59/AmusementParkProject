import { PassportProfileSharePageComponent } from './passport-profile-share-page.component';

describe('PassportProfileSharePageComponent responsive contract', () => {
  it('contains every selection grid within a 320px viewport', () => {
    const styles: string = (PassportProfileSharePageComponent as unknown as { ɵcmp: { styles: string[] } }).ɵcmp.styles.join('\n');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('minmax(0, 1fr)');
    expect(styles).toContain('minmax(min(100%, 15rem), 1fr)');
    expect(styles).toContain('minmax(min(100%, 12rem), 1fr)');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 420px)');
  });
});
