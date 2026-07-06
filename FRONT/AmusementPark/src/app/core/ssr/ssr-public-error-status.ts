import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from './ssr-http-status.service';

export function applySsrPublicDataErrorStatus(error: unknown, ssrHttpStatusService: SsrHttpStatusService): void {
  if (hasHttpStatus(error, 404)) {
    ssrHttpStatusService.setNotFound();
    return;
  }

  ssrHttpStatusService.setStatus(503);
}
