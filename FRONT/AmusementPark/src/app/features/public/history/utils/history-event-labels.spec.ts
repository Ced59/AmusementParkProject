import {
  HISTORY_EVENT_LABEL_LANGUAGES,
  HISTORY_EVENT_TYPE_KEYS,
  HISTORY_EVENT_TYPE_LABELS,
  resolveHistoryEventTypeLabel,
} from './history-event-labels';

describe('history event labels', () => {
  it('covers every supported language with the same event types', () => {
    for (const language of HISTORY_EVENT_LABEL_LANGUAGES) {
      const languageKeys: string[] = Object.keys(
        HISTORY_EVENT_TYPE_LABELS[language] ?? {},
      ).sort();

      expect(languageKeys, language).toEqual([...HISTORY_EVENT_TYPE_KEYS]);
    }
  });

  it('resolves labels without falling back to raw PascalCase for supported languages', () => {
    for (const language of HISTORY_EVENT_LABEL_LANGUAGES) {
      expect(
        resolveHistoryEventTypeLabel('OperatorChange', language),
        language,
      ).not.toBe('Operator Change');
      expect(
        resolveHistoryEventTypeLabel('Retrack', language),
        language,
      ).toBeTruthy();
    }
  });
});
