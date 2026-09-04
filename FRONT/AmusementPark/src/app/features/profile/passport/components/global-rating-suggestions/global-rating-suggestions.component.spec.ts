import { GlobalRatingSuggestionsComponent } from './global-rating-suggestions.component';

describe('GlobalRatingSuggestionsComponent', () => {
  it('keeps every nested grid shrinkable and stacks metrics on mobile', () => {
    const styles: string = (
      GlobalRatingSuggestionsComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('@media (max-width: 520px)');
    expect(styles).toContain('grid-template-columns: 1fr');
  });
});
