import { Directive, Injectable, Input, TemplateRef } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface ToastMessage {
  severity: 'success' | 'info' | 'warn' | 'error';
  summary: string;
  detail: string;
  id?: number;
}

@Injectable({
  providedIn: 'root'
})
export class MessageService {
  private readonly messagesSubject: BehaviorSubject<ToastMessage[]> = new BehaviorSubject<ToastMessage[]>([]);
  readonly messages$ = this.messagesSubject.asObservable();
  private nextId: number = 1;

  add(message: ToastMessage): void {
    const messageWithId: ToastMessage = {
      ...message,
      id: this.nextId
    };
    this.nextId += 1;
    this.messagesSubject.next([...this.messagesSubject.value, messageWithId]);
  }

  remove(id: number): void {
    this.messagesSubject.next(this.messagesSubject.value.filter((message: ToastMessage) => message.id !== id));
  }
}

@Directive({
  selector: '[appUiTemplate]',
  standalone: true
})
export class UiTemplate {
  @Input('appUiTemplate') name: string = '';

  constructor(public readonly template: TemplateRef<unknown>) {
  }
}
