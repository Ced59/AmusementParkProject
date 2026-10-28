# Suivi permanent des intégrations

Ce registre situé dans le dépôt GitHub est la source de vérité de l’inventaire et du suivi par pays. Les JSON `countries/XX.json` portent les données ; les Markdown et le résumé sont des vues générées. L’Excel remis avec le rapport est une photographie de consultation, pas une seconde source à maintenir.

## Règle permanente

**Ne jamais effacer une entrée parce que le parc ou la standalone a été intégré.** Conserver son `entryId`, ses identifiants présents et anciens, ses sources, son état, la date UTC de la dernière intégration réellement appliquée et tout l’historique des intégrations. Conserver également les doublons et exclusions avec leur justification ; cela évite leur redécouverte comme nouvelles créations.

- `PublishedDateUnknown` : fiche déjà visible au relevé ; date d’intégration inconnue, qualité non réauditée ici.
- `ToIntegrate` : fiche masquée existante nécessitant une intégration éditoriale complète.
- `ToQualify` : identité, type, pertinence, existence ou rattachement à un parc à décider avant intégration.
- `InProgress` : dossier réservé ou intégration partielle.
- `Blocked` : obstacle précis documenté dans `nextAction`.
- `Integrated` : intégration terminée avec preuve d’Apply/import et audit final. La publication et Facebook suivent leurs propres autorisations.
- `Excluded` / `Duplicate` : trace conservée ; justifier le motif et renseigner `duplicateOf` pour une fusion.

La date `lastDataIntegrationAt` est le maximum des dates réelles `integrationHistory[].appliedAt`. Elle ne change pas pour une recherche, un audit seul, une réservation, une modification de ce fichier ou une publication sans nouvelle donnée. Une intégration partielle réussie met déjà à jour cette date, sans déclarer le dossier achevé. Les anciennes dates inconnues restent nulles jusqu’à récupération d’une preuve ; ne pas leur attribuer le 22 octobre 2026.

**Aucune montée de version pour une mise à jour du backlog seule**, y compris ses preuves d’intégration et ses règles de suivi. Regrouper ces changements dans une PR documentaire ; ne pas modifier `release-version.json`. Une modification distincte du code du site conserve son propre cycle de version.

Lorsqu’elles sont autorisées, consigner séparément la publication du site et l’annonce Facebook : statut réel, date, URL publique et identifiant de preuve. Une annonce Facebook n’est réussie que lorsque le client officiel renvoie `Published` et son lien ; ne pas confondre un brouillon résolu avec un post publié. Ces dates restent distinctes de `lastDataIntegrationAt`, et la preuve existante doit être consultée avant tout nouvel envoi.

## Qualification des standalone

Consulter aussi [STANDALONE-AUDIT.md](STANDALONE-AUDIT.md). Le compteur des candidates sans fiche ne couvre pas à lui seul le travail standalone : examiner les pistes présentes dans des fiches Park et les dossiers sans ID dont l’identité ou le rattachement reste à résoudre. Ces compteurs sont des sous-ensembles, pas de nouvelles lignes à additionner. Conserver les références `standaloneAssessment.attractions` ; une installation constructeur peut être un parkItem, une modernisation ou un alias. Réserver le dossier existant avant de créer une nouvelle identité.

## Photographies des standalone

Une nouvelle fiche standalone destinée à la publication doit comporter au moins une photographie qui montre réellement cette attraction. Vérifier la source primaire, la licence ou l’autorisation de réutilisation, l’auteur et la date avant `ImportPhoto` ; renseigner le lien source, les crédits, les textes alternatifs et les légendes dans les huit langues. Une photo d’un autre équipement du même site ou une image générique ne comble pas ce besoin. Si aucune photo utilisable n’est établie, conserver la fiche en travail dans le backlog, avec la recherche menée et la prochaine action, puis reprendre la publication après résolution.

Les fiches déjà publiques sans photo restent visibles et prioritaires pour le rattrapage. Le [relevé du 28 octobre 2026](evidence/standalone-photo-coverage-20261028/SOURCES.md) distingue les photos propres aux attractions du logo générique, avec le détail par entrée. Un import photographique est une intégration de données datée par son reçu `ImportPhoto` ; la publication du média est consignée à part et ne remplace pas cette date.

## Découverte de nouvelles fiches

La [liste des créations candidates](CREATIONS-STANDALONE.md) isole les attractions sans correspondance trouvée, avec une section alpine / mountain coasters. Poursuivre la découverte hors du catalogue : annuaires spécialisés, références constructeurs, exploitants et tourisme local. Comparer les noms, alias, adresses, coordonnées et anciennes identités aux Park et StandaloneAttraction publics et masqués ; élargir la recherche au-delà du pays en cas de classement erroné.

Créer une nouvelle entrée JSON seulement quand aucun dossier ne la représente déjà, avec un `entryId` permanent, les sources et la date du relevé, `siteId: null`, `lastDataIntegrationAt: null` et un historique vide. Utiliser `PossibleExisting` si l’identité ou le parent est incertain. Ne pas confondre référence historique et attraction actuellement ouverte. Enregistrer la première date réelle uniquement après l’Apply/import réussi. Pour une installation transfrontalière, conserver une seule identité et un renvoi dans l’autre pays.

## Prise en charge et coordination

1. Se synchroniser avec `origin/master` et lire le JSON du pays, le backlog national historique et les règles API en vigueur.
2. Rechercher à nouveau l’entité via le client officiel dans les parcs et les standalone. Réconcilier les travaux intervenus depuis le relevé, les homonymes, les anciennes identités et les migrations.
3. Réserver la ligne avec un nom de conversation ou de branche explicite. La réservation locale détecte un propriétaire différent ; entre deux clones, publier la réservation sur une branche partagée et vérifier les autres branches/campagnes. Le verrou local n’est pas un verrou distribué d’API.
4. Pour France et Portugal, les campagnes déjà en cours restent responsables de leur lot : consulter leurs résultats avant de démarrer une intégration concurrente. Les autorisations de publication propres à ces campagnes ne sont pas étendues à d’autres pays.

```bash
node tools/codex/integration-backlog.mjs claim --country PT --entry PT-candidate-identifiant --owner conversation-portugal
```

Remplacer l’identifiant d’exemple par un `entryId` réel du JSON. Une réservation n’invente aucune date d’intégration.

## Consigner une intégration réelle

Après chaque lot de données effectivement appliqué, utiliser son reçu ou identifiant de lot comme `eventId`. Conserver une référence de preuve non sensible ; les jetons, exports complets et données privées restent hors du dépôt.

```bash
node tools/codex/integration-backlog.mjs record --country PT --entry PT-candidate-identifiant --event apply-identifiant --at 2026-10-22T18:00:00Z --outcome partial --kind Park --entity-id UUID_REEL --evidence reference-recu --summary "Descriptions et histoire intégrées"
```

Pour terminer : `--outcome completed --audit reference-audit-final`, avec la date du dernier Apply/import réel et sa preuve, pas la date de l’audit. Si le dernier reçu a déjà été enregistré partiellement, ajouter un événement de clôture distinct à la même date réelle, avec la même preuve et la référence d’audit ; ne pas réécrire le reçu antérieur. Utiliser `--kind StandaloneAttraction` pour une attraction autonome.

Le script refuse les dates futures/invalides, les dates antérieures au dernier événement, les preuves manquantes et la réutilisation contradictoire d’un événement. Il conserve l’ancien UUID lors d’une migration, met à jour la date et régénère la vue nationale et le résumé. Une création passe en présence connue mais visibilité à vérifier ; consigner ensuite la visibilité réellement retournée par la recherche officielle, sans la déduire du seul statut Integrated. Pour actualiser la visibilité ou rouvrir une qualification, modifier le JSON avec une preuve datée, puis régénérer les vues.

## Vérification et livraison

```bash
node --test tools/codex/integration-backlog.test.mjs
node tools/codex/integration-backlog.mjs render
node tools/codex/integration-backlog.mjs validate
```

`validate --baseline CHEMIN_DU_REGISTRE_PRECEDENT` interdit la disparition d’un pays, d’une entrée ou d’un événement antérieur et la réécriture de l’historique. La CI applique cette comparaison à la base de la PR. Le résumé et les vues doivent correspondre aux JSON.

Pour corriger un pays (notamment depuis `ZZ`), déplacer l’entrée entière vers le JSON du pays confirmé, sans changer son `entryId` ni son historique. Son préfixe peut donc conserver l’ancien code pays. Documenter la preuve dans les sources et `countryResolution`, conserver le pays initial dans `countryCodeAtInventory`, puis régénérer les vues. La conservation est vérifiée à l’échelle mondiale : le déplacement est permis, la disparition ou la duplication de l’identifiant ne l’est pas. Les commandes suivantes utilisent le nouveau `--country` et le même `--entry`.

En cas de conflit Git, réunir les entrées et les événements des deux branches par identifiant, puis régénérer les vues. Ne jamais choisir une version entière au prix de la perte d’une intégration concurrente. Regrouper les mises à jour documentaires selon la règle des lots du dépôt ; aucun nouveau déploiement n’est nécessaire à chaque Apply. Les compteurs « publiés/masqués » décrivent la dernière présence observée, et le compteur « intégrations avec audit » décrit l’avancement éditorial : ils mesurent deux choses différentes.

## Message à reprendre dans les conversations France et Portugal

Deux [prompts complets, France et Portugal](PROMPTS-FR-PT.md), sont disponibles pour reprendre les campagnes avec le registre corrigé.

> Le nouvel inventaire mondial inclut désormais la France et le Portugal. Avant le prochain lot, synchronise le dépôt et lis `docs/roadmaps/world-integration/README.md`, `WORKFLOW.md` et `countries/FR.json` ou `countries/PT.json`. Rapproche les intégrations réalisées depuis le relevé du 22 octobre 2026 et réserve les prochaines entrées pour éviter les doublons. Une fiche masquée nécessite une vraie intégration éditoriale. Après chaque intégration, conserve la ligne, ajoute sa preuve et sa date réelle dans l’historique ; ne l’efface plus du backlog. Les règles de publication propres au lot national restent applicables.

Ce message est un relais versionné. La présente session ne dispose pas d’un outil permettant de l’envoyer directement dans une autre conversation ChatGPT.
