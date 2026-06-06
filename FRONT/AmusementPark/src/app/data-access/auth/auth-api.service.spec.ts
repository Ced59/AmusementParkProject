import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AuthApiService } from './auth-api.service';

describe('AuthApiService', () => {
  let service: AuthApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(AuthApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('posts login credentials as JSON string with credentials enabled', () => {
    service.login({ email: 'test@example.com', password: 'secret' }).subscribe();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}auth/login`);
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBeTrue();
    expect(request.request.headers.get('Content-Type')).toBe('application/json');
    expect(request.request.body).toBe(JSON.stringify({ email: 'test@example.com', password: 'secret' }));
    request.flush({ token: 'access' });
  });

  it('posts refresh and logout with credentials enabled', () => {
    service.refreshToken().subscribe();
    service.logout().subscribe();

    const refreshRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}auth/refresh-token`);
    expect(refreshRequest.request.withCredentials).toBeTrue();
    refreshRequest.flush({ accessToken: 'next' });

    const logoutRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}auth/logout`);
    expect(logoutRequest.request.withCredentials).toBeTrue();
    logoutRequest.flush(null);
  });

  it('posts account recovery requests to the expected endpoints', () => {
    service.confirmEmail('token').subscribe();
    service.resendConfirmation('test@example.com').subscribe();
    service.forgotPassword('test@example.com').subscribe();
    service.resetPassword('token', 'next', 'next').subscribe();

    const confirmRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}users/confirm-email`);
    expect(confirmRequest.request.body).toEqual({ token: 'token' });
    confirmRequest.flush({ message: 'ok' });

    const resendRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}users/resend-confirmation`);
    expect(resendRequest.request.body).toEqual({ email: 'test@example.com' });
    resendRequest.flush({ message: 'ok' });

    const forgotRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}users/forgot-password`);
    expect(forgotRequest.request.body).toEqual({ email: 'test@example.com' });
    forgotRequest.flush({ message: 'ok' });

    const resetRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}users/reset-password`);
    expect(resetRequest.request.body).toEqual({ token: 'token', newPassword: 'next', newPasswordConfirm: 'next' });
    resetRequest.flush({ message: 'ok' });
  });

  it('posts external login tokens with credentials enabled', () => {
    service.externalLogin('google', 'id-token', 'nonce').subscribe();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}auth/external/google`);
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBeTrue();
    expect(request.request.body).toEqual({ token: 'id-token', nonce: 'nonce' });
    request.flush({ token: 'access' });
  });

  it('gets current user by id', () => {
    service.getCurrentUserById('user-1').subscribe((user) => expect(user.id).toBe('user-1'));

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}users/user-1`);
    expect(request.request.method).toBe('GET');
    request.flush({ id: 'user-1' });
  });
});
