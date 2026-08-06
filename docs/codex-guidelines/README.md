# AmusementPark — Pack de guidelines pour Codex

Date : 2026-08-06
Projet : `amusement-parks.fun`

Ce dossier sert de contexte de travail pour Codex. Il centralise les règles éditoriales et techniques à appliquer lors des tâches liées aux JSON upsert, aux descriptions publiques et aux articles.

## Entrée recommandée

- `park-data-integration-orchestrator.md` : fichier à donner à ChatGPT/Codex pour intégrer un parc de bout en bout sans saturer le contexte. Il impose le parcours par étapes, l’export initial, l’export actualisé avant chaque nouvelle étape, les limites de lots et les fichiers de règles à lire selon l’étape.
- `standalone-attraction-data-integration.md` : fichier à utiliser quand l’entité pertinente est une attraction fixe isolée et non un parc.
- `codex-park-data-editor-api-workflow.md` : complément strictement réservé à Codex lorsqu’il exécute lui-même les étapes par API avec le rôle technique `PARK_DATA_EDITOR`. Il ajoute les garde-fous Preview/Apply et l’upload local des photos sans modifier le workflow ChatGPT.

## Commande courte de complétude

La demande `Complète le parc <nom>` suffit pour lancer le parcours complet avec Codex. Elle signifie :

- retrouver ou cadrer le parc à l’étape 0 ;
- exécuter de façon autonome toutes les étapes 1 à 8 applicables avec le flux API `PARK_DATA_EDITOR` ;
- réexporter après chaque Apply et travailler en lots bornés jusqu’à épuisement réel de chaque étape ;
- rechercher aussi les lacunes de l’existant, pas seulement ajouter les informations les plus faciles à trouver ;
- atteindre le contrat de complétude exigeant décrit dans l’orchestrateur et produire un audit chiffré final ;
- s’arrêter au seuil `prêt pour publication` tant que l’utilisateur n’a pas explicitement demandé de publier.

La commande ne demande ni modification de code, ni accès direct à l’administration. Si le jeton technique manque, est expiré ou révoqué, Codex signale le blocage sans contourner le flux prévu.

Dans ChatGPT, le même orchestrateur et les mêmes exigences éditoriales s’appliquent, mais l’utilisateur reste l’opérateur des exports, Preview et Apply et valide le passage entre les étapes.

## Documents disponibles

- `park-data-integration-orchestrator.md` : orchestrateur principal du parcours complet.
- `codex-park-data-editor-api-workflow.md` : surcouche d’exécution API autonome réservée à Codex ; ChatGPT continue d’utiliser l’orchestrateur et les étapes existantes sans changement.
- `standalone-attraction-data-integration.md` : flux d’intégration et de migration des attractions fixes isolées.
- `park-graph-upsert-enums.md` : liste des enums et valeurs autorisées dans les JSON Park Graph Upsert.
- `park-data-integration-steps/00-intake-and-export.md` : cadrage, pertinence, export et découpage anti-saturation.
- `park-data-integration-steps/01-park-core-upsert.md` : identité du parc, dates principales, coordonnées, statut, exploitant et fondateur.
- `park-data-integration-steps/02-zones-upsert.md` : zones officielles et structure de visite.
- `park-data-integration-steps/03-park-items-inventory-upsert.md` : inventaire des parkItems, dates, statuts, références et rattachements.
- `park-data-integration-steps/04-rich-descriptions-localization.md` : descriptions longues, naturelles et localisées dans les 8 langues.
- `park-data-integration-steps/05-images-and-reference-enrichment.md` : images importables, logos, crédits, biographies et références.
- `park-data-integration-steps/06-opening-hours-and-named-events.md` : horaires, exceptions datées et événements nommés.
- `park-data-integration-steps/07-history-timelines-and-articles.md` : histoire du parc, histoire des parkItems et articles rattachés.
- `park-data-integration-steps/08-final-audit-and-publication.md` : audit final avant publication.

## Ordre de lecture conseillé pour Codex

1. Lire ce `README.md`.
2. Lire `park-data-integration-orchestrator.md` pour une intégration complète de parc.
3. Lire `codex-park-data-editor-api-workflow.md` pour les autorisations et le parcours d’images propres à Codex.
4. Lire successivement les fichiers applicables des étapes 0 à 8 dans `park-data-integration-steps/`.

## Règles globales non négociables

- Toujours vérifier la pertinence de l’entité pour `amusement-parks.fun` avant d’enrichir ou de formater un JSON.
- Ne jamais enrichir artificiellement une entité douteuse.
- Ne pas se limiter aux coasters : référencer les attractions, zones, restaurants, boutiques, hôtels, parkings, services, points d’accès, spectacles fixes, animaux/enclos et autres contenus visiteurs nommables quand ils sont fiables.
- Une attraction fixe isolée pertinente ne doit pas être transformée en faux parc. Utiliser le flux `StandaloneAttraction` et migrer l’ancien parc mono-attraction si une fiche legacy existe.
- Les descriptions doivent être naturelles, spécifiques au lieu, agréables à lire, orientées visiteur et non mécaniques.
- Une description raconte l’entité elle-même : ce que le visiteur voit, son ambiance et sa singularité. Elle ne donne jamais de consigne d’itinéraire, ne conseille pas de « garder » un lieu pour un profil, ne propose pas une pause entre des files et ne réutilise pas un paragraphe passe-partout pour toute une catégorie.
- Cette exigence éditoriale s’applique aussi aux titres, résumés, articles, descriptions internes visibles, textes alternatifs et légendes d’images. Les champs de crédits portent les crédits ; les textes visiteurs ne décrivent jamais la collecte, l’import, la qualité de la source ou le fonctionnement d’un outil.
- Ne jamais écrire de formulations du type “ce que ça apporte à la journée”, “au groupe”, “comment l’intégrer dans la journée” ou “quand cela devient utile”.
- Ne pas mettre les conditions d’accès, restrictions, tailles, tarifs ou informations purement techniques dans les descriptions : ces données doivent aller dans les champs JSON prévus.
- Les conditions d’accès de chaque attraction doivent être recherchées systématiquement et intégrées dans `items[].attractionDetails.accessConditions[]` quand elles sont fiables.
- Les enums utilisées dans un JSON upsert doivent venir de `park-graph-upsert-enums.md`, avec les valeurs canoniques exactes.
- Les dates ne doivent jamais être inventées. Si seule l’année d’ouverture ou de fermeture est fiable, renseigner l’année seule dans le JSON ; ne jamais fabriquer un `01-01` ou un premier jour de mois.
- Les images externes doivent pointer vers une URL HTTP(S) publique que l’importeur peut télécharger et reconnaître comme image réelle. Un CDN est accepté s’il renvoie bien des octets d’image importables.
- Rechercher et vérifier le logo officiel actuel comme un élément distinct de la photo principale. Il doit devenir le logo courant, rester sans watermark ajouté et être contrôlé dans l’export final.
- Rechercher une image fidèle pour chaque attraction actuelle, annoncée, en construction ou définitivement fermée, ainsi que pour chaque jalon et article historique, quand une image acceptable existe. Une source non officielle est admissible si l’image montre sans ambiguïté la bonne entité, reste créditable et ne porte aucun watermark d’un site tiers.
- Ne conclure à une image introuvable qu’après une recherche réelle dans les sources officielles, presse, archives et sources spécialisées pertinentes. Conserver la lacune et sa raison dans l’audit plutôt que d’utiliser une image générique ou trompeuse.
- Une image ne doit jamais être livrée si son propriétaire n’est pas résolu. Un warning Preview du type `Remote image ignored: owner could not be resolved` est une erreur de livrable à corriger avant import.
- Tout `manufacturerKey`, `zoneKey`, `operatorKey`, `founderKey` ou `ownerKey` utilisé doit être enregistré par la section que le processeur traite avant son utilisation. Pour une image de parkItem ou de référence, l’existence en base ne remplace pas la redéclaration dans `items[]` ou `references`.
- Les `zoneKey` et `manufacturerKey` sont des causes fréquentes d’erreurs : tout JSON qui les utilise doit embarquer les zones minimales et constructeurs minimaux nécessaires quand l’export actualisé ne prouve pas déjà leur existence.
- Une alerte de clé non résolue effectivement retournée par Preview bloque le livrable. Les clés d’images utilisées par les articles doivent en plus être comparées statiquement aux `images[].key` du même JSON, car le Preview ne les valide pas.
- Les horaires, dates d’ouverture et événements datés doivent être vérifiés avec des sources actuelles et ne doivent pas être mélangés aux tarifs si les tarifs ne sont pas implémentés.
- Les libellés et raisons visibles dans le calendrier doivent être réservés aux événements nommés, exceptions datées ou informations temporaires utiles. Ne jamais y répéter des commentaires généraux sur tous les jours normaux.
- Les articles doivent apporter une vraie valeur éditoriale, avec des sources vérifiées, et ne doivent pas devenir des fiches techniques déguisées.
- Pour un parc majeur ou historiquement riche, rechercher les attractions définitivement fermées, les transformations structurantes et les annonces récentes à effet durable. Une histoire légère ou limitée à l’ouverture du parc n’est pas considérée complète lorsque les sources permettent davantage.
- Les événements et articles historiques doivent être rédigés pour les visiteurs, sans phrases d’audit interne, justification de méthode, “repère documentaire prudent” ou formulation mécanique sur la présence confirmée d’un élément.
- Les sources d’articles et d’événements doivent être des URL HTTP(S) valides et joignables au moment de la génération. Ne jamais livrer de source en 404, 410, erreur serveur, soft-404 ou URL inventée.
- Pour une intégration complète, ne jamais enchaîner deux étapes sans export actualisé du parc après l’application de l’étape précédente.
- Les JSON upsert doivent rester bornés : une étape, un lot cohérent, aucune copie massive de l’export complet si seules quelques entités changent.
- Chaque livraison de JSON upsert doit inclure un récap visible avant le fichier : ce qui est ajouté, corrigé, masqué ou conservé, le périmètre exact du lot, un compteur d’avancement traité/total et le reste à traiter avant l’étape suivante.
- La cible de complétude est une qualité `Excellent` sans bloqueur, avec le même degré de rigueur pour chaque parc. La quantité de contenu reste proportionnée à la taille, au statut et aux sources : exigence élevée ne signifie jamais invention ou remplissage artificiel.
- L’audit final compare aussi les corps de paragraphes après retrait des titres et noms d’entités. Un HTML différent uniquement grâce au `<h2>` ne rend pas deux descriptions réellement distinctes ; tout groupe de texte répété doit être réécrit ou justifié factuellement.

## Anciennes guidelines

Les anciennes guidelines séparées JSON, descriptions et articles ont été consolidées dans l’orchestrateur et les fichiers d’étapes. Ne pas recréer de règles parallèles : toute évolution doit enrichir le fichier d’étape concerné.

## Usage attendu

Quand Codex travaille sur le projet, il doit citer ou appliquer ces fichiers comme règles de référence, puis produire des changements cohérents avec le style validé pour AmusementPark.
