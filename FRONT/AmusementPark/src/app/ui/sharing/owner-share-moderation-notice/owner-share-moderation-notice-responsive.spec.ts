import { OwnerShareModerationNoticeComponent } from './owner-share-moderation-notice.component';

describe('OwnerShareModerationNoticeComponent responsive contract', () => {
  it('keeps the moderation explanation inside narrow mobile viewports', () => {
    const styles: string = (
      OwnerShareModerationNoticeComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('minmax(0, 1fr)');
    expect(styles).toContain('overflow: hidden');
    expect(styles).toContain('overflow-wrap: anywhere');
  });
});
