import { AdminLiveOperationsComponent } from './admin-live-operations.component';

describe('AdminLiveOperationsComponent responsive contract', () => {
  it('keeps every structural container bounded by the viewport', () => {
    expect(AdminLiveOperationsComponent).toBeDefined();
    const styles: string = (
      AdminLiveOperationsComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('@media (max-width: 40rem)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('.live-operations__backtest-metrics');
    expect(styles).toContain('.live-operations__backtest-method');
  });
});
