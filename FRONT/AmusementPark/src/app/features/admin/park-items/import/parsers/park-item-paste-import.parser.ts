import { ParkItemBulkCreateDraft } from '@app/models/parks/park-item-bulk-create';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';

type ColumnKey =
  | 'name'
  | 'category'
  | 'type'
  | 'zoneName'
  | 'manufacturerName'
  | 'isVisible'
  | 'descriptionFr';

const DEFAULT_COLUMNS: readonly ColumnKey[] = [
  'name',
  'category',
  'type',
  'zoneName',
  'manufacturerName',
  'descriptionFr',
];

const HEADER_ALIASES: Readonly<Record<string, ColumnKey>> = {
  name: 'name',
  nom: 'name',
  item: 'name',
  category: 'category',
  categorie: 'category',
  cat: 'category',
  type: 'type',
  zone: 'zoneName',
  zonename: 'zoneName',
  constructeur: 'manufacturerName',
  manufacturer: 'manufacturerName',
  fabricant: 'manufacturerName',
  visible: 'isVisible',
  visibility: 'isVisible',
  visibilite: 'isVisible',
  description: 'descriptionFr',
  descriptionfr: 'descriptionFr',
};

export function parseParkItemPasteImport(source: string): ParkItemBulkCreateDraft[] {
  const lines: string[] = source
    .split(/\r?\n/)
    .map((line: string) => line.trim())
    .filter((line: string) => line.length > 0 && !line.startsWith('#') && !line.startsWith('//'));

  if (lines.length === 0) {
    return [];
  }

  const separator: string = detectSeparator(lines);
  const firstValues: string[] = splitDelimitedLine(lines[0], separator);
  const headerColumns: ColumnKey[] | null = mapHeader(firstValues);
  const columns: readonly ColumnKey[] = headerColumns ?? DEFAULT_COLUMNS;
  const dataLines: string[] = headerColumns ? lines.slice(1) : lines;

  return dataLines
    .map((line: string, index: number) => mapValuesToDraft(splitDelimitedLine(line, separator), columns, index + 1))
    .filter((draft: ParkItemBulkCreateDraft) => !!draft.name?.trim());
}

function detectSeparator(lines: readonly string[]): string {
  const candidates: readonly string[] = ['\t', ';', ','];
  let bestSeparator: string = ';';
  let bestScore: number = -1;

  for (const candidate of candidates) {
    const score: number = lines
      .slice(0, 5)
      .reduce((sum: number, line: string) => sum + splitDelimitedLine(line, candidate).length, 0);
    if (score > bestScore) {
      bestSeparator = candidate;
      bestScore = score;
    }
  }

  return bestScore <= lines.slice(0, 5).length ? ';' : bestSeparator;
}

function mapHeader(values: readonly string[]): ColumnKey[] | null {
  const mapped: ColumnKey[] = values
    .map((value: string) => HEADER_ALIASES[normalizeToken(value)])
    .filter((value: ColumnKey | undefined): value is ColumnKey => !!value);

  return mapped.includes('name') && mapped.length >= Math.min(values.length, 2) ? mapped : null;
}

function mapValuesToDraft(values: readonly string[], columns: readonly ColumnKey[], rowNumber: number): ParkItemBulkCreateDraft {
  const draft: ParkItemBulkCreateDraft = {
    rowNumber,
    adminReviewStatus: 'ToReview',
    isVisible: false,
  };

  columns.forEach((column: ColumnKey, index: number) => {
    const value: string | null = normalizeValue(values[index]);
    if (value === null) {
      return;
    }

    switch (column) {
      case 'name':
        draft.name = value;
        break;
      case 'category':
        draft.category = parseEnum<ParkItemCategory>(value, [
          'Attraction',
          'Restaurant',
          'Hotel',
          'Animal',
          'Show',
          'Shop',
          'Service',
          'Transport',
          'Other',
        ]);
        break;
      case 'type':
        draft.type = parseEnum<ParkItemType>(value, [
          'Attraction',
          'RollerCoaster',
          'WaterRide',
          'FlatRide',
          'DarkRide',
          'FamilyRide',
          'ThrillRide',
          'TransportRide',
          'WalkThrough',
          'Playground',
          'InteractiveExperience',
          'Cinema',
          'ObservationRide',
          'AnimalExhibit',
          'Restaurant',
          'Snack',
          'Hotel',
          'Show',
          'Shop',
          'Game',
          'MeetAndGreet',
          'Service',
          'Toilets',
          'FirstAid',
          'Information',
          'Locker',
          'Parking',
          'Transport',
          'Station',
          'Other',
        ]);
        break;
      case 'zoneName':
        draft.zoneName = value;
        break;
      case 'manufacturerName':
        draft.manufacturerName = value;
        break;
      case 'isVisible':
        draft.isVisible = parseBoolean(value);
        break;
      case 'descriptionFr':
        draft.descriptionFr = value;
        break;
    }
  });

  return draft;
}

function splitDelimitedLine(line: string, separator: string): string[] {
  const values: string[] = [];
  let current: string = '';
  let quoted: boolean = false;

  for (let index: number = 0; index < line.length; index += 1) {
    const character: string = line[index];
    const nextCharacter: string | undefined = line[index + 1];

    if (character === '"' && quoted && nextCharacter === '"') {
      current += '"';
      index += 1;
      continue;
    }

    if (character === '"') {
      quoted = !quoted;
      continue;
    }

    if (character === separator && !quoted) {
      values.push(current.trim());
      current = '';
      continue;
    }

    current += character;
  }

  values.push(current.trim());
  return values;
}

function parseEnum<T extends string>(value: string, allowedValues: readonly T[]): T | null {
  const normalizedValue: string = normalizeToken(value);
  return allowedValues.find((allowedValue: T) => normalizeToken(allowedValue) === normalizedValue) ?? null;
}

function parseBoolean(value: string): boolean | null {
  const normalizedValue: string = normalizeToken(value);
  if (['true', '1', 'yes', 'oui', 'visible'].includes(normalizedValue)) {
    return true;
  }

  if (['false', '0', 'no', 'non', 'hidden', 'masque'].includes(normalizedValue)) {
    return false;
  }

  return null;
}

function normalizeValue(value: string | undefined): string | null {
  const normalized: string = (value ?? '').trim();
  return normalized.length > 0 ? normalized : null;
}

function normalizeToken(value: string): string {
  return value
    .trim()
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .replace(/[^a-zA-Z0-9]/g, '')
    .toLowerCase();
}
