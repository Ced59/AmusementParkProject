# Entity JSON import 1.1.0

This delivery generalizes the former localized content JSON import into a broader entity JSON import workflow.

## Scope

The same admin endpoint still keeps the existing search-first workflow, but the JSON payload can now update both localized fields and selected business fields for existing entities.

Supported targets include parks, park zones, park items, park operators, park founders, attraction manufacturers, images, image tags, and reusable access condition types.

## Park item access conditions

`accessConditions` on a park item can create or update access conditions. Unknown `typeKey` values are resolved into reusable access condition type definitions in the Mongo `attractionAccessConditionTypes` collection.

## UI

Park and park item editors now expose a JSON import tab. The centralized admin JSON import page remains available for all supported entity types.

## Safety

The import is partial by default: omitted fields are not modified. Localized values are merged by language unless `replaceExisting` or `mode: "replace"` is provided.
