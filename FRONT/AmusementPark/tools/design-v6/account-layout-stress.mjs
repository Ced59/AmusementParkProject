// A CI-only enlarged-font stress case, not a claim of native browser zoom QA.
export async function withDoubledRootFont({ evaluate, capture }) {
  const initial = await evaluate('({ inline: document.documentElement.style.fontSize, pixels: parseFloat(getComputedStyle(document.documentElement).fontSize) })');
  if (!Number.isFinite(initial.pixels) || initial.pixels < 12 || initial.pixels > 24) throw new Error('Unexpected root font before the bounded enlarged-font case.');
  const pixels = initial.pixels * 2;
  try {
    await evaluate(`document.documentElement.style.fontSize = ${JSON.stringify(`${pixels}px`)}`);
    let measured;
    for (let frame = 0; frame < 30; frame++) {
      measured = await evaluate('new Promise(resolve => requestAnimationFrame(() => resolve(parseFloat(getComputedStyle(document.documentElement).fontSize))))');
      if (measured === pixels) break;
    }
    if (measured !== pixels) {
      const css = await evaluate('(() => { const root = document.documentElement; const computed = getComputedStyle(root); return { inline: root.style.fontSize, priority: root.style.getPropertyPriority("font-size"), computed: computed.fontSize, transition: computed.transitionProperty, duration: computed.transitionDuration }; })()');
      const error = new Error(`The enlarged-font override did not take effect: expected ${pixels}px, measured ${measured}px.`);
      error.fontOverrideEvidence = { expectedPixels: pixels, measuredPixels: measured ?? null, initial, css };
      throw error;
    }
    await capture({ rootFontSizePx: pixels });
  } finally {
    await evaluate(`document.documentElement.style.fontSize = ${JSON.stringify(initial.inline)}`);
  }
}
