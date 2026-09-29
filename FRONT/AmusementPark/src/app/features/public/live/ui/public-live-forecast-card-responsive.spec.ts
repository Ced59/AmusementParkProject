import { PublicLiveForecastCardComponent } from './public-live-forecast-card.component';

interface ComponentDefinitionWithStyles {
  readonly ɵcmp: {
    readonly styles: readonly string[];
  };
}

describe('PublicLiveForecastCardComponent responsive contract', () => {
  it('keeps forecast evidence inside a 320px viewport', () => {
    const definition: ComponentDefinitionWithStyles =
      PublicLiveForecastCardComponent as unknown as ComponentDefinitionWithStyles;
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
