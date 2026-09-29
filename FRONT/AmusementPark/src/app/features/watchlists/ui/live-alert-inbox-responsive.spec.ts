import { LiveAlertInboxComponent } from './live-alert-inbox.component';

describe('Live alert inbox responsive contract', () => {
  it('stacks proofs, images and actions inside narrow viewports', () => {
    const styles: string = (
      LiveAlertInboxComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('width: 100%');
  });
});
