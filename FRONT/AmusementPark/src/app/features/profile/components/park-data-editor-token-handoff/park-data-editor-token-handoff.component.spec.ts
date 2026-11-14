import { webcrypto } from 'node:crypto';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ParkDataEditorTokenApiService } from '@data-access/users/park-data-editor-token-api.service';
import { ParkDataEditorTokenHandoffComponent } from './park-data-editor-token-handoff.component';

describe('ParkDataEditorTokenHandoffComponent', () => {
  const rawToken = 'apf_pde_0123456789abcdef0123456789abcdef.' + 'a'.repeat(43);
  const api = {
    createToken: vi.fn(() => of({
      token: { id: 'token-1', expiresAtUtc: '2026-12-14T00:00:00Z' },
      plainTextToken: rawToken
    })),
    revoke: vi.fn(() => of(undefined))
  };

  beforeEach(async () => {
    vi.stubGlobal('crypto', webcrypto);
    api.createToken.mockClear();
    api.revoke.mockClear();
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
});
