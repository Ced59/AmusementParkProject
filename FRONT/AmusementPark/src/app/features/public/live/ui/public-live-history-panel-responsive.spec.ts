import { PublicLiveHistoryPanelComponent } from './public-live-history-panel.component';

interface ComponentDefinitionWithStyles {
  readonly ɵcmp: {
    readonly styles: readonly string[];
  };
}

describe('PublicLiveHistoryPanelComponent responsive contract', () => {
  it('keeps the hourly chart inside narrow mobile viewports without horizontal scrolling', () => {
    const definition: ComponentDefinitionWithStyles =
      PublicLiveHistoryPanelComponent as unknown as ComponentDefinitionWithStyles;
    const styles: string = definition.ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow: hidden');
    expect(styles).toContain('@media (max-width: 430px)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).not.toContain('overflow-x: auto');
    expect(styles).not.toContain('width: 100vw');
  });
});
