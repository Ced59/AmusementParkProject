import type { MockedObject } from 'vitest';
import { HttpErrorResponse } from '@angular/common/http';

import { SsrHttpStatusService } from './ssr-http-status.service';
import { applySsrPublicDataErrorStatus } from './ssr-public-error-status';

describe('applySsrPublicDataErrorStatus', () => {
  let ssrHttpStatusService: MockedObject<SsrHttpStatusService>;

  beforeEach(() => {
    ssrHttpStatusService = {
      setNotFound: vi.fn().mockName('SsrHttpStatusService.setNotFound'),
      setStatus: vi.fn().mockName('SsrHttpStatusService.setStatus'),
    } as unknown as MockedObject<SsrHttpStatusService>;
  });

  it('keeps 404 errors as not found during SSR', () => {
    applySsrPublicDataErrorStatus(
      new HttpErrorResponse({ status: 404 }),
      ssrHttpStatusService,
    );

    expect(ssrHttpStatusService.setNotFound).toHaveBeenCalled();
    expect(ssrHttpStatusService.setStatus).not.toHaveBeenCalled();
  });

  it('marks transient public data errors as service unavailable during SSR', () => {
    applySsrPublicDataErrorStatus(
      new HttpErrorResponse({ status: 503 }),
      ssrHttpStatusService,
    );

    expect(ssrHttpStatusService.setNotFound).not.toHaveBeenCalled();
    expect(ssrHttpStatusService.setStatus).toHaveBeenCalledTimes(1);
    expect(ssrHttpStatusService.setStatus).toHaveBeenCalledWith(503);
  });
});
