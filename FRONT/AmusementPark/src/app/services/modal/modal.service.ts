import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ModalService {
  private displayLoginModalSource = new BehaviorSubject<boolean>(false);
  displayLoginModal$ = this.displayLoginModalSource.asObservable();

  constructor() {}

  openLoginModal() {
    this.displayLoginModalSource.next(true);
  }

  closeLoginModal() {
    this.displayLoginModalSource.next(false);
  }
}
