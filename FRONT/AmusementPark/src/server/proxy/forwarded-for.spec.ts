import { appendForwardedFor } from './forwarded-for';

describe('forwarded-for proxy chain', () => {
  it('preserves the complete incoming chain before appending the immediate proxy', () => {
    const result = appendForwardedFor(
      '192.0.2.123, 203.0.113.42',
      '::ffff:172.19.0.6',
    );

    expect(result).toBe(
      '192.0.2.123, 203.0.113.42, ::ffff:172.19.0.6',
    );
  });

  it('uses the immediate address when no forwarded chain exists', () => {
    expect(appendForwardedFor(undefined, '::ffff:172.19.0.6')).toBe(
      '::ffff:172.19.0.6',
    );
  });

  it('keeps the existing chain when the immediate address is unavailable', () => {
    expect(appendForwardedFor('203.0.113.42', undefined)).toBe('203.0.113.42');
  });
});
