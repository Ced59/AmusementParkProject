import { Observable, of, throwError } from 'rxjs';

export class FakeAuthService {
  loggedIn = false;
  token: string | null = null;
  tokenError: unknown | null = null;

  isLoggedIn(): boolean {
    return this.loggedIn;
  }

  ensureValidAccessToken(_silent: boolean): Observable<string | null> {
    if (this.tokenError) {
      return throwError(() => this.tokenError);
    }

    return of(this.token);
  }
}
