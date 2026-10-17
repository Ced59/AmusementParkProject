// Failure-only geometry diagnostics; never changes the layout or acceptance.
export function collectLayoutOverflow() {
  return [...document.body.querySelectorAll('*')].flatMap(element => {
    const box = element.getBoundingClientRect();
    if (!box.width || !box.height || (box.x >= -1 && box.right <= innerWidth + 1)) return [];
    const style = getComputedStyle(element);
    if (style.display === 'none' || style.visibility === 'hidden') return [];
    return [{ selector: `${element.tagName.toLowerCase()}${element.id ? `#${element.id}` : ''}${[...element.classList].map(name => `.${name}`).join('')}`,
      x: box.x, right: box.right, width: box.width, scrollWidth: element.scrollWidth, clientWidth: element.clientWidth,
      fontSize: style.fontSize, minWidth: style.minWidth, gridTemplateColumns: style.gridTemplateColumns }];
  }).sort((left, right) => right.right - left.right).slice(0, 12);
}
