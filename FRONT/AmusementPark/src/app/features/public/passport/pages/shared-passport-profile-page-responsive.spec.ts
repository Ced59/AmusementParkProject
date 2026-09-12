import { SharedPassportProfilePageComponent } from './shared-passport-profile-page.component';

describe('SharedPassportProfilePageComponent responsive contract', () => {
  it('keeps the public passport within the viewport down to 320px', () => {
    const styles: string = (SharedPassportProfilePageComponent as unknown as { ɵcmp: { styles: string[] } }).ɵcmp.styles.join('\n');
    expect(styles).toContain('width: 76rem');
    expect(styles).toContain('max-width: calc(100% - 2rem)');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('minmax(0, 1fr)');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 430px)');
  });
});
