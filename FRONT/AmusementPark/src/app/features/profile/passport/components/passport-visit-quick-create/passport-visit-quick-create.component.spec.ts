import { PassportVisitQuickCreateComponent } from './passport-visit-quick-create.component';

describe('PassportVisitQuickCreateComponent responsive contract', () => {
  it('bounds the dialog to the dynamic viewport and the safe areas', () => {
    const styles: string = (
      PassportVisitQuickCreateComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-height: min(52rem, 100dvh - 2rem)');
    expect(styles).toContain('max-width: 100vw !important');
    expect(styles).toContain('env(safe-area-inset-bottom)');
  });

  it('reflows date fields and actions to one column on narrow mobile viewports', () => {
    const styles: string = (
      PassportVisitQuickCreateComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('@media (max-width: 520px)');
    expect(styles).toContain('.passport-date-fields');
    expect(styles).toContain('grid-template-columns: 1fr');
    expect(styles).toContain('@media (max-width: 360px)');
  });
});
