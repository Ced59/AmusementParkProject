import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { Subject } from 'rxjs';

import { AccountLayoutComponent } from './account-layout.component';

interface ActivatedRouteStub {
  firstChild: {
    snapshot?: {
      data: Record<string, unknown>;
    };
  } | null;
}

describe('AccountLayoutComponent', () => {
  let activatedRoute: ActivatedRouteStub;
  let fixture: ComponentFixture<AccountLayoutComponent>;
  let navigations: Subject<NavigationEnd>;

  beforeEach(async () => {
    navigations = new Subject<NavigationEnd>();
    activatedRoute = {
      firstChild: {
        snapshot: {
          data: { accountLayout: 'wide' }
        }
      }
    };

    TestBed.overrideComponent(AccountLayoutComponent, {
      set: {
        imports: [],
        template: `
          <main [class.app-account-layout__main--wide]="wideLayout()"></main>
        `
      }
    });

    await TestBed.configureTestingModule({
      imports: [AccountLayoutComponent],
      providers: [
        { provide: ActivatedRoute, useValue: activatedRoute },
        { provide: Router, useValue: { events: navigations } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AccountLayoutComponent);
  });

  it('uses the wide shell only for routes that explicitly request it', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('main').classList).toContain('app-account-layout__main--wide');

    activatedRoute.firstChild = {
      snapshot: {
        data: {}
      }
    };
    fixture.destroy();
    fixture = TestBed.createComponent(AccountLayoutComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('main').classList).not.toContain('app-account-layout__main--wide');
  });

  it('keeps the default shell while the child route snapshot is not ready', () => {
    activatedRoute.firstChild = {};
    fixture.destroy();
    fixture = TestBed.createComponent(AccountLayoutComponent);

    expect(() => {
      fixture.detectChanges();
    }).not.toThrow();
    expect(fixture.nativeElement.querySelector('main').classList).not.toContain('app-account-layout__main--wide');
  });

  it('refreshes the OnPush shell when the lazy profile route finishes navigating', () => {
    activatedRoute.firstChild = {};
    fixture.destroy();
    fixture = TestBed.createComponent(AccountLayoutComponent);
    fixture.detectChanges();
    const main: HTMLElement = fixture.nativeElement.querySelector('main');
    expect(main.classList).not.toContain('app-account-layout__main--wide');

    activatedRoute.firstChild = { snapshot: { data: { accountLayout: 'wide' } } };
    navigations.next(new NavigationEnd(1, '/fr/profile', '/fr/profile'));
    fixture.detectChanges();
    expect(main.classList).toContain('app-account-layout__main--wide');

    activatedRoute.firstChild = { snapshot: { data: {} } };
    navigations.next(new NavigationEnd(2, '/fr/forgot-password', '/fr/forgot-password'));
    fixture.detectChanges();
    expect(main.classList).not.toContain('app-account-layout__main--wide');
  });
});
