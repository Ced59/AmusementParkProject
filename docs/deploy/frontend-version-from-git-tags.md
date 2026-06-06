# Version frontend depuis les tags Git

Le footer Angular affiche la version lue depuis `src/environments/version.generated.ts`.
Ce fichier est genere avant les builds et tests npm par `npm run generate-version`.

La version utilisee est le dernier tag reachable depuis le commit en cours, via:

```bash
git describe --tags --abbrev=0
```

Si aucun tag n'est disponible, le fallback affiche est:

```text
v0.0.0-dev
```

Le navigateur ne fait aucun appel runtime vers GitHub. Le fichier de version est inclus dans le bundle Angular au moment du build.

## Build Docker et CI

Le workflow de production recupere les tags Git avec `fetch-depth: 0` pour les jobs qui buildent le frontend.

L'image Docker frontend ne depend pas de `.git` dans son image finale. Le workflow calcule la version avant le build Docker et la transmet via le build arg `SITE_VERSION`.
Un build Docker local sans `SITE_VERSION` reste possible et utilise le fallback si aucun tag Git n'est disponible dans le contexte.

## Creer un tag apres merge

Le tag de release doit pointer vers le commit deja merge sur `master`. Ne cree pas le tag depuis une branche de pull request.

Exemple local:

```bash
git fetch origin master --tags
git switch master
git pull --ff-only origin master
git tag v0.3.0
git push origin v0.3.0
```

Il est aussi possible d'utiliser le workflow manuel GitHub Actions `Create release tag`:

1. lancer le workflow depuis GitHub Actions;
2. saisir un tag comme `v0.3.0`;
3. le workflow verifie que le tag n'existe pas deja;
4. il checkout `master`, cree le tag sur le HEAD de `master`, puis pousse le tag.

Le tag doit exister avant le build/deploy frontend qui doit l'afficher. Apres creation d'un tag post-merge, relancer le workflow de production depuis `master` si un deploiement doit embarquer cette nouvelle version.
