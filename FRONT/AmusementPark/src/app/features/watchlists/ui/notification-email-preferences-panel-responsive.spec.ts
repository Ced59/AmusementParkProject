import { NotificationEmailPreferencesPanelComponent } from './notification-email-preferences-panel.component';

describe('Notification email preferences responsive contract', () => {
  it('keeps content and actions inside narrow mobile viewports', () => {
    const styles: string = (
      NotificationEmailPreferencesPanelComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('width: 100%');
  });
});
