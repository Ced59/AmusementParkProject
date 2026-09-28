import { AdminHistoryWorkbenchComponent } from './admin-history-workbench.component';

describe('admin history workbench responsive contract', () => {
  it('contains long content and collapses every multi-column layout on mobile', () => {
    const styles: string = (
      AdminHistoryWorkbenchComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 48rem)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('flex: 1 1 100%');
  });

  it('does not expose an edit action for terminal retracted resources', () => {
    const harness = workbenchPrototype();

    expect(harness.canEdit('Retracted')).toBe(false);
    expect(harness.canEdit('Published')).toBe(true);
    expect(harness.canEdit('Draft')).toBe(true);
  });
});

function workbenchPrototype(): {
  canEdit: (stage: 'Draft' | 'Published' | 'Retracted') => boolean;
} {
  return AdminHistoryWorkbenchComponent.prototype as unknown as {
    canEdit: (stage: 'Draft' | 'Published' | 'Retracted') => boolean;
  };
}
