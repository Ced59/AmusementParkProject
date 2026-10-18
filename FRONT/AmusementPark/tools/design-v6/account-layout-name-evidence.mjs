// Exact presentation-only migration: two static English names become i18n names.
// Never permits a missing handler, input, permission or other template contract.
export function accountLayoutNameLocalizationEvidence(file, source, missing) {
  const expected = [
    ['attribute', 'aside', 'aria-label', 'Account experience introduction'],
    ['attribute', 'div', 'aria-label', 'Account feature highlights'],
  ];
  if (file !== 'FRONT/AmusementPark/src/app/ui/layouts/account-layout/account-layout.component.html'
      || JSON.stringify(missing) !== JSON.stringify(expected)
      || !source.includes("[attr.aria-label]=\"'accountLayout.introductionLabel' | translate\"")
      || !source.includes("role=\"group\" [attr.aria-label]=\"'accountLayout.featuresLabel' | translate\"")) return null;
  return expected.map(contract => ({ contract,
    reason: 'English-only account region/group names are localized in eight dictionaries; the feature group now has explicit group semantics.' }));
}
