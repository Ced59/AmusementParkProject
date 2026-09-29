import { PublicLivePanelComponent } from './public-live-panel.component';

interface ComponentDefinitionWithStyles {
  readonly ɵcmp: {
    readonly styles: readonly string[];
  };
}

describe('PublicLivePanelComponent responsive contract', () => {
  it('keeps live status, filters and long names inside a 320px viewport', () => {
    const definition: ComponentDefinitionWithStyles = PublicLivePanelComponent as unknown as ComponentDefinitionWithStyles;
    const styles: string = definition.ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('minmax(0, 1fr)');
    expect(styles).toContain('minmax(min(100%, 18rem), 1fr)');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 560px)');
    expect(styles).not.toContain('overflow-x: auto');
  });
});
