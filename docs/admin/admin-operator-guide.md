# Admin operator guide

This guide summarizes the repeatable admin workflow for parks and park items.

## Park setup

1. Open Admin > Parks.
2. Create the park with its name, visibility, main location, localized descriptions, logo and photos.
3. Keep the park hidden until the required public content is ready.
4. Add park zones from the park content management section.

## Fast park item creation

1. Open the park item workbench from the park items list.
2. Use quick create for repeated entries when zone, category, type or manufacturer stay the same.
3. Keep new quick items in ToReview until the content has been checked.
4. Use the full editor only for fields that are not part of the quick flow.

## Enrichment

1. Use inline edits for small corrections: zone, category, type, visibility and review status.
2. Use bulk edit only after selecting the intended rows.
3. Use the Descriptions action to open the quick description panel.
4. Save descriptions before changing filters or pages.
5. Open the full item editor for maps, access conditions, photos or detailed technical fields.

## Import

1. Paste rows into the import tool.
2. Review the preview before applying.
3. Correct rows with errors before apply.
4. Apply only validated rows and keep imported items in ToReview by default.
5. Reopen the list after import and filter incomplete content.

## Publication checks

1. Confirm the item has a name, category, precise type and zone when relevant.
2. Confirm FR and EN descriptions where public SEO coverage is expected.
3. Confirm visibility only after content quality is acceptable.
4. Use review status to separate ToReview, Validated and ToProcessLater work.

## Public contextual editing

1. Open the public page while authenticated as admin.
2. Keep the default anonymous view when checking visitor rendering.
3. Switch to user or moderator view only to compare public read behavior; these modes never grant server rights.
4. Switch to admin preview only when hidden draft content must be inspected.
5. Enable edit mode only after selecting admin preview.
6. On mobile, open one contextual drawer at a time and close it before navigating to another block.
7. For JSON updates, always run preview before apply and keep all localized languages in the bounded payload.
8. After apply, wait for the public block refresh before opening another edit action.

## Contextual editing review checklist

1. Visitor default stays anonymous and has no admin toolbar.
2. SSR output does not load the admin toolbar.
3. Simulated or authenticated public reads are not shared-cacheable.
4. JSON exports contain only the selected block and required attachment ids.
5. Localized JSON contains every supported language for localized blocks.
6. New contextual creations stay hidden or ToReview unless explicitly published later.
7. Mobile drawers have no horizontal overflow and keep primary actions reachable.

## Error recovery

1. If save fails, do not leave the editor immediately.
2. The editor keeps the form data in place and shows a retry action.
3. Retry once after network errors.
4. If the retry fails, copy the current field values into an internal issue before refreshing.
