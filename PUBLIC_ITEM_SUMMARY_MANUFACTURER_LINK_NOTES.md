# Public item summary manufacturer link

Version: 1.1.0

## Change

The public park item detail "En un coup d'œil" / summary panel now displays a conditional manufacturer row when the attraction has a linked manufacturer and its public reference data can be resolved.

## Behaviour

- If `attractionDetails.manufacturerId` and the manufacturer name are both available, the summary panel displays the manufacturer.
- The manufacturer value is rendered as a link to the public manufacturer reference page, using the same route helper already used by the technical details block.
- If no manufacturer is linked or the name cannot be resolved, the row is omitted and the panel layout remains unchanged.

## Architecture

The change stays in the public park item view mapper. It reuses the existing `buildPublicParkReferenceRouteCommands` helper and the existing `ParkItemDetailRowViewModel` link rendering path, without adding special rendering logic in the template.
