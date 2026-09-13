import { AdminShareModerationComponent } from './admin-share-moderation.component';

describe('AdminShareModerationComponent responsive contract', (): void => {
  it('uses cards and collapses controls for narrow viewports', (): void => {
    const styles: string = (AdminShareModerationComponent as unknown as {
      ɵcmp: { styles: string[] };
    }).ɵcmp.styles.join('\n');

    expect(styles).toContain('minmax(min(100%, 22rem), 1fr)');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 48rem)');
    expect(styles).toContain('@media (max-width: 30rem)');
  });
});
