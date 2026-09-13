import { Observable, of } from 'rxjs';

import { ResetPasswordPageStateAuthApiServicePort } from '../../reset-password-page-state-data.ports';

export class FakeAuthPort implements ResetPasswordPageStateAuthApiServicePort {
  public response$: Observable<{
    message: string;
  }> = of({ message: 'Mot de passe réinitialisé.' });
  public readonly calls: {
    token: string;
    newPassword: string;
    confirmPassword: string;
  }[] = [];

  resetPassword(
    token: string,
    newPassword: string,
    confirmPassword: string,
  ): Observable<{
    message: string;
  }> {
    this.calls.push({ token, newPassword, confirmPassword });
    return this.response$;
  }
}
