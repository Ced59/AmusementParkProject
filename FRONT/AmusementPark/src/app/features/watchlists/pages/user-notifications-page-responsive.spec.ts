import { UserNotificationsPageComponent } from './user-notifications-page.component';

describe('User notifications responsive contract', () => {
  it('keeps cards, proofs and actions inside narrow viewports', () => {
    const styles: string = (
      UserNotificationsPageComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('.notification-card__proof');
    expect(styles).toContain('.notification-card__lifecycle');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('width: 100%');
  });
});
