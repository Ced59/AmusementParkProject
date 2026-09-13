import { SafeHtml } from '@angular/platform-browser';

export class DomSanitizerStub {
  bypassSecurityTrustHtml(value: string): SafeHtml {
    return value as unknown as SafeHtml;
  }
}
