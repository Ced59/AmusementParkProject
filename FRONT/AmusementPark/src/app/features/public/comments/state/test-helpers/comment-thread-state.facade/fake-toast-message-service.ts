export class FakeToastMessageService {
  readonly messages: string[] = [];

  add(
    _severity: 'success' | 'info' | 'warn' | 'error',
    _summary: string,
    detail: string
  ): void {
    this.messages.push(detail);
  }
}
