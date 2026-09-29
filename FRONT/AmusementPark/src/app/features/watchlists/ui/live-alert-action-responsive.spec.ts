import { LiveAlertActionComponent } from './live-alert-action.component';

describe('Live alert action responsive contract', () => {
  it('stacks types, durations and active alerts inside narrow viewports', () => {
    const styles: string = (
      LiveAlertActionComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('width: 100%');
  });
});
