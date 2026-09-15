import { UserCollectionsPageComponent } from './user-collections-page.component';

describe('User collections responsive contract', () => {
  it('keeps cards, actions and long text inside a narrow mobile viewport', () => {
    const styles: string = (
      UserCollectionsPageComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('minmax(min(100%, 21rem), 1fr)');
    expect(styles).toContain('@media (max-width: 36rem)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
  });
});
