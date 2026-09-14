import { AdminParkFitOperationalControlsComponent } from './admin-park-fit-operational-controls.component';

describe('Admin Park Fit portfolio controls responsive contract', () => {
  it('contains actions and long explanations inside narrow mobile viewports', () => {
    const styles: string = (
      AdminParkFitOperationalControlsComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 30rem)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
  });
});
