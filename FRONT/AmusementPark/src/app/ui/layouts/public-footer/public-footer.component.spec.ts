import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { siteVersion } from '../../../../environments/version.generated';
import { PublicFooterComponent } from './public-footer.component';

describe('PublicFooterComponent', () => {
  let fixture: ComponentFixture<PublicFooterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, PublicFooterComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('en', {
      footer: {
        ariaLabel: 'Site footer',
        brand: {
          tagline: 'The complete map of thrills.'
        },
        explore: {
          title: 'Explore',
          allParks: 'All parks',
          topRollerCoasters: 'Top rollercoasters',
          interactiveMap: 'Interactive map',
          news: 'News'
        },
        about: {
          title: 'About',
          project: 'The project',
          sources: 'Sources',
          contact: 'Contact',
          privacy: 'Privacy'
        },
        languages: {
          title: 'Languages'
        },
        bottom: {
          copy: 'Copyright {{year}} AmusementPark',
          version: 'Version {{version}}'
        }
      }
    });
    translateService.use('en');

    fixture = TestBed.createComponent(PublicFooterComponent);
    fixture.detectChanges();
  });

  it('displays the generated site version', () => {
    const textContent = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(textContent).toContain(`Version ${siteVersion}`);
  });
});
