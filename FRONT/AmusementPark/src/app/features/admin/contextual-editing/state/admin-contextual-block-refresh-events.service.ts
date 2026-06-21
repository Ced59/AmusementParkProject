import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';

import { AdminContextualBlockEntityType, AdminContextualBlockType } from '../models/admin-contextual-block.model';

export interface AdminContextualBlockAppliedEvent {
  readonly blockType: AdminContextualBlockType;
  readonly entityType: AdminContextualBlockEntityType;
  readonly entityId: string;
  readonly appliedAtUtc: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminContextualBlockRefreshEvents {
  private readonly appliedBlockSubject = new Subject<AdminContextualBlockAppliedEvent>();

  public readonly appliedBlock$: Observable<AdminContextualBlockAppliedEvent> = this.appliedBlockSubject.asObservable();

  notifyBlockApplied(event: AdminContextualBlockAppliedEvent): void {
    this.appliedBlockSubject.next(event);
  }
}
