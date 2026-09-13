import { Observable, of } from 'rxjs';

export class FakeAuthService {
  token: string | null = null;
  roles: string[] = [];

  ensureValidAccessToken(_forceRefreshAttempt: boolean): Observable<string | null> {
    return of(this.token);
  }

  hasRole(expectedRole: string): boolean {
    return this.roles.includes(expectedRole);
  }
}
