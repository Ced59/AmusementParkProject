import { PublicVideoListViewComponent } from './public-video-list-view.component';

describe('PublicVideoListViewComponent', () => {
  it('keeps long entity names inside the mobile hero and actions', () => {
    const styles: string = (
      PublicVideoListViewComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('.public-video-list__hero-content');
    expect(styles).toMatch(/\.public-video-list__actions[\s\S]*span/);
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('white-space: normal');
    expect(styles).toContain('overflow-wrap: anywhere');
  });
});
