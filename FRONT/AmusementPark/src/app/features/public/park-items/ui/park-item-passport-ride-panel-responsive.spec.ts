import { ParkItemPassportRidePanelComponent } from './park-item-passport-ride-panel.component';

describe('park item passport ride responsive contract', () => {
  it('bounds the panel to the viewport and reflows its form on narrow screens', () => {
    const styles: string = (
      ParkItemPassportRidePanelComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('@media (max-width: 520px)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('min-height: 44px');
  });
});
