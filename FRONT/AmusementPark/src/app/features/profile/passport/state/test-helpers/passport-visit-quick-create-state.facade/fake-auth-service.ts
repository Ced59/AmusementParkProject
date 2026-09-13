import { Observable, of } from 'rxjs';

export class FakeAuthService {
  token: string | null = 'token';

  ensureValidAccessToken(_forceRefresh: boolean): Observable<string | null> {
    return of(this.token);
  }
}
