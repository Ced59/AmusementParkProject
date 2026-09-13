import { SafeHtml } from '@angular/platform-browser';

export class DomSanitizerStub {
  public bypassSecurityTrustHtml(value: string): SafeHtml {
    return `SAFE:${value}` as unknown as SafeHtml;
  }
}
