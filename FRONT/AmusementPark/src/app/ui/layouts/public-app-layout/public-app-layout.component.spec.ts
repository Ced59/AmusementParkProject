import { isPlatformBrowser, NgComponentOutlet } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, NO_ERRORS_SCHEMA, PLATFORM_ID, Type } from '@angular/core';
import { of } from 'rxjs';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AuthService } from '@app/services/auth/auth.service';
import { SharedService } from '@app/services/shared/shared.service';
import { AdminPublicViewModeFacade } from '@features/admin/contextual-editing/state/admin-public-view-mode.facade';
import { PublicParkNavigationTreeFacade } from '@features/public/navigation/state/public-park-navigation-tree.facade';
import { PublicAppLayoutComponent } from './public-app-layout.component';

class PublicParkNavigationTreeFacadeStub {
  public readonly initialize = jasmine.createSpy('initialize');
}

@Component({
  selector: 'app-test-admin-public-view-toolbar',
  template: '<div class="admin-public-view-toolbar"></div>'
})
class TestAdminPublicViewToolbarComponent {
}

describe('PublicAppLayoutComponent', () => {
  let authService: jasmine.SpyObj<AuthService>;
  let fixture: ComponentFixture<PublicAppLayoutComponent>;

  beforeEach(async () => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['hasRole', 'isLoggedIn']);
    authService.isLoggedIn.and.returnValue(false);
    authService.hasRole.and.returnValue(false);

    const sharedService: jasmine.SpyObj<SharedService> = jasmine.createSpyObj<SharedService>('SharedService', ['getLoginStatusListener']);
    sharedService.getLoginStatusListener.and.returnValue(of());

    TestBed.overrideComponent(PublicAppLayoutComponent, {
      set: {
        imports: [NgComponentOutlet],
        providers: [
          { provide: PublicParkNavigationTreeFacade, useClass: PublicParkNavigationTreeFacadeStub }
        ],
        schemas: [NO_ERRORS_SCHEMA]
      }
    });

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, PublicAppLayoutComponent],
      providers: [
        ...provideCommonTestDependencies(),
        AdminPublicViewModeFacade,
        { provide: AuthService, useValue: authService },
        { provide: SharedService, useValue: sharedService },
        { provide: PublicParkNavigationTreeFacade, useClass: PublicParkNavigationTreeFacadeStub },
        { provide: PLATFORM_ID, useValue: 'browser' }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();
  });

  it('does not render the admin toolbar for anonymous visitors', async () => {
    fixture = TestBed.createComponent(PublicAppLayoutComponent);
    fixture.detectChanges();

    await fixture.whenStable();
    fixture.detectChanges();

    const publicParkNavigationTreeFacade: PublicParkNavigationTreeFacadeStub = getNavigationTreeFacade(fixture);
    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('app-admin-public-view-toolbar')).toBeNull();
    expect(publicParkNavigationTreeFacade.initialize).toHaveBeenCalled();
  });

  it('lazy-renders the admin toolbar for authenticated admins in the browser', async () => {
    expect(isPlatformBrowser(TestBed.inject(PLATFORM_ID))).toBeTrue();
    authService.isLoggedIn.and.returnValue(true);
    authService.hasRole.and.returnValue(true);

    fixture = TestBed.createComponent(PublicAppLayoutComponent);
    spyOn(getPublicAppLayoutPrivateApi(fixture), 'loadAdminToolbarComponent')
      .and.returnValue(Promise.resolve(TestAdminPublicViewToolbarComponent));
    fixture.detectChanges();

    await fixture.whenStable();
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('.admin-public-view-toolbar')).not.toBeNull();
    expect(authService.hasRole).toHaveBeenCalledWith('ADMIN');
  });

  it('does not lazy-load the admin toolbar during SSR even for admins', async () => {
    TestBed.overrideProvider(PLATFORM_ID, { useValue: 'server' });
    authService.isLoggedIn.and.returnValue(true);
    authService.hasRole.and.returnValue(true);

    fixture = TestBed.createComponent(PublicAppLayoutComponent);
    const loadToolbarSpy: jasmine.Spy = spyOn(getPublicAppLayoutPrivateApi(fixture), 'loadAdminToolbarComponent')
      .and.returnValue(Promise.resolve(TestAdminPublicViewToolbarComponent));
    fixture.detectChanges();

    await fixture.whenStable();
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(isPlatformBrowser(TestBed.inject(PLATFORM_ID))).toBeFalse();
    expect(loadToolbarSpy).not.toHaveBeenCalled();
    expect(host.querySelector('.admin-public-view-toolbar')).toBeNull();
  });
});

function getNavigationTreeFacade(fixture: ComponentFixture<PublicAppLayoutComponent>): PublicParkNavigationTreeFacadeStub {
  return fixture.debugElement.injector.get(PublicParkNavigationTreeFacade) as unknown as PublicParkNavigationTreeFacadeStub;
}

function getPublicAppLayoutPrivateApi(fixture: ComponentFixture<PublicAppLayoutComponent>): {
  loadAdminToolbarComponent: () => Promise<Type<unknown>>;
} {
  return fixture.componentInstance as unknown as {
    loadAdminToolbarComponent: () => Promise<Type<unknown>>;
  };
}
