# AmusementPark — Pack de guidelines pour Codex

Branche : `docs/codex-guidelines-20260630`  
Date : 2026-06-30  
Projet : `amusement-parks.fun`

Ce dossier sert de contexte de travail pour Codex. Il centralise les règles éditoriales et techniques à appliquer lors des tâches liées aux JSON upsert, aux descriptions publiques et aux articles.

## Documents disponibles

- `park-graph-upsert-json-guideline-r10.md` : règles de génération, correction et enrichissement des Park Graph Upsert JSON.
- `description-guidelines-r2.md` : charte obligatoire pour les descriptions publiques des parcs, zones, parkItems et références.
- `articles-guideline-r2-live-sources.md` : règles éditoriales pour les articles historiques, de visite, médias et actualités durables.

## Ordre de lecture conseillé pour Codex

1. Lire ce `README.md`.
2. Lire `park-graph-upsert-json-guideline-r10.md` avant toute modification JSON.
3. Lire `description-guidelines-r2.md` avant toute rédaction ou réécriture de description.
4. Lire `articles-guideline-r2-live-sources.md` avant toute création ou modification d’article.

## Règles globales non négociables

- Toujours vérifier la pertinence de l’entité pour `amusement-parks.fun` avant d’enrichir ou de formater un JSON.
- Ne jamais enrichir artificiellement une entité douteuse.
- Ne pas se limiter aux coasters : référencer les attractions, zones, restaurants, boutiques, hôtels, parkings, services, points d’accès, spectacles fixes, animaux/enclos et autres contenus visiteurs nommables quand ils sont fiables.
- Les descriptions doivent être naturelles, spécifiques au lieu, agréables à lire, orientées visiteur et non mécaniques.
- Ne jamais écrire de formulations du type “ce que ça apporte à la journée”, “au groupe”, “comment l’intégrer dans la journée” ou “quand cela devient utile”.
- Ne pas mettre les conditions d’accès, restrictions, tailles, tarifs ou informations purement techniques dans les descriptions : ces données doivent aller dans les champs JSON prévus.
- Les images externes doivent être des liens directs vers de vrais fichiers image téléchargeables, sans proxy CDN, preview, watermark ou page HTML.
- Tout `manufacturerKey`, `zoneKey`, `operatorKey`, `founderKey` ou `ownerKey` utilisé doit être résolu dans le même JSON ou par une identité existante sûre.
- Les horaires, dates d’ouverture et événements datés doivent être vérifiés avec des sources actuelles et ne doivent pas être mélangés aux tarifs si les tarifs ne sont pas implémentés.
- Les articles doivent apporter une vraie valeur éditoriale, avec des sources vérifiées, et ne doivent pas devenir des fiches techniques déguisées.

## Usage attendu

Quand Codex travaille sur le projet, il doit citer ou appliquer ces fichiers comme règles de référence, puis produire des changements cohérents avec le style validé pour AmusementPark.