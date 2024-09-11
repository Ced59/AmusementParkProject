import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ModalService {
  private modals: any = {
    loginModal: new BehaviorSubject<boolean>(false),
    languageModal: new BehaviorSubject<boolean>(false)
  };

  constructor() {}

  openModal(modalName: string) {
    const modal = this.modals[modalName];
    if (modal) {
      modal.next(true);
    } else {
      console.error(`No modal found with the name '${modalName}'`);
    }
  }

  closeModal(modalName: string) {
    const modal = this.modals[modalName];
    if (modal) {
      modal.next(false);
    } else {
      console.error(`No modal found with the name '${modalName}'`);
    }
  }

  getModalStatus(modalName: string) {
    const modal = this.modals[modalName];
    if (modal) {
      return modal.asObservable();
    } else {
      console.error(`No modal found with the name '${modalName}'`);
      return null;
    }
  }
}
