import { Pipe, PipeTransform } from '@angular/core';

import { UrlSecurityService } from '@shared/utils/security/url-security.service';

@Pipe({
  name: 'safeExternalUrl',
  standalone: true
})
export class SafeExternalUrlPipe implements PipeTransform {
  constructor(private readonly urlSecurityService: UrlSecurityService) {
  }

  transform(value: string | null | undefined): string | null {
    return this.urlSecurityService.sanitizeExternalUrl(value);
  }
}
