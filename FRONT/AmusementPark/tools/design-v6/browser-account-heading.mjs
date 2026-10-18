// Browser-side presentation measurements, independent of authentication and data.
export function collectAccountAsideHeading() {
  const aside = document.querySelector('.app-account-layout__aside');
  const bounds = node => {
    const box = node.getBoundingClientRect();
    return { x: box.x, y: box.y, right: box.right, bottom: box.bottom, width: box.width, height: box.height };
  };
  if (!aside || aside.getBoundingClientRect().width === 0 || aside.getBoundingClientRect().height === 0) return { visible: false };
  const title = aside.querySelector('.app-account-layout__title');
  if (!title) return { visible: true, aside: bounds(aside), title: null, textRects: [] };
  const range = document.createRange();
  range.selectNodeContents(title);
  return { visible: true, aside: bounds(aside), title: bounds(title),
    textRects: [...range.getClientRects()].filter(part => part.width > 0 && part.height > 0)
      .map(part => ({ x: part.x, y: part.y, right: part.right, bottom: part.bottom })) };
}

export function accountAsideHeadingIssues(measurement) {
  if (measurement?.visible === false) return [];
  const finite = (box, keys) => box && keys.every(key => Number.isFinite(box[key]));
  if (measurement?.visible !== true || !finite(measurement.aside, ['x', 'y', 'right', 'bottom', 'width', 'height'])
    || measurement.aside.width <= 0 || measurement.aside.height <= 0
    || !finite(measurement.title, ['x', 'y', 'right', 'bottom', 'width', 'height'])
    || measurement.title.width <= 0 || measurement.title.height <= 0
    || !Array.isArray(measurement.textRects) || !measurement.textRects.length
    || measurement.textRects.some(box => !finite(box, ['x', 'y', 'right', 'bottom']))) {
    return ['account introduction heading geometry not measured'];
  }
  const aside = measurement.aside;
  return measurement.textRects.some(box => box.x < aside.x - 1 || box.right > aside.right + 1
    || box.y < aside.y - 1 || box.bottom > aside.bottom + 1)
    ? ['account introduction heading text clipped outside its panel'] : [];
}
