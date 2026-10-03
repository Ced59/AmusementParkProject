# M18.9 — Scans de dépendances dans la CI

## Objectif

M18.9 ajoute une étape de sécurité au pipeline GitHub Actions pour signaler les dépendances vulnérables côté backend .NET et côté frontend npm.

L'objectif du premier passage est volontairement progressif : produire des rapports et des warnings CI sans bloquer automatiquement tout le déploiement au premier audit.

## Workflow concerné

```txt
.github/workflows/production.yml
```

Nouveau job :

```txt
dependency-security
```

Le job s'exécute sur pull request, push et déclenchement manuel, comme les builds existants.

## Scans backend .NET

Commande exécutée :

```bash
dotnet list AmusementPark.sln package --vulnerable --include-transitive
```

Rapport archivé :

```txt
reports/dependencies/dotnet-vulnerable.txt
```

Si des vulnérabilités sont détectées, le job émet un warning GitHub Actions.

## Scans frontend npm

Commandes exécutées :

```bash
npm audit --audit-level=moderate --json
npm audit --audit-level=moderate
npm audit signatures
```

Rapports archivés :

```txt
reports/dependencies/npm-audit.json
reports/dependencies/npm-audit.txt
reports/dependencies/npm-audit-signatures.txt
```

`npm audit signatures` est exécuté en best-effort : un échec est signalé en notice, mais ne bloque pas encore la CI.

## Artefact CI

Tous les rapports sont publiés dans :

```txt
dependency-security-reports
```

## Politique actuelle

- Les vulnérabilités modérées, hautes et critiques restent visibles dans les rapports archivés.
- Une vulnérabilité critique ou haute détectée par npm ou .NET fait échouer le job et bloque les images ainsi que le déploiement.
- Les alertes modérées restent non bloquantes afin de suivre les dépendances d’outillage sans masquer le niveau de risque.
- Les rapports sont téléversés même lorsqu’un seuil bloquant est atteint.

### Exception temporaire et bornée

Une alerte haute peut exceptionnellement être tolérée lorsque l’éditeur n’a encore publié aucune version corrigée. Cette tolérance est déclarée dans `.github/dependency-security-exceptions.json` et reste bloquante par défaut :

- l’identifiant npm et l’identifiant GHSA doivent correspondre exactement ;
- toute la chaîne affectée doit être marquée `dev: true` dans le verrou npm ;
- aucune alerte critique ne peut être tolérée ;
- toute autre alerte haute continue de bloquer ;
- la tolérance expire automatiquement à la date déclarée.

L’exception actuelle concerne uniquement `GHSA-ch52-4w7c-c8xp`, transitive aux outils Angular, et expire le 17 octobre 2026. Elle doit être supprimée dès la publication d’une version corrigée.
