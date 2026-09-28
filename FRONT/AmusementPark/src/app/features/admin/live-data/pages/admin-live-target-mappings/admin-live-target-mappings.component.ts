import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import {
  CreateLiveTargetMappingCandidateRequest,
  LiveMappingStatus,
  LiveTargetMapping,
  LiveTargetMappingDecision,
  LiveTargetMappingQuery,
  LiveTargetType,
  ReviewLiveTargetMappingRequest
} from '@app/models/admin/live-data/live-target-mapping.models';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { UiTemplate } from '@shared/ui/primitives/api';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { Card } from '@shared/ui/primitives/card';
import { AdminLiveTargetMappingsFacade } from '../../state/admin-live-target-mappings.facade';

@Component({
  selector: 'app-admin-live-target-mappings',
  templateUrl: './admin-live-target-mappings.component.html',
  styleUrl: './admin-live-target-mappings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminLiveTargetMappingsFacade],
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    PageStateComponent,
    UiTemplate,
    ButtonDirective,
    Card
  ]
})
export class AdminLiveTargetMappingsComponent implements OnInit {
  protected readonly state = this.facade.state;
  protected readonly mappings = this.facade.mappings;
  protected readonly pagination = this.facade.pagination;
  protected readonly mutationPending = this.facade.mutationPending;
  protected readonly feedbackKey = this.facade.feedbackKey;
  protected readonly mutationErrorKey = this.facade.mutationErrorKey;
  protected readonly statuses: readonly LiveMappingStatus[] = [
    'Candidate',
    'Verified',
    'Suspended',
    'Superseded',
    'Rejected'
  ];
  protected readonly targetTypes: readonly LiveTargetType[] = ['Park', 'ParkItem'];

  protected search: string = '';
  protected status: LiveMappingStatus | '' = '';
  protected targetType: LiveTargetType | '' = '';
  protected page: number = 1;
  protected sourceId: string = 'themeparks-wiki';
  protected externalTargetId: string = '';
  protected externalParentTargetId: string = '';
  protected externalDisplayName: string = '';
  protected externalParentDisplayName: string = '';
  protected externalCountryCode: string = '';
  protected candidateTargetType: LiveTargetType = 'ParkItem';
  protected suggestedInternalTargetId: string = '';
  protected suggestedParkId: string = '';

  private readonly targetIds: Map<string, string> = new Map<string, string>();
  private readonly parkIds: Map<string, string> = new Map<string, string>();
  private readonly reviewNotes: Map<string, string> = new Map<string, string>();

  constructor(protected readonly facade: AdminLiveTargetMappingsFacade) {
  }

  ngOnInit(): void {
    this.reload();
  }

  protected reload(page: number = this.page): void {
    this.page = Math.max(1, page);
    this.facade.load(this.currentQuery());
  }

  protected applyFilters(): void {
    this.facade.clearFeedback();
    this.reload(1);
  }

  protected clearFilters(): void {
    this.search = '';
    this.status = '';
    this.targetType = '';
    this.applyFilters();
  }

  protected createCandidate(): void {
    const suggestionIsComplete: boolean = Boolean(
      this.suggestedInternalTargetId.trim() && this.suggestedParkId.trim()
    );
    const request: CreateLiveTargetMappingCandidateRequest = {
      sourceId: this.sourceId.trim(),
      targetType: this.candidateTargetType,
      externalTargetId: this.externalTargetId.trim(),
      externalParentTargetId: this.optional(this.externalParentTargetId),
      externalDisplayName: this.externalDisplayName.trim(),
      externalParentDisplayName: this.optional(this.externalParentDisplayName),
      externalCountryCode: this.externalCountryCode.trim().toUpperCase(),
      suggestedInternalTargetId: suggestionIsComplete
        ? this.suggestedInternalTargetId.trim()
        : null,
      suggestedParkId: suggestionIsComplete ? this.suggestedParkId.trim() : null
    };
    this.facade.createCandidate(request, this.currentQuery());
  }

  protected canCreateCandidate(): boolean {
    const requiredPresent: boolean = Boolean(
      this.sourceId.trim()
      && this.externalTargetId.trim()
      && this.externalDisplayName.trim()
      && this.externalCountryCode.trim().length === 2
    );
    const parentIsValid: boolean = this.candidateTargetType === 'Park'
      ? !this.externalParentTargetId.trim() && !this.externalParentDisplayName.trim()
      : Boolean(this.externalParentTargetId.trim() && this.externalParentDisplayName.trim());
    const suggestionIsValid: boolean = Boolean(this.suggestedInternalTargetId.trim())
      === Boolean(this.suggestedParkId.trim());
    return requiredPresent && parentIsValid && suggestionIsValid;
  }

  protected targetIdFor(mapping: LiveTargetMapping): string {
    return this.targetIds.get(mapping.mappingId) ?? mapping.target?.id ?? '';
  }

  protected parkIdFor(mapping: LiveTargetMapping): string {
    return this.parkIds.get(mapping.mappingId) ?? mapping.target?.parkId ?? '';
  }

  protected noteFor(mapping: LiveTargetMapping): string {
    return this.reviewNotes.get(mapping.mappingId) ?? '';
  }

  protected setTargetId(mappingId: string, value: string): void {
    this.targetIds.set(mappingId, value);
  }

  protected setParkId(mappingId: string, value: string): void {
    this.parkIds.set(mappingId, value);
  }

  protected setReviewNote(mappingId: string, value: string): void {
    this.reviewNotes.set(mappingId, value);
  }

  protected canReview(
    mapping: LiveTargetMapping,
    decision: LiveTargetMappingDecision
  ): boolean {
    if (decision === 'Verify' || decision === 'Correct') {
      if (!this.targetIdFor(mapping).trim() || !this.parkIdFor(mapping).trim()) {
        return false;
      }
    }

    return decision === 'Verify' || Boolean(this.noteFor(mapping).trim());
  }

  protected review(
    mapping: LiveTargetMapping,
    decision: LiveTargetMappingDecision
  ): void {
    const requiresTarget: boolean = decision === 'Verify' || decision === 'Correct';
    const request: ReviewLiveTargetMappingRequest = {
      expectedRevision: mapping.revision,
      decision,
      internalTargetId: requiresTarget ? this.targetIdFor(mapping).trim() : null,
      parkId: requiresTarget ? this.parkIdFor(mapping).trim() : null,
      reviewNote: this.optional(this.noteFor(mapping))
    };
    this.facade.review(mapping.mappingId, request, this.currentQuery());
  }

  protected canGoPrevious(): boolean {
    return this.page > 1;
  }

  protected canGoNext(): boolean {
    const totalPages: number = this.pagination()?.totalPages ?? this.page;
    return this.page < totalPages;
  }

  protected statusKey(status: LiveMappingStatus): string {
    return `admin.liveMappings.status.${status}`;
  }

  protected typeKey(type: LiveTargetType): string {
    return `admin.liveMappings.type.${type}`;
  }

  protected statusClass(status: LiveMappingStatus): string {
    return `live-mappings__status live-mappings__status--${status.toLowerCase()}`;
  }

  private currentQuery(): LiveTargetMappingQuery {
    return {
      page: this.page,
      pageSize: 25,
      sourceId: null,
      status: this.status || null,
      targetType: this.targetType || null,
      search: this.optional(this.search)
    };
  }

  private optional(value: string): string | null {
    const normalized: string = value.trim();
    return normalized || null;
  }
}
