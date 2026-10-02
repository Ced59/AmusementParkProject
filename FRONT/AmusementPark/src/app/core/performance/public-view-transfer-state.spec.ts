import { PLATFORM_ID, TransferState } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PublicViewTransferState } from './public-view-transfer-state';

describe('PublicViewTransferState', () => {
  it('keeps server values available during repeated reads within the same render', () => {
    TestBed.configureTestingModule({ providers: [{ provide: PLATFORM_ID, useValue: 'server' }] });
    const transfer = TestBed.inject(PublicViewTransferState);
    transfer.store('public-view', []);
    expect(transfer.consume('public-view')).toEqual([]);
    expect(transfer.consume('public-view')).toEqual([]);
    expect(TestBed.inject(TransferState).toJson()).toContain('public-view');
  });

  it('consumes server values only once in the browser, including empty results', () => {
    const state = TestBed.inject(TransferState);
    const serverTransfer = new PublicViewTransferState(state, 'server');
    serverTransfer.store('public-view', []);
    const transfer = TestBed.inject(PublicViewTransferState);
    expect(transfer.consume('public-view')).toEqual([]);
    expect(transfer.consume('public-view')).toBeUndefined();
  });

  it('does not retain subsequent browser requests as transferred data', () => {
    const transfer = TestBed.inject(PublicViewTransferState);
    transfer.store('public-view', ['updated']);
    expect(transfer.consume('public-view')).toBeUndefined();
  });
});
