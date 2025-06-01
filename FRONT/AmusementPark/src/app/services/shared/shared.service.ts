import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SharedService {
  private loginStatusSubject = new Subject<void>();

  constructor() {}

  emitLoginStatusChange() {
    this.loginStatusSubject.next();
  }

  getLoginStatusListener() {
    return this.loginStatusSubject.asObservable();
  }
}
