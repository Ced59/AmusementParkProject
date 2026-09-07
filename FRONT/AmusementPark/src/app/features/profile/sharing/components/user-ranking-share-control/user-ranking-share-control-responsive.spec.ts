import { UserRankingShareControlComponent } from './user-ranking-share-control.component';

describe('UserRankingShareControlComponent responsive contract', () => {
  it('bounds every editor branch and stacks actions on narrow mobile viewports', () => {
    const styles: string = (
      UserRankingShareControlComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow: hidden');
    expect(styles).toContain('grid-template-columns: repeat(2, minmax(0, 1fr))');
    expect(styles).toContain('@media (max-width: 720px)');
    expect(styles).toContain('@media (max-width: 520px)');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('grid-template-columns: 1fr');
  });
});
