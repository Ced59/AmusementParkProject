export class FakeToastMessageService {
  readonly messages: Array<{
    severity: string;
    summary: string;
    detail: string;
  }> = [];

  add(
    severity: 'success' | 'info' | 'warn' | 'error',
    summary: string,
    detail: string,
  ): void {
    this.messages.push({ severity, summary, detail });
  }
}
