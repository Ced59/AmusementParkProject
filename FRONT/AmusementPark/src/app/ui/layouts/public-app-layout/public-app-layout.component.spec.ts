import type { Mock, MockedObject } from 'vitest';
import { isPlatformBrowser, NgComponentOutlet } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, NO_ERRORS_SCHEMA, PLATFORM_ID, Type } from '@angular/core';
import { of } from 'rxjs';

import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import { AuthService } from '@app/services/auth/auth.service';
import { SharedService } from '@app/services/shared/shared.service';
import { AdminPublicViewModeFacade } from '@features/admin/contextual-editing/state/admin-public-view-mode.facade';
import { PublicParkNavigationTreeFacade } from '@features/public/navigation/state/public-park-navigation-tree.facade';
import { PublicAppLayoutComponent } from './public-app-layout.component';

class PublicParkNavigationTreeFacadeStub {
  public readonly initialize = vi.fn();
}

@Component({
  selector: 'app-test-admin-public-view-toolbar',
  template: '<div class="admin-public-view-toolbar"></div>',
})
class TestAdminPublicViewToolbarComponent {}

describe('PublicAppLayoutComponent', () => {
  let authService: MockedObject<AuthService>;
  let fixture: ComponentFixture<PublicAppLayoutComponent>;

  beforeEach(async () => {
    authService = {
      hasRole: vi.fn().mockName('AuthService.hasRole'),
      isLoggedIn: vi.fn().mockName('AuthService.isLoggedIn'),
    } as unknown as MockedObject<AuthService>;
    authService.isLoggedIn.mockReturnValue(false);
    authService.hasRole.mockReturnValue(false);

    const sharedService: MockedObject<SharedService> = {
      getLoginStatusListener: vi
        .fn()
        .mockName('SharedService.getLoginStatusListener'),
    } as unknown as MockedObject<SharedService>;
    sharedService.getLoginStatusListener.mockReturnValue(of());

    TestBed.overrideComponent(PublicAppLayoutComponent, {
      set: {
        imports: [NgComponentOutlet],
        providers: [
          {
            provide: PublicParkNavigationTreeFacade,
            useClass: PublicParkNavigationTreeFacadeStub,
          },
        ],
        schemas: [NO_ERRORS_SCHEMA],
      },
    });

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, PublicAppLayoutComponent],
      providers: [
        ...provideCommonTestDependencies(),
        AdminPublicViewModeFacade,
        { provide: AuthService, useValue: authService },
        { provide: SharedService, useValue: sharedService },
        {
          provide: PublicParkNavigationTreeFacade,
          useClass: PublicParkNavigationTreeFacadeStub,
        },
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();
  });

  it('does not render the admin toolbar for anonymous visitors', async () => {
    fixture = TestBed.createComponent(PublicAppLayoutComponent);
    fixture.detectChanges();

    await fixture.whenStable();
    fixture.detectChanges();

    const publicParkNavigationTreeFacade: PublicParkNavigationTreeFacadeStub =
      getNavigationTreeFacade(fixture);
    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('app-admin-public-view-toolbar')).toBeNull();
    expect(publicParkNavigationTreeFacade.initialize).toHaveBeenCalled();
  });

  it('uses wider desktop content while preserving compact mobile gutters', () => {
    const styles: string = (
      PublicAppLayoutComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('--content-max-width: 92rem');
    expect(styles).toContain('--content-wide-max-width: 100rem');
    expect(styles).toContain('width: min(100% - 1.5rem, var(--content-wide-max-width))');
    expect(styles).toContain('width: min(100% - 1rem, var(--content-max-width))');
  });

  it('lazy-renders the admin toolbar for authenticated admins in the browser', async () => {
    expect(isPlatformBrowser(TestBed.inject(PLATFORM_ID))).toBe(true);
    authService.isLoggedIn.mockReturnValue(true);
    authService.hasRole.mockReturnValue(true);

    fixture = TestBed.createComponent(PublicAppLayoutComponent);
    vi.spyOn(
      getPublicAppLayoutPrivateApi(fixture),
      'loadAdminToolbarComponent',
    ).mockReturnValue(Promise.resolve(TestAdminPublicViewToolbarComponent));
    fixture.detectChanges();

    await fixture.whenStable();
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('.admin-public-view-toolbar')).not.toBeNull();
    expect(authService.hasRole).toHaveBeenCalledWith('ADMIN');
  });

  it('does not lazy-load the admin toolbar during SSR even for admins', async () => {
    TestBed.overrideProvider(PLATFORM_ID, { useValue: 'server' });
    authService.isLoggedIn.mockReturnValue(true);
    authService.hasRole.mockReturnValue(true);

    fixture = TestBed.createComponent(PublicAppLayoutComponent);
    const loadToolbarSpy: Mock = vi
      .spyOn(getPublicAppLayoutPrivateApi(fixture), 'loadAdminToolbarComponent')
      .mockReturnValue(Promise.resolve(TestAdminPublicViewToolbarComponent));
    fixture.detectChanges();

    await fixture.whenStable();
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(isPlatformBrowser(TestBed.inject(PLATFORM_ID))).toBe(false);
    expect(loadToolbarSpy).not.toHaveBeenCalled();
    expect(host.querySelector('.admin-public-view-toolbar')).toBeNull();
  });
});

function getNavigationTreeFacade(
  fixture: ComponentFixture<PublicAppLayoutComponent>,
): PublicParkNavigationTreeFacadeStub {
  return fixture.debugElement.injector.get(
    PublicParkNavigationTreeFacade,
  ) as unknown as PublicParkNavigationTreeFacadeStub;
}

function getPublicAppLayoutPrivateApi(
  fixture: ComponentFixture<PublicAppLayoutComponent>,
): {
  loadAdminToolbarComponent: () => Promise<Type<unknown>>;
} {
  return fixture.componentInstance as unknown as {
    loadAdminToolbarComponent: () => Promise<Type<unknown>>;
  };
}
