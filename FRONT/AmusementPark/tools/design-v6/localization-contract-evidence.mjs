// Presentation changes may add labels, but must preserve every existing value.
export function compareLocalizationContracts(before, after) {
  const leaves = (value, prefix = '', result = new Map()) => {
    if (value !== null && typeof value === 'object' && !Array.isArray(value)) {
      for (const [key, child] of Object.entries(value)) leaves(child, prefix ? `${prefix}.${key}` : key, result);
    } else result.set(prefix, JSON.stringify(value));
    return result;
  };
  const original = leaves(before);
  const current = leaves(after);
  const missing = [...original.keys()].filter(key => !current.has(key));
  const changed = [...original.keys()].filter(key => current.has(key) && current.get(key) !== original.get(key));
  const added = [...current.keys()].filter(key => !original.has(key));
  return { baselineCount: original.size, currentCount: current.size, missing, changed, added };
}
