import { ProfileComparisonInvitationCreatorComponent } from '../../components/profile-comparison-invitation-creator/profile-comparison-invitation-creator.component';
import { ProfileComparisonInvitationPageComponent } from './profile-comparison-invitation-page.component';

describe('Profile comparison invitation responsive contract', () => {
  it('contains creator and consent layouts inside narrow viewports', () => {
    const creatorStyles: string = (ProfileComparisonInvitationCreatorComponent as unknown as {
      ɵcmp: { styles: string[] };
    }).ɵcmp.styles.join('\n');
    const consentStyles: string = (ProfileComparisonInvitationPageComponent as unknown as {
      ɵcmp: { styles: string[] };
    }).ɵcmp.styles.join('\n');

    expect(creatorStyles).toContain('minmax(0, 1fr)');
    expect(creatorStyles).toContain('minmax(min(100%, 14rem), 1fr)');
    expect(creatorStyles).toContain('@media (max-width: 480px)');
    expect(consentStyles).toContain('overflow-x: clip');
    expect(consentStyles).toContain('minmax(0, 1fr)');
    expect(consentStyles).toContain('@media (max-width: 520px)');
  });
});
