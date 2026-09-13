import { Observable, of } from 'rxjs';

import { ForgotPasswordPageStateAuthApiServicePort } from '../../forgot-password-page-state-data.ports';

export class FakeAuthPort implements ForgotPasswordPageStateAuthApiServicePort {
  public response$: Observable<{
    message: string;
  }> = of({ message: 'Email envoyé.' });
  public readonly calls: string[] = [];

  forgotPassword(email: string): Observable<{
    message: string;
  }> {
    this.calls.push(email);
    return this.response$;
  }
}
