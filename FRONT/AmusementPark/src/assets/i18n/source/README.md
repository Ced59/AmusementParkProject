# i18n source fragments

Edit translations in `{language}/**/*.json`.

The `../en.json`, `../fr.json`, `../es.json`, `../de.json`, `../it.json`, `../nl.json`, `../pl.json` and `../pt.json` files are generated from those fragments and are the only files loaded by the Angular runtime.

Run `npm run i18n:build` after changing source fragments, or `npm run i18n:check` to verify that generated files are current and all languages expose the same keys.

Do not add override files. Shared corrections must be written directly into the language source fragments.
