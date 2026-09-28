import { AdminLiveTargetMappingsComponent } from './admin-live-target-mappings.component';

describe('admin live target mappings responsive contract', () => {
  it('keeps the page available as a lazy standalone component', () => {
    expect(AdminLiveTargetMappingsComponent).toBeDefined();
  });

  it('defines narrow-screen layout guards without horizontal fixed widths', () => {
    const styles: string = (
      AdminLiveTargetMappingsComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('@media (max-width: 36rem)');
    expect(styles).toContain('minmax(0, 1fr)');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).not.toMatch(/min-width:\s*[4-9]\d{2}px/);
  });
});
