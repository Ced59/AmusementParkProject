// A CI-only enlarged-font stress case, not a claim of native browser zoom QA.
export async function withDoubledRootFont({ evaluate, capture }) {
  const initial = await evaluate('({ inline: document.documentElement.style.fontSize, pixels: parseFloat(getComputedStyle(document.documentElement).fontSize) })');
  if (!Number.isFinite(initial.pixels) || initial.pixels < 12 || initial.pixels > 24) throw new Error('Unexpected root font before the bounded enlarged-font case.');
  const pixels = initial.pixels * 2;
  try {
    await evaluate(`document.documentElement.style.fontSize = ${JSON.stringify(`${pixels}px`)}`);
    const measured = await evaluate('parseFloat(getComputedStyle(document.documentElement).fontSize)');
    if (measured !== pixels) throw new Error('The enlarged-font override did not take effect.');
    await capture({ rootFontSizePx: pixels });
  } finally {
    await evaluate(`document.documentElement.style.fontSize = ${JSON.stringify(initial.inline)}`);
  }
}
