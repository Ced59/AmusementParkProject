import { PublicResponseHeaderWriter } from '../../public-response-transport-policy';

export class TestPublicResponseHeaderWriter implements PublicResponseHeaderWriter {
  readonly headers: Map<string, string> = new Map<string, string>();

  setHeader(name: string, value: string): void {
    this.headers.set(name, value);
  }
}
