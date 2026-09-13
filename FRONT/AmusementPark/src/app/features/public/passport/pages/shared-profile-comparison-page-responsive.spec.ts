import { ProfileComparisonManagementComponent } from '../../../profile/passport/components/profile-comparison-management/profile-comparison-management.component';
import { SharedProfileComparisonPageComponent } from './shared-profile-comparison-page.component';

describe('Shared profile comparison responsive contract', () => {
  it('contains every comparison surface inside narrow viewports', () => {
    const pageStyles: string = (
      SharedProfileComparisonPageComponent as unknown as {
        ɵcmp: { styles: string[] };
      }
    ).ɵcmp.styles.join('\n');
    const managementStyles: string = (
      ProfileComparisonManagementComponent as unknown as {
        ɵcmp: { styles: string[] };
      }
    ).ɵcmp.styles.join('\n');

    expect(pageStyles).toContain('overflow-x: clip');
    expect(pageStyles).toContain('minmax(0, 1fr)');
    expect(pageStyles).toContain('minmax(min(100%, 18rem), 1fr)');
    expect(pageStyles).toContain('@media (max-width: 480px)');
    expect(managementStyles).toContain('overflow-x: clip');
    expect(managementStyles).toContain('minmax(min(100%, 22rem), 1fr)');
    expect(managementStyles).toContain('@media (max-width: 420px)');
  });
});
