// CI-only private presentation input, never imported by the application.
export function createAccountTripFixture(kind, date, user, trips) {
  const editable = kind === 'standard';
  const plan = { ...(trips[1] ?? { tripPlanId: '00000000-0000-4000-8000-000000000061',
    title: 'Voyage de recette uniquement — titre long Äéł', dateProposal: { kind: 'Fixed', startDate: '2026-12-20', endDate: '2026-12-20', candidateDates: [] },
    destinationTimeZoneId: 'Europe/Paris', status: 'Draft', accessScope: 'Private', memberCount: 2, createdAtUtc: date, updatedAtUtc: date, version: 1 }),
    isOwner: editable, effectiveRole: editable ? 'Owner' : 'Viewer', canEditPlan: editable, canEditProgram: editable,
    canInvite: editable, canChangeRoles: editable };
  const parkId = '00000000-0000-4000-8000-000000000050';
  const candidate = { candidateId: '00000000-0000-4000-8000-000000000560', parkId,
    parkName: 'Parc de recette uniquement — nom très long Äéł', isParkAvailable: true, candidateDates: ['2026-12-20'],
    source: 'Manual', state: 'Selected', collectiveNote: 'Note collective de recette uniquement', fitSnapshot: null,
    sortPosition: 0, version: 1, createdAtUtc: date, updatedAtUtc: date };
  const day = { dayPlanId: '00000000-0000-4000-8000-000000000570', localDate: '2026-12-20', parkCandidateId: candidate.candidateId,
    parkId, parkName: candidate.parkName, isParkAvailable: true, desiredArrivalTime: '10:00', groupNote: 'Note de recette uniquement',
    blocks: [{ blockId: '00000000-0000-4000-8000-000000000580', type: 'Note', title: 'Repère de recette uniquement — texte long Äéł', details: null, localTime: null, sortPosition: 0 }],
    version: 1, createdAtUtc: date, updatedAtUtc: date };
  const program = { candidates: editable ? [candidate] : [], days: editable ? [day] : [] };
  const preferenceItems = editable ? ['MustDo', 'WantToDo', 'Unknown'].map((level, index) => ({
    parkId, parkName: candidate.parkName, parkItemId: `00000000-0000-4000-8000-${String(600 + index).padStart(12, '0')}`,
    parkItemName: `Lieu de recette uniquement — nom long Äéł (${index})`, mainImageId: null, level, reason: index === 0 ? 'Sensations' : null, version: 1 })) : [];
  const preferences = { tripPlanId: plan.tripPlanId, tripTitle: plan.title, planVersion: 1, canVote: editable, items: preferenceItems };
  const summary = { tripPlanId: plan.tripPlanId, tripTitle: plan.title, planVersion: 1, participantCount: 2, canDecide: editable,
    items: preferenceItems.map((item, index) => ({ ...item, mustDoCount: index === 0 ? 1 : 0, wantToDoCount: index === 1 ? 1 : 0,
      optionalCount: 0, notForMeCount: index === 0 ? 1 : 0, unansweredCount: index === 2 ? 2 : 0,
      compatibility: ['Conflict', 'Consensus', 'Unknown'][index], isCompatibilityKnown: index !== 2,
      hasIndividualConstraint: index === 0, isGroupPriority: index === 1,
      officialStatus: null, officialSourceUrl: null, officialStatusVerifiedAtUtc: null,
      decision: index === 1 ? { status: 'Retained', reason: 'Décision de recette uniquement', decidedByDisplayName: 'Membre de recette', decidedAtUtc: date, version: 1 } : null })) };
  const issues = editable ? ['OpeningHoursUnknown', 'NewMemberConstraint', 'DateOutsideProposal'].map((code, index) => ({
    code, severity: ['Information', 'Attention', 'Critical'][index], localDate: day.localDate, parkId, parkName: candidate.parkName,
    parkItemId: null, parkItemName: null, officialStatus: null, officialSourceUrl: null, officialVerifiedAtUtc: null })) : [];
  const coherence = { tripPlanId: plan.tripPlanId, tripTitle: plan.title, planVersion: 1, evaluatedAtUtc: date,
    criticalCount: editable ? 1 : 0, attentionCount: editable ? 1 : 0, informationCount: editable ? 1 : 0,
    days: editable ? [{ localDate: day.localDate, parkId, parkName: candidate.parkName, isParkAvailable: true,
      parkStatus: null, openingState: 'Unknown', openingHoursSourceUrl: null, openingHoursVerifiedAtUtc: null, dayPlanUpdatedAtUtc: date }] : [],
    travelSegments: [], issues };
  const activity = { tripTitle: plan.title, entries: editable ? [{ sequence: 3, kind: 'DatesChanged', actorDisplayName: 'Membre de recette',
    isCurrentUser: false, affectedCount: 1, occurredAtUtc: date }] : [], nextBeforeSequence: editable ? 3 : null };
  const olderActivity = { tripTitle: plan.title, entries: [...activity.entries, { sequence: 2, kind: 'TripCreated',
    actorDisplayName: 'Compte de recette', isCurrentUser: true, affectedCount: 1, occurredAtUtc: date }], nextBeforeSequence: null };
  const participants = { participants: [{ memberId: user.id, displayName: 'Compte de recette uniquement — nom long Äéł',
    role: plan.effectiveRole, isCurrentUser: true, joinedAtUtc: date }, { memberId: '00000000-0000-4000-8000-000000000018',
    displayName: 'Autre membre de recette', role: editable ? 'Participant' : 'Owner', isCurrentUser: false, joinedAtUtc: date }],
    canManageRoles: editable, canTransferOwnership: editable, canLeave: !editable, tripVersion: 1 };
  const invitations = { inviterDisplayName: 'Compte de recette', invitations: [] };
  const notifications = { enabled: false, version: 1, unreadCount: 0, hasMoreUnread: false };
  const exported = { schemaVersion: 'ci-ui-only', title: plan.title, dateProposal: structuredClone(plan.dateProposal),
    destinationTimeZoneId: plan.destinationTimeZoneId, status: plan.status, generatedAtUtc: date,
    candidateParks: program.candidates.map(item => ({ parkName: item.parkName, isParkAvailable: item.isParkAvailable,
      candidateDates: item.candidateDates, state: item.state, collectiveNote: item.collectiveNote })),
    days: program.days.map(item => ({ localDate: item.localDate, parkName: item.parkName, isParkAvailable: item.isParkAvailable,
      desiredArrivalTime: item.desiredArrivalTime, groupNote: item.groupNote, blocks: item.blocks })), collectiveDecisions: [] };
  const transition = { title: plan.title, destinationToday: '2026-12-21', days: editable ? [{ localDate: day.localDate, parkId,
    parkName: candidate.parkName, isParkAvailable: true, canConfirm: true, canResume: false, isSelectionLocked: false,
    existingVisitId: null, existingVisitStatus: null, attractions: preferenceItems.map((item, index) => ({ parkItemId: item.parkItemId,
      name: item.parkItemName, mainImageId: null, ownPreference: item.level, historicalConsistency: index ? 'Unverified' : 'Verified', isPreselected: index === 0 })) }] : [] };
  return { plan, program, preferences, summary, coherence, activity, olderActivity, participants, invitations, notifications, exported, transition };
}

export function accountTripFixtureResponse(request, url, fixture) {
  if (fixture.kind === 'expired' || request.method !== 'GET') return null;
  const data = fixture.tripData;
  const prefix = `/api/me/trips/${data.plan.tripPlanId}`;
  if (url.pathname === '/api/me/collections' && url.search === '?targetType=Park') return { status: 200, body: structuredClone(fixture.collections.filter(item => item.targetType === 'Park')) };
  if (url.pathname === `${prefix}/activity` && url.search === '?beforeSequence=3' && data.activity.nextBeforeSequence === 3) return { status: 200, body: structuredClone(data.olderActivity) };
  if (url.search) return null;
  const pages = new Map([[prefix, data.plan], [`${prefix}/program`, data.program], [`${prefix}/preferences`, data.preferences],
    [`${prefix}/preference-summary`, data.summary], [`${prefix}/coherence`, data.coherence], [`${prefix}/activity`, data.activity],
    [`${prefix}/participants`, data.participants], [`${prefix}/invitations`, data.invitations], [`${prefix}/notifications`, data.notifications],
    [`${prefix}/export`, data.exported], [`${prefix}/passport-transition`, data.transition]]);
  return pages.has(url.pathname) ? { status: 200, body: structuredClone(pages.get(url.pathname)) } : null;
}
