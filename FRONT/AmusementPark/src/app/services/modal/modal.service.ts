import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

const MODAL_NAMES = ['loginModal', 'languageModal'] as const;
export type ModalName = typeof MODAL_NAMES[number];

type ModalStateRegistry = Record<ModalName, BehaviorSubject<boolean>>;

@Injectable({
  providedIn: 'root'
})
export class ModalService {
  private readonly modals: ModalStateRegistry = {
    loginModal: new BehaviorSubject<boolean>(false),
    languageModal: new BehaviorSubject<boolean>(false)
  };

  openModal(modalName: ModalName): void {
    this.modals[modalName].next(true);
  }

  closeModal(modalName: ModalName): void {
    this.modals[modalName].next(false);
  }

  getModalStatus(modalName: ModalName): Observable<boolean> {
    return this.modals[modalName].asObservable();
  }
}
