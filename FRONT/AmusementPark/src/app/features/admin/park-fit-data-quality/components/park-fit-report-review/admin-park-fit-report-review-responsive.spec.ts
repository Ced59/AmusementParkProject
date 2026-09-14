import { AdminParkFitReportReviewComponent } from './admin-park-fit-report-review.component';

describe('Admin Park Fit report responsive contract', () => {
  it('stacks actions and prevents report cards from overflowing on mobile', () => {
    const styles: string = (
      AdminParkFitReportReviewComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 420px)');
    expect(styles).toContain('flex-direction: column');
  });
});
