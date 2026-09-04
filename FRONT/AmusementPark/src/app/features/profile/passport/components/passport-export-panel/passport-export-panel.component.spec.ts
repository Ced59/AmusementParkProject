import { PassportExportPanelComponent } from './passport-export-panel.component';

describe('PassportExportPanelComponent responsive contract', () => {
  it('bounds every export region and stacks its controls on mobile', () => {
    const styles: string = (
      PassportExportPanelComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('@media (max-width: 760px)');
    expect(styles).toContain('grid-template-columns: 1fr');
    expect(styles).toContain('@media (max-width: 520px)');
  });
});
