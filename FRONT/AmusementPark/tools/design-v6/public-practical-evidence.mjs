export function publicPracticalReadEvidence(target, status, bytes) {
  const url = new URL(target, 'https://amusement-parks.fun');
  if (!/^\/api\/parks\/[0-9a-f-]{36}\/weather\/historical-comparisons$/i.test(url.pathname) || status !== 200) return undefined;
  if (bytes.byteLength > 2 * 1024 * 1024) throw new Error('Public practical evidence exceeds its response bound.');
  const parsed = JSON.parse(Buffer.from(bytes).toString('utf8'));
  const data = parsed.data ?? parsed;
  if (!Array.isArray(data.years) || data.years.length > 20) throw new Error('Invalid bounded weather history evidence.');
  return { kind: 'weather-history', yearsCount: data.years.length };
}

export function openingCalendarLayoutIssues(cells) {
  if (!Array.isArray(cells) || cells.length < 2) return ['missing calendar cells'];
  const issues = [];
  const inside = (child, parent) => child && parent && child.x >= parent.x - 1
    && child.right <= parent.right + 1 && child.y >= parent.y - 1 && child.bottom <= parent.bottom + 1;
  for (const [index, cell] of cells.entries()) {
    if (!inside(cell.button, cell.bounds)) issues.push(`cell ${index}: button outside table cell`);
    if (!cell.labels?.length) issues.push(`cell ${index}: missing day label`);
    for (const label of cell.labels ?? []) {
      if (!inside(label.bounds, cell.button)) issues.push(`cell ${index}: label outside button`);
      if (label.scrollWidth > label.clientWidth + 1 || label.scrollHeight > label.clientHeight + 1) {
        issues.push(`cell ${index}: clipped label`);
      }
      if (label.today && (label.textOverflow === 'ellipsis' || label.whiteSpace === 'nowrap')) {
        issues.push(`cell ${index}: today label cannot reflow`);
      }
    }
  }
  return issues;
}
