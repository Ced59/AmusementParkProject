import { AdminHistoryDiagnosticsComponent } from './admin-history-diagnostics.component';

describe('admin history diagnostics responsive contract', () => {
  it('contains long content and collapses every multi-column layout on mobile', () => {
    const styles: string = (
      AdminHistoryDiagnosticsComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 42rem)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('flex-direction: column');
  });
});
