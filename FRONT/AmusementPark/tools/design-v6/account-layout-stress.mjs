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

// Native CDP environment override, not a physical-device or browser-zoom claim.
export async function withBottomSafeArea({ cdp, capture }) {
  const zeroInsets = { top: 0, left: 0, bottom: 0, right: 0, topMax: 0, leftMax: 0, bottomMax: 0, rightMax: 0 };
  await cdp.send('Emulation.setSafeAreaInsetsOverride', { insets: { ...zeroInsets, bottom: 34, bottomMax: 34 } });
  try {
    await capture({ safeAreaBottomPx: 34 });
  } finally {
    // Unset CDP values become undefined; explicit zeros restore this CI baseline.
    await cdp.send('Emulation.setSafeAreaInsetsOverride', { insets: zeroInsets });
  }
}
