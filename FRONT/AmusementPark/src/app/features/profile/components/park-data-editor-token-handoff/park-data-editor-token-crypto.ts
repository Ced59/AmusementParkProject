const publicKeyHeader = '-----BEGIN PUBLIC KEY-----';
const publicKeyFooter = '-----END PUBLIC KEY-----';

export async function importParkEditorRecipientKey(pem: string, subtle: SubtleCrypto): Promise<CryptoKey> {
  const trimmed: string = pem.trim();
  if (trimmed.length > 4096 || !trimmed.startsWith(publicKeyHeader) || !trimmed.endsWith(publicKeyFooter)) {
    throw new Error('Invalid recipient key');
  }

  const body: string = trimmed.slice(publicKeyHeader.length, -publicKeyFooter.length).replace(/\s/g, '');
  if (!body || !/^[A-Za-z0-9+/]+={0,2}$/.test(body)) {
    throw new Error('Invalid recipient key');
  }

  const binary: string = atob(body);
  const der: Uint8Array<ArrayBuffer> = new Uint8Array(new ArrayBuffer(binary.length));
  for (let index = 0; index < binary.length; index += 1) {
    der[index] = binary.charCodeAt(index);
  }
  const key: CryptoKey = await subtle.importKey(
    'spki', der, { name: 'RSA-OAEP', hash: 'SHA-256' }, false, ['encrypt']);
  const algorithm: RsaHashedKeyAlgorithm = key.algorithm as RsaHashedKeyAlgorithm;
  const exponent: Uint8Array = algorithm.publicExponent;
  if (algorithm.modulusLength < 2048
    || exponent.length !== 3
    || exponent[0] !== 1
    || exponent[1] !== 0
    || exponent[2] !== 1) {
    throw new Error('Invalid recipient key');
  }

  return key;
}

export async function encryptParkEditorToken(
  token: string,
  recipientKey: CryptoKey,
  subtle: SubtleCrypto
): Promise<string> {
  const encoded: Uint8Array = new TextEncoder().encode(token);
  const plaintext: Uint8Array<ArrayBuffer> = new Uint8Array(new ArrayBuffer(encoded.length));
  plaintext.set(encoded);
  encoded.fill(0);
  try {
    const ciphertext: ArrayBuffer = await subtle.encrypt({ name: 'RSA-OAEP' }, recipientKey, plaintext);
    const base64: string = btoa(String.fromCharCode(...new Uint8Array(ciphertext)));
    return `APF-PDE1.${base64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '')}`;
  } finally {
    plaintext.fill(0);
  }
}
