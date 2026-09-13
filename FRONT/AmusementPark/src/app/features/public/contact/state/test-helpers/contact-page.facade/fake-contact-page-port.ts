import { Observable, of } from 'rxjs';

import { ContactGrievanceSubmission, SubmitContactGrievanceRequest } from '@app/models/contact/contact-grievance.models';

import { ContactPageDataPort } from '../../contact-page-data.ports';

export class FakeContactPagePort implements ContactPageDataPort {
  public response$: Observable<ContactGrievanceSubmission> = of({
    accepted: true,
    submittedAtUtc: '2026-06-17T00:00:00Z',
  });
  public readonly calls: SubmitContactGrievanceRequest[] = [];

  submitGrievance(
    request: SubmitContactGrievanceRequest,
  ): Observable<ContactGrievanceSubmission> {
    this.calls.push(request);
    return this.response$;
  }
}
