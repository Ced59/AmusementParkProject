# Version frontend depuis la version de PR et les tags Git

Le footer Angular affiche la version lue depuis `src/environments/version.generated.ts`.
Ce fichier est genere avant les builds et tests npm par `npm run generate-version`.

Chaque PR qui prepare une version frontend porte la version attendue dans:

```text
FRONT/AmusementPark/release-version.json
```

Pour configurer cette version localement:

```bash
cd FRONT/AmusementPark
npm run release:version -- 1.2.0
```

Le fichier `release-version.json` doit etre committe dans la PR. Il sert a:

- afficher la version voulue dans les builds de PR;
- informer la PR de la version a creer;
- creer automatiquement le tag sur `master` apres merge.

Les tags de ce repository utilisent le format sans prefixe `v`, par exemple:

```text
1.2.0
```

Si aucun tag n'est disponible, le fallback affiche est:

```text
v0.0.0-dev
```

Le navigateur ne fait aucun appel runtime vers GitHub. Le fichier de version est inclus dans le bundle Angular au moment du build.

## Build Docker et CI

Le workflow de production recupere les tags Git avec `fetch-depth: 0` pour les jobs qui buildent le frontend.

Sur pull request, la CI lit `release-version.json`, verifie que la version est valide, et verifie que le tag n'existe pas deja si le fichier de version a change dans la PR.

Sur push vers `master` apres merge, la CI:

1. lit `release-version.json`;
2. detecte si ce fichier a change dans le commit merge;
3. verifie que le tag n'existe pas deja;
4. cree le tag sur le SHA merge dans `master`;
5. build et deploie le frontend avec cette version.

L'image Docker frontend ne depend pas de `.git` dans son image finale. Le workflow transmet la version creee/taggee via le build arg `SITE_VERSION`.
Un build Docker local sans `SITE_VERSION` reste possible et utilise `release-version.json`, puis les tags Git, puis le fallback si aucune source n'est disponible.

## Creer un tag apres merge

Le tag de release est cree automatiquement par la pipeline de production quand la PR est mergee dans `master` et que `release-version.json` a change.

Ne cree pas le tag depuis une branche de pull request. Si la creation du tag echoue, verifier que la version de PR n'est pas deja utilisee par un tag existant.
