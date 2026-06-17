import { EventEmitter } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';

import { ContactGrievanceSubmission, SubmitContactGrievanceRequest } from '@app/models/contact/contact-grievance.models';
import { TranslationService } from '@app/services/translation.service';
import { CONTACT_PAGE_DATA_PORT, ContactPageDataPort } from './contact-page-data.ports';
import { ContactPageFacade } from './contact-page.facade';

class FakeContactPagePort implements ContactPageDataPort {
  public response$: Observable<ContactGrievanceSubmission> = of({ accepted: true, submittedAtUtc: '2026-06-17T00:00:00Z' });
  public readonly calls: SubmitContactGrievanceRequest[] = [];

  submitGrievance(request: SubmitContactGrievanceRequest): Observable<ContactGrievanceSubmission> {
    this.calls.push(request);
    return this.response$;
  }
}

describe('ContactPageFacade', () => {
  let facade: ContactPageFacade;
  let port: FakeContactPagePort;

  beforeEach(() => {
    port = new FakeContactPagePort();

    TestBed.configureTestingModule({
      providers: [
        ContactPageFacade,
        { provide: CONTACT_PAGE_DATA_PORT, useValue: port },
        {
          provide: TranslationService,
          useValue: {
            languageChanged: new EventEmitter<string>(),
            getCurrentLang: () => 'fr'
          }
        }
      ]
    });

    facade = TestBed.inject(ContactPageFacade);
  });

  it('submits a trimmed grievance with current language and honeypot value', () => {
    facade.submit('  Une suggestion claire.  ', ' hidden-field ');

    expect(port.calls).toEqual([
      {
        message: 'Une suggestion claire.',
        website: 'hidden-field',
        languageCode: 'fr'
      }
    ]);
    expect(facade.submitted()).toBeTrue();
    expect(facade.submitting()).toBeFalse();
    expect(facade.errorKey()).toBeNull();
  });

  it('sets an error key when submission fails', () => {
    port.response$ = throwError(() => new Error('network'));

    facade.submit('Une suggestion claire.', '');

    expect(facade.submitted()).toBeFalse();
    expect(facade.submitting()).toBeFalse();
    expect(facade.errorKey()).toBe('contactPage.form.error');
  });

  it('ignores duplicate submissions while one is in progress', () => {
    const pendingResponse = new Subject<ContactGrievanceSubmission>();
    port.response$ = pendingResponse.asObservable();

    facade.submit('Une suggestion claire.', '');
    facade.submit('Une autre suggestion.', '');

    expect(port.calls.length).toBe(1);

    pendingResponse.next({ accepted: true, submittedAtUtc: '2026-06-17T00:00:00Z' });
    pendingResponse.complete();

    expect(facade.submitted()).toBeTrue();
  });
});
