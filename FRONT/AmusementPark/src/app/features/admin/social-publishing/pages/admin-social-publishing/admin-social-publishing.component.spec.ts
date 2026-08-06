import { HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
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
    ).flush({ publishers: [], recentPublications: [] });
  });

  afterEach(() => {
    fixture.destroy();
    httpTestingController.verify();
  });

  it('fills the automatic text after a pasted URL without replacing a later custom edit during pagination', fakeAsync(() => {
    const url: string = 'https://amusement-parks.fun/fr/park/park-1/park-test';
    harness.publicationForm.controls.url.setValue(url);
    tick(350);

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
  }));
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
