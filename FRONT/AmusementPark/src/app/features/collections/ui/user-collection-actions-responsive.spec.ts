import { UserCollectionActionsComponent } from './user-collection-actions.component';

describe('User collection actions responsive contract', () => {
  it('stacks full-width actions without horizontal overflow on mobile', () => {
    const styles: string = (
      UserCollectionActionsComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 36rem)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('width: 100%');
  });
});
