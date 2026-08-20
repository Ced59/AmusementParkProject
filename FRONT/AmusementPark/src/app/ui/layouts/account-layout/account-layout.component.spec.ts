import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';

import { AccountLayoutComponent } from './account-layout.component';

interface ActivatedRouteStub {
  firstChild: {
    snapshot: {
      data: Record<string, unknown>;
    };
  } | null;
}

describe('AccountLayoutComponent', () => {
  let activatedRoute: ActivatedRouteStub;
  let fixture: ComponentFixture<AccountLayoutComponent>;

  beforeEach(async () => {
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
          <main [class.app-account-layout__main--wide]="wideLayout"></main>
        `
      }
    });

    await TestBed.configureTestingModule({
      imports: [AccountLayoutComponent],
      providers: [
        { provide: ActivatedRoute, useValue: activatedRoute }
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
});
