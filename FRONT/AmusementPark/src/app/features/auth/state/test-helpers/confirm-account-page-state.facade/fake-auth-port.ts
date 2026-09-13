import { Observable, of } from 'rxjs';

import { ConfirmAccountPageStateAuthApiServicePort } from '../../confirm-account-page-state-data.ports';

export class FakeAuthPort implements ConfirmAccountPageStateAuthApiServicePort {
  public response$: Observable<{
    message: string;
  }> = of({ message: 'Compte confirmé.' });
  public readonly calls: string[] = [];

  confirmEmail(token: string): Observable<{
    message: string;
  }> {
    this.calls.push(token);
    return this.response$;
  }
}
