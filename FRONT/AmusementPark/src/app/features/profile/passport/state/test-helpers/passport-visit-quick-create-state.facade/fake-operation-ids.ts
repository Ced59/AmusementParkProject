import { PassportVisitOperationIdPort } from '../../passport-visit-quick-create-state-data.ports';

export class FakeOperationIds implements PassportVisitOperationIdPort {
  private count: number = 0;

  create(): string {
    this.count += 1;
    return `operation-${this.count}`;
  }
}
