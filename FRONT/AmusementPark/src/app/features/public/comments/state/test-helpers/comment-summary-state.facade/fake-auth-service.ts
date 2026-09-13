import { Observable, of } from 'rxjs';

export class FakeAuthService {
  ensureValidAccessToken(): Observable<string | null> {
    return of(null);
  }

  hasRole(): boolean {
    return false;
  }
}
