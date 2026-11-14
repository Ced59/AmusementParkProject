import { webcrypto } from 'node:crypto';

import { encryptParkEditorToken, importParkEditorRecipientKey } from './park-data-editor-token-crypto';

describe('park data editor token handoff encryption', () => {
  const subtle: SubtleCrypto = webcrypto.subtle as unknown as SubtleCrypto;

  it('encrypts the one-time token for the matching private key', async () => {
    const keyPair: CryptoKeyPair = await subtle.generateKey(
      { name: 'RSA-OAEP', modulusLength: 2048, publicExponent: new Uint8Array([1, 0, 1]), hash: 'SHA-256' },
      true,
      ['encrypt', 'decrypt']) as CryptoKeyPair;
    const spki: ArrayBuffer = await subtle.exportKey('spki', keyPair.publicKey);
    const pem: string = `-----BEGIN PUBLIC KEY-----\n${btoa(String.fromCharCode(...new Uint8Array(spki)))}\n-----END PUBLIC KEY-----`;
    const recipient: CryptoKey = await importParkEditorRecipientKey(pem, subtle);
    const token = 'apf_pde_0123456789abcdef0123456789abcdef.' + 'a'.repeat(43);

    const packageText: string = await encryptParkEditorToken(token, recipient, subtle);
    expect(packageText.startsWith('APF-PDE1.')).toBe(true);
    expect(packageText).not.toContain(token);

    const encoded: string = packageText.slice('APF-PDE1.'.length).replace(/-/g, '+').replace(/_/g, '/');
    const binary: string = atob(encoded);
    const ciphertext: Uint8Array<ArrayBuffer> = new Uint8Array(new ArrayBuffer(binary.length));
    for (let index = 0; index < binary.length; index += 1) {
      ciphertext[index] = binary.charCodeAt(index);
    }
    const decrypted: ArrayBuffer = await subtle.decrypt({ name: 'RSA-OAEP' }, keyPair.privateKey, ciphertext);
    expect(new TextDecoder().decode(decrypted)).toBe(token);
  });

  it('rejects malformed keys before creating a server token', async () => {
    await expect(importParkEditorRecipientKey('not a public key', subtle)).rejects.toThrow('Invalid recipient key');
  });
});
