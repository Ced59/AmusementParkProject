// A router cancellation invalidates a paused CDP request. Never ignore this
// protocol error without the matching Network.loadingFailed cancellation.
export function isCanceledInterceptionError(error, networkId, canceledNetworkRequests) {
  return Boolean(networkId && canceledNetworkRequests.has(networkId)
    && error.message === 'Invalid InterceptionId.');
}
