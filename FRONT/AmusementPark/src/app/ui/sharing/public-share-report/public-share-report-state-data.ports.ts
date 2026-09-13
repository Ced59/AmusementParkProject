import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { SubmitShareModerationReportRequest } from '@app/models/sharing/share-moderation.models';

export interface PublicShareReportPort {
  submit(request: SubmitShareModerationReportRequest): Observable<void>;
}

export const PUBLIC_SHARE_REPORT_PORT = new InjectionToken<PublicShareReportPort>(
  'PUBLIC_SHARE_REPORT_PORT',
);
