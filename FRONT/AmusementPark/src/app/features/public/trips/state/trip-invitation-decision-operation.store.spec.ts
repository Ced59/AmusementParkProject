import { TripInvitationDecisionOperationStore } from './trip-invitation-decision-operation.store';

describe('TripInvitationDecisionOperationStore', () => {
  const token: string = 'opaque-secret-token';
  const store: TripInvitationDecisionOperationStore = new TripInvitationDecisionOperationStore();

  afterEach(() => sessionStorage.clear());

  it('restores the operation without writing the opaque token to storage', () => {
    store.write(token, 'accept', 'operation-1');

    expect(store.read(token)).toEqual({ decision: 'accept', operationId: 'operation-1' });
    expect(Object.keys(sessionStorage).join('|')).not.toContain(token);
    expect(Object.values(sessionStorage).join('|')).not.toContain(token);
  });

  it('drops malformed stored values', () => {
    store.write(token, 'accept', 'operation-1');
    const key: string = Object.keys(sessionStorage)[0];
    sessionStorage.setItem(key, '{broken');

    expect(store.read(token)).toBeNull();
    expect(sessionStorage.getItem(key)).toBeNull();
  });
});
