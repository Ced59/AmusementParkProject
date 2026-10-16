// A router cancellation invalidates a paused CDP request. Never ignore this
// protocol error without the matching Network.loadingFailed cancellation.
export function isCanceledInterceptionError(error, networkId, canceledNetworkRequests) {
  return Boolean(networkId && canceledNetworkRequests.has(networkId)
    && error.message === 'Invalid InterceptionId.');
}

// A committed native main-document replacement also invalidates requests from
// its exact previous frame/loader. Same-document routing is not this proof.
export function isRetiredDocumentInterceptionError(error, networkId, retiredNetworkRequests) {
  return Boolean(networkId && retiredNetworkRequests.has(networkId)
    && error.message === 'Invalid InterceptionId.');
}

export function advanceMainDocument(current, frame, requestDocuments, retiredNetworkRequests) {
  if (frame.parentId || !frame.id || !frame.loaderId) return current;
  const document = JSON.stringify([frame.id, frame.loaderId]);
  if (current && document !== current) {
    for (const [id, owner] of requestDocuments) {
      if (JSON.stringify([owner.frameId, owner.loaderId]) === current) retiredNetworkRequests.add(id);
    }
  }
  // Retire exact outstanding IDs, not every future request of a loader which
  // may become active again through the back/forward cache.
  return document;
}
