import { TripInvitationPreviewPageComponent } from './trip-invitation-preview-page.component';

describe('Trip invitation preview responsive contract', () => {
  it('contains every private preview field inside narrow mobile viewports', () => {
    const styles: string = (
      TripInvitationPreviewPageComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('grid-template-columns: repeat(3, minmax(0, 1fr))');
    expect(styles).toContain('@media (max-width: 42rem)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('padding-bottom: calc(5.75rem + env(safe-area-inset-bottom))');
  });
});
