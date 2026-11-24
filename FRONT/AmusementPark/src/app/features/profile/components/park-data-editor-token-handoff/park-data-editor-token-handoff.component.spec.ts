import { webcrypto } from 'node:crypto';
import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ParkDataEditorTokenApiService } from '@data-access/users/park-data-editor-token-api.service';
import { ParkDataEditorToken } from '@app/models/users/user-admin-responses';
import { ParkDataEditorTokenHandoffComponent } from './park-data-editor-token-handoff.component';

describe('ParkDataEditorTokenHandoffComponent', () => {
  const rawToken = 'apf_pde_0123456789abcdef0123456789abcdef.' + 'a'.repeat(43);
  const activeToken: ParkDataEditorToken = {
    id: 'token-1', label: 'Previous Codex session', displayPrefix: 'apf_pde_abcd',
    createdAtUtc: '2026-11-16T00:00:00Z', expiresAtUtc: '2026-12-16T00:00:00Z',
    lastUsedAtUtc: null, revokedAtUtc: null, revokedByUserId: null, revocationReason: null, isActive: true
  };
  const api = {
    createToken: vi.fn(() => of({
      token: { id: 'token-1', expiresAtUtc: '2026-12-14T00:00:00Z' },
      plainTextToken: rawToken
    })),
    listTokens: vi.fn(() => of([] as ParkDataEditorToken[])),
    revoke: vi.fn(() => of(undefined))
  };

  beforeEach(async () => {
    vi.stubGlobal('crypto', webcrypto);
    api.createToken.mockReset();
    api.createToken.mockImplementation(() => of({
      token: { id: 'token-1', expiresAtUtc: '2026-12-14T00:00:00Z' }, plainTextToken: rawToken
    }));
    api.listTokens.mockReset();
    api.listTokens.mockImplementation(() => of([] as ParkDataEditorToken[]));
    api.revoke.mockReset();
    api.revoke.mockImplementation(() => of(undefined));
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, ParkDataEditorTokenHandoffComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: ParkDataEditorTokenApiService, useValue: api }
      ]
    }).compileComponents();
  });

  afterEach(() => vi.unstubAllGlobals());

  it('does not create a token for an invalid recipient key', async () => {
    const fixture: ComponentFixture<ParkDataEditorTokenHandoffComponent> =
      TestBed.createComponent(ParkDataEditorTokenHandoffComponent);
    const component = fixture.componentInstance as unknown as {
      publicKeyPem: string;
      createPackage(): Promise<void>;
    };
    component.publicKeyPem = 'invalid key';
    await component.createPackage();

    expect(api.createToken).not.toHaveBeenCalled();
  });

  it('shows only the encrypted package after token creation', async () => {
    const subtle: SubtleCrypto = webcrypto.subtle as unknown as SubtleCrypto;
    const keyPair: CryptoKeyPair = await subtle.generateKey(
      { name: 'RSA-OAEP', modulusLength: 2048, publicExponent: new Uint8Array([1, 0, 1]), hash: 'SHA-256' },
      true,
      ['encrypt', 'decrypt']) as CryptoKeyPair;
    const spki: ArrayBuffer = await subtle.exportKey('spki', keyPair.publicKey);
    const pem: string = `-----BEGIN PUBLIC KEY-----\n${btoa(String.fromCharCode(...new Uint8Array(spki)))}\n-----END PUBLIC KEY-----`;
    const fixture: ComponentFixture<ParkDataEditorTokenHandoffComponent> =
      TestBed.createComponent(ParkDataEditorTokenHandoffComponent);
    const component = fixture.componentInstance as unknown as {
      publicKeyPem: string;
      createPackage(): Promise<void>;
    };
    component.publicKeyPem = pem;

    await component.createPackage();
    fixture.detectChanges();

    expect(api.createToken).toHaveBeenCalledOnce();
    const rendered: HTMLElement = fixture.nativeElement as HTMLElement;
    const packageField: HTMLTextAreaElement | null = rendered.querySelector('#park-editor-encrypted-package');
    expect(packageField?.value.startsWith('APF-PDE1.')).toBe(true);
    expect(rendered.textContent).not.toContain(rawToken);
    expect(packageField?.value).not.toContain(rawToken);
  });

  it('revokes a newly created token if browser encryption fails', async () => {
    const subtle = {
      importKey: vi.fn().mockResolvedValue({
        algorithm: { modulusLength: 2048, publicExponent: new Uint8Array([1, 0, 1]) }
      }),
      encrypt: vi.fn().mockRejectedValue(new Error('Encryption failed'))
    } as unknown as SubtleCrypto;
    vi.stubGlobal('crypto', { subtle });
    const fixture: ComponentFixture<ParkDataEditorTokenHandoffComponent> =
      TestBed.createComponent(ParkDataEditorTokenHandoffComponent);
    const component = fixture.componentInstance as unknown as {
      publicKeyPem: string;
      createPackage(): Promise<void>;
      encryptedPackage(): string | null;
    };
    component.publicKeyPem = '-----BEGIN PUBLIC KEY-----\nAQAB\n-----END PUBLIC KEY-----';

    await component.createPackage();

    expect(api.createToken).toHaveBeenCalledOnce();
    expect(api.revoke).toHaveBeenCalledWith('token-1');
    expect(component.encryptedPackage()).toBeNull();
  });

  it('explains the active-token limit and shows existing tokens', async () => {
    api.createToken.mockReturnValue(throwError(() => new HttpErrorResponse({
      status: 400, error: { errorCode: 'park-data-editor-token.active-limit-reached' }
    })));
    api.listTokens.mockReturnValue(of([activeToken]));
    const fixture = TestBed.createComponent(ParkDataEditorTokenHandoffComponent);
    const component = fixture.componentInstance as unknown as {
      publicKeyPem: string;
      createPackage(): Promise<void>;
      errorKey(): string | null;
      activeTokens(): ParkDataEditorToken[];
    };
    const subtle: SubtleCrypto = webcrypto.subtle as unknown as SubtleCrypto;
    const keyPair = await subtle.generateKey(
      { name: 'RSA-OAEP', modulusLength: 2048, publicExponent: new Uint8Array([1, 0, 1]), hash: 'SHA-256' },
      true, ['encrypt', 'decrypt']) as CryptoKeyPair;
    const spki = await subtle.exportKey('spki', keyPair.publicKey);
    component.publicKeyPem = `-----BEGIN PUBLIC KEY-----\n${btoa(String.fromCharCode(...new Uint8Array(spki)))}\n-----END PUBLIC KEY-----`;

    await component.createPackage();
    await fixture.whenStable();

    expect(component.errorKey()).toBe('parkEditorAccess.activeLimit');
    expect(api.listTokens).toHaveBeenCalledOnce();
    expect(component.activeTokens()).toEqual([activeToken]);
  });

  it('lets the account owner revoke a selected active token', async () => {
    vi.stubGlobal('confirm', vi.fn(() => true));
    api.listTokens.mockReturnValue(of([activeToken]));
    const fixture = TestBed.createComponent(ParkDataEditorTokenHandoffComponent);
    const component = fixture.componentInstance as unknown as {
      loadTokens(): Promise<void>;
      revokeToken(tokenId: string): Promise<void>;
      activeTokens(): ParkDataEditorToken[];
    };

    await component.loadTokens();
    await component.revokeToken('token-1');

    expect(api.revoke).toHaveBeenCalledWith('token-1');
    expect(component.activeTokens()).toEqual([]);
  });

  it('shows a lookup error instead of an empty list when token loading fails', async () => {
    api.listTokens.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
    const fixture = TestBed.createComponent(ParkDataEditorTokenHandoffComponent);
    const component = fixture.componentInstance as unknown as { loadTokens(): Promise<void> };

    await component.loadTokens();
    fixture.detectChanges();

    const tokenSection = (fixture.nativeElement as HTMLElement).querySelector('.editor-handoff__tokens');
    expect(tokenSection?.querySelector('.editor-handoff__error[role="alert"]')).not.toBeNull();
    expect(tokenSection?.querySelectorAll('p')).toHaveLength(1);
  });

  it('identifies an expired sign-in session', async () => {
    api.createToken.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 401 })));
    const fixture = TestBed.createComponent(ParkDataEditorTokenHandoffComponent);
    const component = fixture.componentInstance as unknown as {
      publicKeyPem: string;
      createPackage(): Promise<void>;
      errorKey(): string | null;
    };
    const subtle: SubtleCrypto = webcrypto.subtle as unknown as SubtleCrypto;
    const keyPair = await subtle.generateKey(
      { name: 'RSA-OAEP', modulusLength: 2048, publicExponent: new Uint8Array([1, 0, 1]), hash: 'SHA-256' },
      true, ['encrypt', 'decrypt']) as CryptoKeyPair;
    const spki = await subtle.exportKey('spki', keyPair.publicKey);
    component.publicKeyPem = `-----BEGIN PUBLIC KEY-----\n${btoa(String.fromCharCode(...new Uint8Array(spki)))}\n-----END PUBLIC KEY-----`;

    await component.createPackage();

    expect(component.errorKey()).toBe('parkEditorAccess.sessionExpired');
  });
});
