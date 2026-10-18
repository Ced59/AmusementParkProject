// Finish the current local document's fonts and intercepted responses before
// replacing it. No protocol error is ignored and no request is forwarded.
export async function settleAccountDocument({ evaluate, pending }) {
  const fontStatus = await evaluate('document.fonts.ready.then(() => document.fonts.status)');
  if (fontStatus !== 'loaded') throw new Error('The isolated document fonts did not settle before capture or navigation.');
  for (let pass = 0; pending.size && pass < 16; pass++) {
    const settled = await Promise.allSettled([...pending]);
    if (settled.some(item => item.status === 'rejected')) throw new Error('An intercepted response failed while settling the local document.');
  }
  if (pending.size) throw new Error('The isolated document still has intercepted responses after its bounded drain.');
}
