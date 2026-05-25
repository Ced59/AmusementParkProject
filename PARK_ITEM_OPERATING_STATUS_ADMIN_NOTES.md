# Park item operating status — 1.1.0 polish

## Goal

The attraction technical field `attractionDetails.status` already existed in the API/domain model and public rendering, but it was not editable from the park item admin editor.

## Changes

- Added an admin-selectable operating status field in the park item attraction details tab.
- Added shared front-end status options:
  - `Operating`
  - `UnderConstruction`
  - `TemporarilyClosed`
  - `ClosedDefinitively`
  - `Removed`
  - `Planned`
  - `Unknown`
- Wired the field through the existing form mapper so it is preserved on update.
- Added localized labels in all supported front-end languages.
- Kept the backend unchanged because `AttractionDetails.Status` was already present in Core, WebAPI contracts, HTTP mappers, normalization and JSON import.
- Added localized public display for known statuses in the item detail technical rows and spotlight rows.
- Updated the park item JSON import example to include `attractionDetails.status`.

## Architecture note

No new backend-specific case was added. The change uses the existing `AttractionDetails.Status` property and remains a front-end editor/form-mapping improvement.
