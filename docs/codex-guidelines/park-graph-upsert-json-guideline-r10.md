# AmusementPark — Guideline JSON Upsert r10

Version : **r10**  
Date : **2026-06-30**  
Projet : **amusement-parks.fun**  
Portée : Park Graph Upsert, références, images, historiques, dates, horaires et articles rattachés aux événements.

Ce document donne à Codex les règles à appliquer avant de générer, corriger ou enrichir un JSON upsert.

---

## 1. Règle bloquante absolue : vérifier la pertinence avant tout

Avant tout formatage, correction, enrichissement ou génération d’un JSON upsert, vérifier que l’entité demandée est pertinente pour `amusement-parks.fun`.

### Entités pertinentes

Une entité est pertinente si elle correspond clairement à l’un de ces cas :

- parc d’attractions ;
- parc à thème ;
- parc aquatique ;
- parc familial avec attractions mécaniques fixes ;
- parc animalier avec attractions ou parcours nommables ;
- ancien parc disparu documenté ;
- attraction fixe isolée rattachable à un parc ou à un lieu de loisirs stable ;
- constructeur, opérateur, fondateur, propriétaire ou figure liée à l’histoire des parcs ;
- attraction déplacée dont l’historique relie plusieurs parcs ;
- zone, restaurant, boutique, hôtel, parking, entrée, service, spectacle ou animal/enclos clairement rattaché à un parc.

### Entités douteuses ou hors cible

Ne jamais enrichir artificiellement une entité douteuse pour la faire entrer dans le projet.

Si l’entité semble hors cible :

- documenter le doute dans `metadata.notes` ;
- utiliser `adminReviewStatus: "ToReview"` si une décision humaine est nécessaire ;
- masquer avec `isVisible: false` si elle doit être conservée pour traçabilité ;
- utiliser une suppression contrôlée seulement pour un doublon ou une erreur déjà identifiée ;
- ne pas inventer de zones, attractions, photos ou descriptions longues.

### Itinérants et fêtes foraines

Les attractions réellement itinérantes hors parc fixe ne sont pas des parcs à créer. Elles peuvent être pertinentes uniquement si elles sont rattachées à l’histoire d’un parc, installées durablement dans un parc, utiles pour documenter une relocalisation ou liées à un constructeur/événement historique.

---

## 2. Parc majeur : règle absolue après pertinence

Après avoir validé la pertinence, vérifier si le parc est majeur.

Un parc majeur doit recevoir un traitement complet :

- descriptions longues, naturelles et spécifiques ;
- histoire complète si disponible ;
- zones officielles ;
- attractions principales et secondaires ;
- restaurants, boutiques, services, parkings, hôtels si fiables ;
- exploitants, fondateurs, propriétaires, constructeurs ;
- images du parc, logo et parkItems ;
- coordonnées précises si disponibles ;
- horaires/dates vérifiés ;
- articles historiques si le sujet mérite un développement éditorial.

Ne pas traiter un parc majeur comme une simple fiche minimale.

---

## 3. Ne pas se limiter aux coasters

Le site veut référencer tout ce qu’il y a dans les parcs, pas seulement les montagnes russes.

À rechercher et ajouter quand c’est fiable :

- attractions mécaniques ;
- dark rides ;
- parcours scéniques ;
- zones officielles ;
- restaurants ;
- boutiques ;
- hôtels ;
- parkings ;
- entrées et points d’accès ;
- services visiteurs ;
- spectacles ou expériences fixes ;
- animaux, enclos ou espaces animaliers nommables ;
- lieux publics nommables et stables.

Les coasters ne sont qu’une catégorie parmi d’autres.

---

## 4. Intégrité référentielle obligatoire

Avant de livrer un Park Graph Upsert, vérifier que toute clé utilisée est résolue.

Sont bloquants :

- `manufacturerKey` non résolu ;
- `zoneKey` non résolu ;
- `operatorKey` non résolu ;
- `founderKey` non résolu ;
- `ownerKey` non résolu ;
- image rattachée à un owner inexistant ;
- attraction liée à une zone non présente ;
- référence à un constructeur doublonné ou mal nommé.

Tout `items[].attractionDetails.manufacturerKey` non null doit correspondre exactement à un constructeur présent dans le même JSON ou à une identité existante sûre.

---

## 5. Règles sur les descriptions dans le JSON

Les descriptions doivent suivre `description-guidelines-r2.md`.

À respecter impérativement :

- descriptions naturelles ;
- ton éditorial ;
- texte orienté visiteur ;
- pas de langage interne ;
- pas de restrictions dans les descriptions ;
- pas de tarifs dans les descriptions ;
- pas de dates/horaire dans les descriptions narratives ;
- pas de phrases mécaniques ;
- pas de copier-coller entre parcs.

Les conditions d’accès, tailles minimales, restrictions et données techniques vont dans les champs JSON dédiés, pas dans le texte public.

---

## 6. Horaires et dates d’ouverture

Les horaires et dates d’ouverture doivent être traités avec prudence.

Règles :

- vérifier les sources actuelles ;
- ne pas mélanger horaires et tarifs si les tarifs ne sont pas encore implémentés ;
- distinguer période d’ouverture, jours d’ouverture, horaires et fermetures exceptionnelles ;
- ne pas inventer un calendrier complet à partir d’une information partielle ;
- documenter les incertitudes dans `metadata.notes` ;
- localiser les éléments liés aux horaires s’ils ne le sont pas ;
- éviter de transformer une information saisonnière en règle permanente.

Les horaires doivent aller dans la structure prévue, pas dans les descriptions.

---

## 7. Images externes

Chaque image externe ajoutée dans un JSON upsert doit utiliser un lien direct vers le fichier image réel.

Accepté :

- URL directe `.jpg`, `.jpeg`, `.png`, `.webp` ;
- image téléchargeable réelle ;
- source fiable ;
- image correspondant clairement au parc ou parkItem.

Interdit :

- page HTML ;
- page de preview ;
- image de miniature non directe ;
- proxy/optimiseur CDN ;
- URL `cdn-cgi/image` ;
- URL encodée contenant `__im` ;
- URL avec paramètres de transformation comme `?width=` ou `?format=` ;
- image avec watermark, sauf logo officiel ;
- image générique ne montrant pas le bon élément.

Pour le logo du parc, ajouter l’image si disponible et fiable.

Ne pas utiliser de lien CDN interne du site comme source externe.

---

## 8. Visibilité et statuts

Un parkItem confirmé doit rester visible même s’il est fermé, sauf cas particulier de non-pertinence.

Règles :

- `isVisible: true` pour les éléments confirmés, y compris fermés ;
- ajouter un statut de fermeture si l’élément est définitivement fermé ;
- tag ou note `closed-definitively` si prévu par le modèle ;
- conserver les éléments historiques utiles ;
- ne pas supprimer une attraction fermée simplement parce qu’elle n’existe plus physiquement.

La suppression contrôlée est réservée aux doublons, erreurs ou entités hors cible.

---

## 9. Zones

Créer uniquement des zones officielles ou clairement établies.

Ne pas créer de zones à partir de :

- événements saisonniers ;
- catégories de confort ;
- regroupements inventés ;
- localisation approximative ;
- imagination éditoriale.

Un hôtel ne doit pas être placé dans une zone sauf mention officielle ou rattachement explicite.

---

## 10. Coordonnées GPS

Les coordonnées doivent être précises et utiles.

À vérifier :

- coordonnées du parc ;
- entrée principale ;
- parking si pertinent ;
- parkItems si visibles et identifiables ;
- ancien emplacement pour un parc historique fermé.

Ne pas inventer de coordonnées. Si les coordonnées précises d’un parkItem ne sont pas fiables, mieux vaut les laisser absentes que fausses.

---

## 11. Articles rattachés à l’histoire

Si l’histoire d’un parc ou d’une attraction mérite un développement long, créer ou préparer un article selon `articles-guideline-r2-live-sources.md`.

Cas typiques :

- histoire complète d’un parc ;
- ouverture majeure ;
- fermeture ;
- démolition ;
- relocalisation d’une attraction ;
- exploitants successifs ;
- fondateur ou figure historique ;
- captations onride/offride originales ;
- patrimoine de loisirs.

Les articles doivent être sourcés et ne remplacent pas les descriptions.

---

## 12. Fabricants, exploitants, fondateurs

Les références doivent être utiles, propres et non doublonnées.

Règles :

- vérifier les doublons avant d’ajouter un constructeur ;
- ne pas créer `Anton Schwarzkopf` si une fiche `Schwarzkopf` doit être renommée ou fusionnée ;
- rattacher les attractions au bon identifiant existant ;
- écrire des biographies génériques et réutilisables ;
- éviter de centrer une bio constructeur sur un seul parc ;
- ne pas modifier Vekoma si sa bio a été explicitement validée, sauf demande directe.

Les biographies doivent être assez longues pour les acteurs majeurs et traduites si le JSON est complet.

---

## 13. Multilingue

Quand un enrichissement complet est demandé, prévoir les langues :

- `fr-FR` ;
- `en-US` ;
- `es-ES` ;
- `de-DE` ;
- `it-IT` ;
- `nl-NL` ;
- `pt-PT` ;
- `pl-PL`.

Les traductions doivent être naturelles, pas mécaniques.

---

## 14. Mode merge et prudence

En mode `merge`, ne pas effacer les données existantes sauf demande claire.

À préserver :

- images déjà présentes ;
- IDs existants ;
- rattachements fiables ;
- contenu validé ;
- biographies validées ;
- coordonnées existantes si elles sont correctes.

Si une correction doit remplacer une donnée existante, expliquer la raison dans les notes ou livrer un JSON minimal ciblé.

---

## 15. Checklist bloquante avant livraison

Avant de livrer un JSON upsert, vérifier :

- l’entité est pertinente ;
- le parc majeur a reçu un traitement complet si applicable ;
- les parkItems ne se limitent pas aux coasters ;
- toutes les clés référentielles sont résolues ;
- les zones existent et sont officielles ;
- les images sont des fichiers directs ;
- aucune image CDN/proxy/preview/watermark non autorisée ;
- les descriptions respectent la charte r2 ;
- les restrictions ne sont pas dans les descriptions ;
- les horaires/dates sont sourcés et localisés ;
- les tarifs ne sont pas ajoutés si non implémentés ;
- les éléments fermés mais confirmés restent visibles ;
- les doublons constructeurs/opérateurs/fondateurs sont évités ;
- le JSON reste valide ;
- aucun champ obligatoire n’est cassé ;
- les incertitudes sont documentées.

---

## 16. Résumé pour Codex

Codex doit d’abord vérifier la pertinence, puis la qualité du parc, puis l’intégrité référentielle. Il doit enrichir tout le contenu public pertinent du parc, pas seulement les coasters, avec des descriptions naturelles et des données structurées au bon endroit. Un JSON livré doit être utile, fiable, importable et cohérent avec `amusement-parks.fun`.