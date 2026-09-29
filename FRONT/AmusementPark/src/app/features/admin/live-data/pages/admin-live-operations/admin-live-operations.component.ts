import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import {
  LiveOperationalScope,
  UpdateLiveOperationalControlRequest
} from '@app/models/admin/live-data/live-operations.models';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { UiTemplate } from '@shared/ui/primitives/api';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { Card } from '@shared/ui/primitives/card';
import { Tag } from '@shared/ui/primitives/tag';
import { AdminLiveOperationsFacade } from '../../state/admin-live-operations.facade';

interface PendingLiveOperationalChange {
  readonly scope: LiveOperationalScope;
  readonly collectionEnabled: boolean;
  readonly publicReadEnabled: boolean;
  readonly actionKey: string;
}

@Component({
  selector: 'app-admin-live-operations',
  templateUrl: './admin-live-operations.component.html',
  styleUrl: './admin-live-operations.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminLiveOperationsFacade],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    TranslateModule,
    PageStateComponent,
    UiTemplate,
    ButtonDirective,
    Card,
    Tag
  ]
})
export class AdminLiveOperationsComponent implements OnInit {
  protected readonly state = this.facade.state;
  protected readonly dashboard = this.facade.dashboard;
  protected readonly pendingScopeKey = this.facade.pendingScopeKey;
  protected readonly replayPending = this.facade.replayPending;
  protected readonly feedbackKey = this.facade.feedbackKey;
  protected readonly mutationErrorKey = this.facade.mutationErrorKey;
  protected readonly pendingChange = signal<PendingLiveOperationalChange | null>(null);
  protected readonly reason = new FormControl<string>('', { nonNullable: true });

  constructor(protected readonly facade: AdminLiveOperationsFacade) {
  }

  ngOnInit(): void {
    this.facade.load();
  }

  protected openCollectionChange(scope: LiveOperationalScope): void {
    const enabled: boolean = !scope.collectionEnabled;
    this.openChange(
      scope,
      enabled,
      scope.publicReadEnabled,
      enabled ? 'resumeCollection' : 'stopCollection'
    );
  }

  protected openPublicReadChange(scope: LiveOperationalScope): void {
    const enabled: boolean = !scope.publicReadEnabled;
    this.openChange(
      scope,
      scope.collectionEnabled,
      enabled,
      enabled ? 'showPublicly' : 'hidePublicly'
    );
  }

  protected confirm(): void {
    const change: PendingLiveOperationalChange | null = this.pendingChange();
    const reason: string = this.reason.value.trim();
    if (change === null || reason.length < 3) {
      return;
    }

    const scope: LiveOperationalScope = change.scope;
    const request: UpdateLiveOperationalControlRequest = {
      scopeType: scope.scopeType,
      sourceId: scope.sourceId,
      externalEntityId: scope.externalEntityId,
      internalParkId: scope.internalParkId,
      targetType: scope.targetType,
      internalTargetId: scope.internalTargetId,
      collectionEnabled: change.collectionEnabled,
      publicReadEnabled: change.publicReadEnabled,
      expectedRevision: scope.revision,
      reason
    };
    this.facade.update(this.scopeKey(scope), request);
    this.cancel();
  }

  protected cancel(): void {
    this.pendingChange.set(null);
    this.reason.setValue('');
  }

  protected scopeKey(scope: LiveOperationalScope): string {
    return [
      scope.scopeType,
      scope.sourceId,
      scope.externalEntityId ?? '',
      scope.internalParkId ?? '',
      scope.targetType ?? '',
      scope.internalTargetId ?? ''
    ].join(':');
  }

  protected scopeTypeKey(scope: LiveOperationalScope): string {
    return `admin.liveOperations.scopeType.${scope.scopeType}`;
  }

  protected effectiveStateKey(enabled: boolean): string {
    return enabled
      ? 'admin.liveOperations.state.running'
      : 'admin.liveOperations.state.stopped';
  }

  protected stateSeverity(enabled: boolean): 'success' | 'danger' {
    return enabled ? 'success' : 'danger';
  }

  private openChange(
    scope: LiveOperationalScope,
    collectionEnabled: boolean,
    publicReadEnabled: boolean,
    actionKey: string
  ): void {
    this.reason.setValue('');
    this.pendingChange.set({ scope, collectionEnabled, publicReadEnabled, actionKey });
    this.facade.clearFeedback();
  }
}
