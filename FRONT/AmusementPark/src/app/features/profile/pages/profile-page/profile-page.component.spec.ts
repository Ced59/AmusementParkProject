import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { ProfilePageComponent } from './profile-page.component';
import { ProfilePageViewComponent } from './profile-page-view.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { OwnerImageUploadDialogComponent } from '@shared/components/owner-image-upload-dialog/owner-image-upload-dialog.component';

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

  it('routes avatar changes through the current-user upload endpoint', () => {
    fixture.detectChanges();

    const uploadDialog: OwnerImageUploadDialogComponent = fixture.debugElement
      .query(By.directive(OwnerImageUploadDialogComponent))
      .componentInstance as OwnerImageUploadDialogComponent;

    expect(uploadDialog.uploadMode).toBe('current-user-avatar');
  });
});
