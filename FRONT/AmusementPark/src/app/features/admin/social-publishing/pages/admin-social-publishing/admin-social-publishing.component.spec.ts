import { HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';

import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../../../../environments/environment';
import { AdminSocialPublishingComponent } from './admin-social-publishing.component';

interface AdminSocialPublishingHarness {
  publicationForm: FormGroup<{
    message: FormControl<string>;
    url: FormControl<string>;
  }>;
  nextImagePage(): void;
  publish(): void;
}

describe('AdminSocialPublishingComponent', () => {
  let fixture: ComponentFixture<AdminSocialPublishingComponent>;
  let harness: AdminSocialPublishingHarness;
  let httpTestingController: HttpTestingController;

  beforeEach(async () => {
    TestBed.overrideComponent(AdminSocialPublishingComponent, {
      set: {
        imports: [],
        template: '',
      },
    });
    await TestBed.configureTestingModule({
      imports: [AdminSocialPublishingComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    httpTestingController = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(AdminSocialPublishingComponent);
    harness = fixture.componentInstance as unknown as AdminSocialPublishingHarness;
    fixture.detectChanges();
    httpTestingController.expectOne(
      (request) => request.url === `${environment.apiBaseUrl}admin/social-publications`,
    ).flush({
      publishers: [{
        network: 'Facebook',
        displayName: 'Facebook',
        isEnabled: true,
        isConfigured: true,
        targetUrl: 'https://www.facebook.com/test',
        supportsAutomaticParkAnnouncements: true,
      }],
      recentPublications: [],
    });
  });

  afterEach(() => {
    fixture.destroy();
    httpTestingController.verify();
  });

  it('fills the automatic text after a pasted URL without replacing a later custom edit during pagination', () => {
    vi.useFakeTimers();

    try {
      const url: string = 'https://amusement-parks.fun/fr/park/park-1/park-test';
      harness.publicationForm.controls.url.setValue(url);
      vi.advanceTimersByTime(350);

      httpTestingController.expectOne((request) =>
        request.url === `${environment.apiBaseUrl}admin/social-publications/draft`
        && request.params.get('url') === url
        && request.params.get('page') === '1'
      ).flush(createDraft(1));
      fixture.detectChanges();

      expect(harness.publicationForm.controls.message.value).toBe('Texte automatique pour Parc Test');

      harness.publicationForm.controls.message.setValue('Mon texte personnalisé');
      harness.nextImagePage();
      httpTestingController.expectOne((request) =>
        request.url === `${environment.apiBaseUrl}admin/social-publications/draft`
        && request.params.get('page') === '2'
      ).flush(createDraft(2));
      fixture.detectChanges();

      expect(harness.publicationForm.controls.message.value).toBe('Mon texte personnalisé');
    } finally {
      vi.useRealTimers();
    }
  });

  it('does not publish while the form URL no longer matches the loaded draft', () => {
    vi.useFakeTimers();

    try {
      const firstUrl: string = 'https://amusement-parks.fun/fr/park/park-1/park-test';
      harness.publicationForm.controls.url.setValue(firstUrl);
      vi.advanceTimersByTime(350);
      httpTestingController.expectOne((request) =>
        request.url === `${environment.apiBaseUrl}admin/social-publications/draft`
        && request.params.get('url') === firstUrl
      ).flush(createDraft(1));
      fixture.detectChanges();

      harness.publicationForm.controls.url.setValue(
        'https://amusement-parks.fun/fr/park/park-2/autre-parc',
      );
      harness.publish();

      httpTestingController.expectNone((request) =>
        request.url === `${environment.apiBaseUrl}admin/social-publications`
        && request.method === 'POST'
      );
    } finally {
      vi.useRealTimers();
    }
  });
});

function createDraft(currentPage: number) {
  return {
    url: 'https://amusement-parks.fun/fr/park/park-1/park-test',
    defaultMessage: 'Texte automatique pour Parc Test',
    targetKind: 'Park',
    targetName: 'Parc Test',
    imageOwnerType: 'Park',
    imageOwnerId: 'park-1',
    images: {
      data: [{
        id: `image-${currentPage}`,
        label: `Image ${currentPage}`,
        isCurrent: currentPage === 1,
        width: 1200,
        height: 630,
      }],
      pagination: {
        currentPage,
        itemsPerPage: 6,
        totalItems: 7,
        totalPages: 2,
      },
    },
  };
}
