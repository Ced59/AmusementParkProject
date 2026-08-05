import { ParkItemsListViewComponent } from './park-items-list-view.component';

describe('ParkItemsListViewComponent', () => {
  it('keeps long park names inside the mobile results header', () => {
    const styles: string = (
      ParkItemsListViewComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toMatch(/\.results-workbench__header[\s\S]*>\s*div/);
    expect(styles).toMatch(/\.results-workbench__back[\s\S]*span/);
    expect(styles).toMatch(/\.results-workbench__header[\s\S]*app-ui-kicker/);
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('white-space: normal');
    expect(styles).toContain('overflow-wrap: anywhere');
  });
});
