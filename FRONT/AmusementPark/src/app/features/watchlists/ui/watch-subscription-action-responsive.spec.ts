import { WatchSubscriptionActionComponent } from './watch-subscription-action.component';

describe('Watch subscription action responsive contract', () => {
  it('collapses group choices and actions without horizontal overflow', () => {
    const styles: string = (
      WatchSubscriptionActionComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('width: 100%');
  });
});
