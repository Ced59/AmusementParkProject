import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Router } from '@angular/router';

import { ProfilePageComponent } from './profile-page.component';
import { ProfilePageViewComponent } from './profile-page-view.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { OwnerImageUploadDialogComponent } from '@shared/components/owner-image-upload-dialog/owner-image-upload-dialog.component';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { ProfilePageStateFacade } from '@features/profile/state/profile-page-state.facade';
import { UserDto } from '@app/models/users/user_dto';

describe('ProfilePageComponent', () => {
  let component: ProfilePageComponent;
  let fixture: ComponentFixture<ProfilePageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, ProfilePageComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(ProfilePageComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('keeps the profile avatar round and constrained on mobile', () => {
    const styles: string = (
      ProfilePageViewComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('.profile-avatar');
    expect(styles).toContain('border-radius: 50%');
    expect(styles).toContain('height: 7.5rem');
    expect(styles).toContain('width: 7.5rem');
    expect(styles).toContain('object-fit: cover');
  });

  it('uses the wide responsive profile workspace', () => {
    const styles: string = (
      ProfilePageViewComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: none');
    expect(styles).toContain('grid-template-columns: minmax(13.5rem, 15.5rem) minmax(0, 1fr)');
    expect(styles).toContain('@media (max-width: 1080px)');
  });

  it('routes avatar changes through the current-user upload endpoint', () => {
    fixture.detectChanges();

    const uploadDialog: OwnerImageUploadDialogComponent = fixture.debugElement
      .query(By.directive(OwnerImageUploadDialogComponent))
      .componentInstance as OwnerImageUploadDialogComponent;

    expect(uploadDialog.uploadMode).toBe('current-user-avatar');
  });

  it('lets the shared image component resolve the raw avatar path exactly once', () => {
    fixture.detectChanges();
    const stateFacade: ProfilePageStateFacade = fixture.debugElement.injector.get(ProfilePageStateFacade);
    const user: UserDto = createUser({
      avatarUrl: '/images/avatar-user-1',
    });

    stateFacade.setUser(user);
    fixture.detectChanges();

    const imageDisplay: ImageDisplayComponent = fixture.debugElement
      .query(By.directive(ImageDisplayComponent))
      .componentInstance as ImageDisplayComponent;

    expect(imageDisplay.imagePathOrUrl).toBe('/images/avatar-user-1');
    expect(imageDisplay.resolvedImageUrl).not.toContain('/api/api/images/');
  });

  it('uses the configured public nickname for ranking sharing with the technical identifier as fallback', () => {
    fixture.detectChanges();
    const stateFacade: ProfilePageStateFacade = fixture.debugElement.injector.get(ProfilePageStateFacade);
    const user: UserDto = createUser({
      firstName: 'Private',
      lastName: 'Name',
      publicDisplayName: 'CoasterCamille',
    });

    stateFacade.setUser(user);
    fixture.detectChanges();
    const tabs: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('.profile-tabs__button');
    tabs[1]?.click();
    fixture.detectChanges();

    const ratingsComponent = fixture.debugElement
      .query(By.css('app-profile-ratings-panel'))
      .componentInstance as { displayName: string };

    expect(ratingsComponent.displayName).toBe('CoasterCamille');

    stateFacade.setUser({ ...user, publicDisplayName: null });
    fixture.detectChanges();

    expect(ratingsComponent.displayName).toBe('User0001');
    expect(ratingsComponent.displayName).not.toBe('Private');
  });

  it('exposes a separate passport entry point and keeps visit creation available', () => {
    fixture.detectChanges();
    const stateFacade: ProfilePageStateFacade = fixture.debugElement.injector.get(ProfilePageStateFacade);
    stateFacade.setUser(createUser());
    fixture.detectChanges();
    const actions: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll(
      '.profile-passport-entry__actions button'
    );

    expect(actions).toHaveLength(2);
  });

  it('navigates from the profile to the localized passport overview', () => {
    const router: Router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);

    component.openPassport();

    expect(router.navigate).toHaveBeenCalledWith(['/', 'en', 'profile', 'passport']);
  });
});

function createUser(overrides: Partial<UserDto> = {}): UserDto {
  return {
    id: 'user-1',
    email: 'camille@example.com',
    firstName: 'Camille',
    lastName: 'Martin',
    publicDisplayName: 'CoasterCamille',
    publicIdentifier: 'User0001',
    isActivated: true,
    isBlocked: false,
    roles: ['USER'],
    preferredLanguage: 'FR',
    preferredMeasurementSystem: 'Metric',
    avatarUrl: '',
    createdAt: '2026-08-21T08:00:00Z',
    updatedAt: '2026-08-21T08:00:00Z',
    ...overrides,
  };
}
