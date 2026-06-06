import { SsrResponseLike } from './ssr-response.token';
import { SsrHttpStatusService } from './ssr-http-status.service';

describe('SsrHttpStatusService', () => {
  it('does nothing when no SSR response is available', () => {
    const service: SsrHttpStatusService = new SsrHttpStatusService(null);

    expect(() => service.setStatus(404)).not.toThrow();
  });

  it('sets an arbitrary status on the SSR response', () => {
    const response: jasmine.SpyObj<SsrResponseLike> = jasmine.createSpyObj<SsrResponseLike>('SsrResponseLike', ['status']);
    const service: SsrHttpStatusService = new SsrHttpStatusService(response);

    service.setStatus(503);

    expect(response.status).toHaveBeenCalledWith(503);
  });

  it('sets the not found status through the dedicated helper', () => {
    const response: jasmine.SpyObj<SsrResponseLike> = jasmine.createSpyObj<SsrResponseLike>('SsrResponseLike', ['status']);
    const service: SsrHttpStatusService = new SsrHttpStatusService(response);

    service.setNotFound();

    expect(response.status).toHaveBeenCalledWith(404);
  });
});
