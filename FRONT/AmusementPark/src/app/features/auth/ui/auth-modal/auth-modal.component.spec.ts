import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';

import { AuthModalComponent } from './auth-modal.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';

describe('AuthModalComponent', () => {
  let component: AuthModalComponent;
  let fixture: ComponentFixture<AuthModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AuthModalComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(AuthModalComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('restores a protected local destination after password sign-in', async () => {
    const router: Router = TestBed.inject(Router);
    vi.spyOn(router, 'url', 'get').mockReturnValue(
      '/fr/home?returnUrl=%2Ffr%2Fprofile%2Fpassport%23passport-anonymous-import'
    );
    const navigateByUrl = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    await component.onLoginSuccess();

    expect(navigateByUrl).toHaveBeenLastCalledWith(
      '/fr/profile/passport#passport-anonymous-import'
    );
  });

  it('does not navigate to an external return destination', async () => {
    const router: Router = TestBed.inject(Router);
    vi.spyOn(router, 'url', 'get')
      .mockReturnValue('/fr/home?returnUrl=https%3A%2F%2Fexample.org');
    const navigateByUrl = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    await component.onLoginSuccess();

    expect(navigateByUrl).not.toHaveBeenCalled();
  });
});
