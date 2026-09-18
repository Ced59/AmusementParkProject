import { TripInvitationDecisionOperationStore } from './trip-invitation-decision-operation.store';

describe('TripInvitationDecisionOperationStore', () => {
  const token: string = 'opaque-secret-token';
  const store: TripInvitationDecisionOperationStore = new TripInvitationDecisionOperationStore();

  afterEach(() => sessionStorage.clear());

  it('restores the operation without writing the opaque token to storage', () => {
    store.write(token, 'user-1', 'accept', 'operation-1');

    expect(store.read(token, 'user-1')).toEqual(expect.objectContaining({
      decision: 'accept',
      operationId: 'operation-1'
    }));
    expect(Object.keys(sessionStorage).join('|')).not.toContain(token);
    expect(Object.values(sessionStorage).join('|')).not.toContain(token);
  });

  it('does not resume an operation under another account in the same tab', () => {
    store.write(token, 'user-1', 'accept', 'operation-1');

    expect(store.read(token, 'user-2')).toBeNull();
    expect(store.read(token, 'user-1')).toEqual(expect.objectContaining({
      decision: 'accept',
      operationId: 'operation-1'
    }));
  });

  it('keeps independent pending decisions for two accounts on the same invitation', () => {
    store.write(token, 'user-1', 'accept', 'operation-1');
    store.write(token, 'user-2', 'decline', 'operation-2');

    expect(store.read(token, 'user-1')).toEqual(expect.objectContaining({
      decision: 'accept',
      operationId: 'operation-1'
    }));
    expect(store.read(token, 'user-2')).toEqual(expect.objectContaining({
      decision: 'decline',
      operationId: 'operation-2'
    }));
    expect(Object.keys(sessionStorage)).toHaveLength(2);

    store.clear(token, 'user-2');

    expect(store.read(token, 'user-1')).toEqual(expect.objectContaining({
      operationId: 'operation-1'
    }));
    expect(store.read(token, 'user-2')).toBeNull();
  });

  it('drops malformed stored values', () => {
    store.write(token, 'user-1', 'accept', 'operation-1');
    const key: string = Object.keys(sessionStorage)[0];
    sessionStorage.setItem(key, '{broken');

    expect(store.read(token, 'user-1')).toBeNull();
    expect(sessionStorage.getItem(key)).toBeNull();
  });
});
