export class FakeModalService {
  readonly openedModals: string[] = [];

  openModal(id: string): void {
    this.openedModals.push(id);
  }
}
