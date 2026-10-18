// Browser-side probe: measurable flat visible text only, not an AA certification.
// Self-contained because the exact function is evaluated in the isolated browser.
export function collectContrastCandidates(options = {}) {
  const parseColor = (value) => {
    if (!/^rgba?\(|^color\(srgb /.test(value)) return null;
    const channels = value.match(/[\d.]+/g)?.map(Number);
    if (!channels || channels.length < 3) return null;
    return [...channels.slice(0, 3).map((channel) => value.startsWith('color(') ? channel : channel / 255), channels[3] ?? 1];
  };
  const over = (front, back) => front.slice(0, 3).map((channel, index) => channel * front[3] + back[index] * (1 - front[3]));
  const luminance = (channels) => channels.slice(0, 3).map((channel) => channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4)
    .reduce((sum, channel, index) => sum + channel * [0.2126, 0.7152, 0.0722][index], 0);
  const warnings = [];
  const seen = new Set();
  let checked = 0;
  let skipped = 0;
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
  for (let node = walker.nextNode(); node; node = walker.nextNode()) {
    const text = node.textContent.trim();
    const element = node.parentElement;
    if (!text || !element || element.closest('script,style,svg,.app-brand-wordmark,.language-entry__playground-mark,[aria-hidden="true"],[aria-disabled="true"],button:disabled')) { skipped++; continue; }
    const bounds = element.getBoundingClientRect();
    if (bounds.width === 0 || bounds.height === 0 || bounds.bottom < 0 || bounds.top > innerHeight) { skipped++; continue; }
    const style = getComputedStyle(element);
    const foreground = parseColor(style.color);
    if (!foreground || foreground[3] < 0.1) { skipped++; continue; }
    const ancestors = [];
    for (let parent = element; parent; parent = parent.parentElement) ancestors.push(parent);
    if (ancestors.some((parent) => {
      const css = getComputedStyle(parent);
      const hiddenDetailsContent = parent.tagName === 'DETAILS' && !parent.open
        && !parent.querySelector(':scope > summary')?.contains(element);
      return hiddenDetailsContent || css.display === 'none' || css.visibility !== 'visible'
        || css.backgroundImage !== 'none' || Number(css.opacity) < 0.99;
    })) { skipped++; continue; }
    let background = [1, 1, 1];
    for (const parent of ancestors.reverse()) {
      const color = parseColor(getComputedStyle(parent).backgroundColor);
      if (color) background = over(color, background);
    }
    const foregroundLuminance = luminance(over(foreground, background));
    const backgroundLuminance = luminance(background);
    const ratio = (Math.max(foregroundLuminance, backgroundLuminance) + 0.05) / (Math.min(foregroundLuminance, backgroundLuminance) + 0.05);
    const size = Number.parseFloat(style.fontSize);
    const required = size >= 24 || (size >= 18.666 && Number(style.fontWeight) >= 700) ? 3 : 4.5;
    const selector = `${element.tagName.toLowerCase()}.${[...element.classList].join('.')}`;
    const key = `${selector}:${style.color}:${text}`;
    checked++;
    if (ratio + 0.01 < required && !seen.has(key)) {
      seen.add(key);
      warnings.push({ selector, text: text.slice(0, 160), foreground: style.color, background, ratio: Number(ratio.toFixed(2)), required, fontSize: size });
    }
  }
  return options.includeCoverage ? { checked, skipped, warnings, scope: 'visible flat text only; excludes images, opacity, hidden and disabled content; not full AA acceptance' } : warnings;
}
