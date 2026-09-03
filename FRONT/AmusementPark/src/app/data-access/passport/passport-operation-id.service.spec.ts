import { PassportOperationIdService } from './passport-operation-id.service';

describe('PassportOperationIdService', () => {
  it('creates distinct printable operation identifiers accepted by the API contract', () => {
    const service: PassportOperationIdService = new PassportOperationIdService();

    const first: string = service.create();
    const second: string = service.create();

    expect(first).not.toBe(second);
    expect(first.length).toBeGreaterThan(0);
    expect(first.length).toBeLessThanOrEqual(128);
    expect(first).toMatch(/^[\x20-\x7e]+$/);
  });
});
