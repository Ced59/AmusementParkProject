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

- Les vulnérabilités sont visibles dans la CI.
- Les rapports sont archivés.
- Le job ne bloque pas encore automatiquement le déploiement pour une vulnérabilité npm ou .NET détectée.
- Une fois les premiers rapports stabilisés, il sera possible de rendre bloquants certains seuils, par exemple critical/high.
