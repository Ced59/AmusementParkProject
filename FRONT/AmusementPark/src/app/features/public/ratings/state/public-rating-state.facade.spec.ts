import { DestroyRef } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';

import { RatingTargetType, UserRating, UserRatingUpsertRequest } from '@app/models/ratings/rating.models';
import { AuthService } from '@app/services/auth/auth.service';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { ModalService } from '@app/services/modal/modal.service';
import { PublicRatingStateFacade } from './public-rating-state.facade';
import { PublicRatingRatingsPort } from './public-rating-state-data.ports';

class FakeDestroyRef implements DestroyRef {
  readonly destroyed = false;

  onDestroy(callback: () => void): () => void {
    void callback;
    return (): void => undefined;
  }
}

class FakeRatingsPort implements PublicRatingRatingsPort {
  readonly upsertCalls: UserRatingUpsertRequest[] = [];
  readonly getMyRatingCalls: Array<{ targetType: RatingTargetType; targetId: string }> = [];
  ratingResponse: UserRating = createUserRating(4.5, 3, 4.5);
  myRatingResponse: UserRating | null = null;

  getMyRating(targetType: RatingTargetType, targetId: string): Observable<UserRating | null> {
    this.getMyRatingCalls.push({ targetType, targetId });
    return of(this.myRatingResponse);
  }

  upsertRating(request: UserRatingUpsertRequest): Observable<UserRating> {
    this.upsertCalls.push(request);
    return of(this.ratingResponse);
  }
}

class FakeAuthService {
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

class FakeModalService {
  readonly openedModals: string[] = [];

  openModal(id: string): void {
    this.openedModals.push(id);
  }
}

class FakeToastMessageService {
  readonly messages: Array<{ severity: string; summary: string; detail: string }> = [];

  add(severity: 'success' | 'info' | 'warn' | 'error', summary: string, detail: string): void {
    this.messages.push({ severity, summary, detail });
  }
}

class FakeTranslateService {
  instant(key: string): string {
    return key;
  }
}

describe('PublicRatingStateFacade', () => {
  it('loads the existing user rating only for authenticated visitors', () => {
    const port: FakeRatingsPort = new FakeRatingsPort();
    const authService: FakeAuthService = new FakeAuthService();
    authService.loggedIn = true;
    port.myRatingResponse = createUserRating(3.5, 2, 4);
    const facade: PublicRatingStateFacade = createFacade(port, authService);

    facade.configure('Park', ' park-1 ', {
      targetType: 'Park',
      targetId: 'park-1',
      ratingCount: 2,
      averageRating: 4,
      bayesianScore: 3.58
    });

    expect(port.getMyRatingCalls).toEqual([{ targetType: 'Park', targetId: 'park-1' }]);
    expect(facade.userRatingValue()).toBe(3.5);
    expect(facade.summary()?.averageRating).toBe(4);
  });

  it('opens the login modal when an anonymous visitor tries to rate', () => {
    const port: FakeRatingsPort = new FakeRatingsPort();
    const authService: FakeAuthService = new FakeAuthService();
    const modalService: FakeModalService = new FakeModalService();
    const facade: PublicRatingStateFacade = createFacade(port, authService, modalService);

    facade.configure('Park', 'park-1', null);
    facade.rate(4.5);

    expect(port.upsertCalls.length).toBe(0);
    expect(modalService.openedModals).toEqual(['loginModal']);
    expect(facade.messageKey()).toBe('ratings.stars.signInMessage');
  });

  it('restores the rating controls when the session check fails', () => {
    const port: FakeRatingsPort = new FakeRatingsPort();
    const authService: FakeAuthService = new FakeAuthService();
    authService.tokenError = new Error('session');
    const facade: PublicRatingStateFacade = createFacade(port, authService);
    const consoleErrorSpy: jasmine.Spy = spyOn(console, 'error').and.stub();

    facade.configure('Park', 'park-1', null);
    facade.rate(4.5);

    expect(port.upsertCalls.length).toBe(0);
    expect(facade.saving()).toBeFalse();
    expect(facade.messageKey()).toBe('ratings.stars.errorMessage');
    expect(consoleErrorSpy).toHaveBeenCalled();
  });

  it('saves an authenticated rating and replaces the summary with the returned aggregate', () => {
    const port: FakeRatingsPort = new FakeRatingsPort();
    const authService: FakeAuthService = new FakeAuthService();
    authService.token = 'token';
    port.ratingResponse = createUserRating(4.5, 5, 4.2);
    const toastMessageService: FakeToastMessageService = new FakeToastMessageService();
    const facade: PublicRatingStateFacade = createFacade(port, authService, new FakeModalService(), toastMessageService);

    facade.configure('ParkItem', ' item-1 ', null);
    facade.rate(4.5);

    expect(port.upsertCalls).toEqual([{ targetType: 'ParkItem', targetId: 'item-1', value: 4.5 }]);
    expect(facade.userRatingValue()).toBe(4.5);
    expect(facade.summary()?.ratingCount).toBe(5);
    expect(facade.summary()?.averageRating).toBe(4.2);
    expect(facade.saving()).toBeFalse();
    expect(facade.messageKey()).toBe('ratings.stars.savedMessage');
    expect(toastMessageService.messages).toEqual([
      { severity: 'success', summary: 'common.success', detail: 'ratings.stars.savedToast' }
    ]);
  });
});

function createFacade(
  port: FakeRatingsPort,
  authService: FakeAuthService,
  modalService: FakeModalService = new FakeModalService(),
  toastMessageService: FakeToastMessageService = new FakeToastMessageService()
): PublicRatingStateFacade {
  return new PublicRatingStateFacade(
    port,
    authService as unknown as AuthService,
    modalService as unknown as ModalService,
    toastMessageService as unknown as ToastMessageService,
    new FakeTranslateService() as unknown as TranslateService,
    new FakeDestroyRef()
  );
}

function createUserRating(value: number, ratingCount: number, averageRating: number): UserRating {
  return {
    id: 'rating-1',
    targetType: 'ParkItem',
    targetId: 'item-1',
    parkId: 'park-1',
    parkItemCategory: 'Attraction',
    parkItemType: 'RollerCoaster',
    value,
    createdAtUtc: '2026-06-19T10:00:00Z',
    updatedAtUtc: '2026-06-19T10:00:00Z',
    summary: {
      targetType: 'ParkItem',
      targetId: 'item-1',
      ratingCount,
      averageRating,
      bayesianScore: 3.8
    }
  };
}
