# ParkDetail Light — PD0 validé

**Projet :** AmusementPark
**Document source de référence :** décisions produit / UX / SEO / performance pour l'allègement de `ParkDetail`
**Statut :** validé
**Date de validation :** 2026-06-11
**Portée :** cadrage uniquement, à lire avant toute demande d'implémentation liée à `ParkDetail`, aux sous-pages parc, à la galerie images parc ou à la future déclinaison `ParkItemDetail`.

---

## 1. Règle obligatoire pour toute implémentation future

Avant toute modification de code concernant `ParkDetail`, les routes publiques de parc, la galerie images de parc, la carte de parc, la page items de parc ou le futur allègement `ParkItemDetail`, ce document doit être relu et appliqué strictement.

Toute proposition d'implémentation doit respecter ces principes :

1. **ParkDetail devient une page de synthèse légère**, pas une page exhaustive.
2. **Un composant retiré visuellement doit aussi disparaître du chargement initial**, des appels API initiaux et du bundle initial quand c'est possible.
3. **Le SSR doit servir le contenu éditorial utile**, pas toutes les expériences interactives.
4. **Les nouvelles routes ne doivent pas gonfler le crawl sans valeur SEO réelle.**
5. **Les expériences lourdes doivent être demandées volontairement par l'utilisateur**, via des sous-pages dédiées.
6. **La refonte ParkDetail doit préparer un pattern réutilisable pour ParkItemDetail.**

---

## 2. Contexte et problème à résoudre

La page `ParkDetail` actuelle a déjà été partiellement protégée côté SSR par une logique de données minimales publiques. Cependant, côté navigateur, elle peut encore déclencher une cascade d'appels coûteux :

- parc principal ;
- parcs proches ;
- explorer / données enrichies ;
- zones ;
- items complets ;
- images du parc ;
- tags images admin ;
- images par item visible.

Cette logique est trop ambitieuse pour une page détail principale, surtout dans un contexte SEO/SSR où les robots peuvent multiplier les accès.

L'objectif de PD0 est donc de figer une nouvelle intention produit : `ParkDetail` doit être une landing page rapide, claire, indexable et utile. La localisation générale du parc reste visible avec une carte Leaflet simple à un seul point. En revanche, la carte détaillée des items, la galerie et l'exploration complète vivent dans des pages spécialisées.

---

## 3. Intention produit validée pour ParkDetail

`ParkDetail` doit répondre à la question suivante :

> Quel est ce parc, où est-il, qu'est-ce qu'on y trouve, et où puis-je aller ensuite ?

`ParkDetail` ne doit plus chercher à répondre directement à :

> Montre-moi immédiatement toute la carte, toutes les photos, tous les items, toutes les zones et toutes les données interactives.

La page doit devenir :

- une page de synthèse ;
- une page SEO forte ;
- une page mobile-first ;
- une page rapide à rendre en SSR ;
- une page de navigation vers des expériences spécialisées.

---

## 4. Contenu validé sur ParkDetail

Les éléments suivants doivent rester sur la page principale :

| Bloc | Décision | Commentaire |
|---|---:|---|
| Nom du parc | Conserver | Élément principal du H1. |
| Localisation | Conserver | Ville, pays, adresse si disponible, avec carte Leaflet générale à un seul marqueur si coordonnées disponibles. |
| Statut | Conserver | Ouvert / fermé définitivement, etc. |
| Type(s) de parc | Conserver | Utile pour compréhension et SEO. |
| Image principale unique | Conserver | Une seule image LCP potentielle. |
| Description longue localisée | Conserver | Cœur éditorial et SEO. |
| Statistiques agrégées | Conserver | Remplace les listes lourdes. |
| Informations pratiques légères | Conserver | Site officiel, fondateur, exploitant si disponibles. |
| CTA vers sous-pages | Conserver | Navigation vers items, images, carte. |
| Breadcrumb | Conserver | SEO et navigation. |
| Canonical / hreflang / metas | Conserver | Indispensable SEO. |

---

## 5. Blocs à déplacer hors de ParkDetail

Les blocs suivants ne doivent plus être chargés automatiquement sur la page principale :

| Bloc | Nouvelle destination | Raison |
|---|---|---|
| Carte détaillée des items | Page `/map` | Leaflet avec markers d'items et données géographiques détaillées hors chargement initial. La carte générale du parc à un seul point reste autorisée sur `ParkDetail`. |
| Galerie complète | Page `/images` | Expérience riche, paginée et indexable, mais séparée. |
| Liste complète des items | Page `/items` | Exploration dédiée avec filtres/pagination. |
| Images par item | Page `/items` ou pages item | Évite le fan-out API. |
| Tags images admin | Aucune utilisation sur ParkDetail public | Donnée non pertinente pour la page principale. |
| Nearby parks | Éventuel module lazy plus tard | Non essentiel au rendu initial. |
| Zones détaillées | Page/section spécialisée plus tard | À éviter sur ParkDetail tant que ce n'est pas éditorialement fort. |

---

## 6. Blocs à supprimer temporairement de ParkDetail

Les blocs suivants doivent être retirés tant qu'ils ne disposent pas de vraies données exploitables :

- météo `coming soon` ;
- horaires `coming soon` ;
- prix `coming soon` ;
- tout placeholder fonctionnel qui occupe de l'espace sans valeur utilisateur immédiate.

Ils pourront revenir plus tard si de vraies données fiables existent et si leur coût de chargement est maîtrisé.

---

## 7. Règles sur les previews d'items

Décision validée : **pas de preview item dans la première passe ParkDetail Light**.

Raison : même une preview limitée peut réintroduire trop vite les dépendances suivantes :

- récupération d'items ;
- récupération d'images par item ;
- mapping complexe ;
- payload plus lourd ;
- envie de multiplier les cartes visuelles.

Une preview pourra être réévaluée plus tard, uniquement après mesure, avec un contrat dédié déjà léger et sans appels image par item.

---

## 8. Parcours utilisateur cible

La page `ParkDetail` doit proposer peu d'actions, mais très claires.

CTA principaux validés :

1. **Voir les attractions et lieux du parc** → route `/items`.
2. **Voir les photos** → route `/images`.
3. **Voir la carte** → route `/map`.
4. **Site officiel** → lien externe si disponible.
5. **Retour aux parcs** → action secondaire.

Ordre de page recommandé :

1. Hero : image principale, nom, localisation, statut.
2. Bandeau de statistiques.
3. Description longue localisée.
4. Bloc de navigation “Explorer ce parc”.
5. Informations pratiques légères.
6. Liens secondaires / footer de page.

---

## 9. Budget d'appels API cible pour ParkDetail

### 9.1 Objectif SSR

Le rendu serveur de `ParkDetail` doit viser :

```text
1 appel principal public ParkDetailSummary
0 appel carte détaillée/items
0 appel galerie complète
0 appel liste complète items
0 appel image par item
0 appel tags admin
0 appel nearby parks
```

### 9.2 Objectif navigateur

Après hydratation :

```text
0 double appel immédiat si TransferState est disponible
sinon 1 seul appel ParkDetailSummary est acceptable temporairement
aucun chargement automatique des expériences lourdes
```

### 9.3 Règle fondamentale

Masquer un composant dans le template ne suffit pas. Si la donnée n'est plus affichée sur `ParkDetail`, elle ne doit plus être demandée par la facade principale.

---

## 10. Endpoint cible ParkDetailSummary

Une prochaine phase devra créer un contrat résumé public pour éviter de continuer à consommer les anciens graphes lourds.

Route recommandée :

```text
GET /parks/{id}/detail-summary
```

Le nom pourra être ajusté selon les conventions existantes du projet, mais l'intention est non négociable : un seul endpoint principal, cacheable, dédié à `ParkDetailLight`.

### 10.1 Données attendues

Le contrat cible doit rester léger et ne pas contenir de listes massives.

Exemple conceptuel :

```ts
ParkDetailSummary {
  id;
  slug;
  name;
  localizedDescription;
  status;
  typeLabels;
  countryCode;
  countryName?;
  city?;
  address?;
  latitude?;
  longitude?;
  websiteUrl?;
  founderSummary?;
  operatorSummary?;
  mainImage?;
  stats: {
    totalItems;
    attractionCount;
    restaurantCount;
    showCount;
    shopCount;
    zoneCount;
    permanentlyClosedItemCount?;
  };
  links: {
    items;
    images;
    map;
    officialWebsite?;
  };
}
```

### 10.2 Données explicitement interdites dans ce contrat

Sauf décision ultérieure documentée, `ParkDetailSummary` ne doit pas contenir :

- liste complète des items ;
- liste complète des images ;
- tags admin ;
- images par item ;
- données complètes de carte ;
- nearby parks ;
- payloads destinés aux pages spécialisées.

---

## 11. Règles SEO et indexation validées

### 11.1 Routes principales

| Route | SSR | Indexation | Sitemap | Décision |
|---|---:|---:|---:|---|
| `/:lang/park/:id/:slug` | Oui | Oui | Oui | Page principale forte. |
| `/:lang/park/:id/:slug/item/:itemId/:itemSlug` | Oui | Oui | Oui | Page item SEO forte. |
| `/:lang/park/:id/:slug/items` | À arbitrer plus tard | Noindex au départ | Non au départ | Page UX d'exploration au départ. |
| `/:lang/park/:id/:slug/images` | Oui minimal ou SSR maîtrisé | Oui | Oui, section séparée | Page images indexable. |
| `/:lang/park/:id/:slug/map` | Non ou SSR minimal | Noindex | Non | Page UX, faible valeur SEO brute. |
| Filtres combinatoires | Non | Noindex | Non | Évite explosion crawl. |

---

## 12. Décision spécifique : page images indexable

La page `/images` du parc est validée comme **indexable**.

Cette décision remplace l'hypothèse initiale qui prévoyait de la mettre en `noindex` au départ.

### 12.1 Condition obligatoire

La page images ne doit pas être une galerie brute. Elle doit être une vraie page SEO éditoriale, paginée, localisée et optimisée.

Elle doit prévoir :

- un H1 dédié, par exemple `Photos de Bellewaerde` ;
- une introduction textuelle localisée ;
- une pagination ;
- du lazy loading ;
- des filtres basés sur les métadonnées images ;
- un tri pertinent, notamment par date de captation si disponible ;
- des `alt` descriptifs ;
- des légendes/captions si possible ;
- des liens contextuels vers le parc, les zones ou les items concernés ;
- un canonical clair ;
- une section sitemap dédiée ou un sitemap images ultérieur ;
- des données SSR limitées à la première page ou au contenu initial utile.

### 12.2 Filtres images

Les filtres basés sur les métadonnées sont souhaités, mais ils ne doivent pas créer une infinité de pages indexables.

Règle :

- la page principale `/images` est indexable ;
- les pages paginées peuvent être indexables si elles sont propres et stables ;
- les combinaisons de filtres doivent être `noindex` par défaut tant qu'elles ne sont pas validées comme pages SEO autonomes ;
- aucune combinaison de filtres ne doit entrer automatiquement dans le sitemap.

### 12.3 Carte des photos

L'idée de localiser les photos sur une carte est validée comme piste future, mais hors PD0.

Quand elle sera implémentée, elle devra respecter la même règle que la carte parc : chargement volontaire, bundle séparé, pas d'impact sur `ParkDetail`.

---

## 13. Page map

La route `/map` doit être créée ou renforcée comme expérience cartographique dédiée.

Décision :

- accessible aux visiteurs ;
- pas dans le sitemap au départ ;
- `noindex` au départ ;
- chargement lazy du module carte ;
- données géographiques ciblées ;
- aucun impact sur le bundle initial de `ParkDetail`.

---

## 14. Page items

La route `/items` est une page d'exploration utilisateur.

Décision PD0 : **prudente au départ**.

- accessible aux visiteurs ;
- noindex temporaire ;
- hors sitemap temporairement ;
- pourra devenir indexable plus tard si elle devient une vraie page SEO : intro textuelle, pagination stable, canonical, metas dédiées, contenu suffisamment autonome.

---

## 15. Règles SSR

`ParkDetailLight` doit rester SSR complet :

- H1 ;
- description ;
- stats ;
- image principale ;
- canonical ;
- hreflang ;
- metas ;
- JSON-LD si pertinent.

Les pages lourdes doivent éviter le SSR exhaustif :

- `/map` : CSR ou SSR minimal ;
- `/images` : SSR maîtrisé, limité à la première page et au contenu SEO initial ;
- `/items` : SSR à réévaluer lors de sa transformation SEO.

---

## 16. Règles de sitemap

Le sitemap doit rester sélectif.

À inclure :

- ParkDetail ;
- ParkItemDetail ;
- ParkImages page principale ;
- pages images paginées seulement si elles sont stables, utiles et correctement canoniques.

À exclure :

- carte ;
- filtres combinatoires ;
- pages techniques ;
- pages avec contenu faible ;
- pages qui déclenchent une expérience lourde sans valeur SEO autonome.

Une section dédiée est recommandée :

```text
sitemap-static
sitemap-parks
sitemap-park-items
sitemap-park-images
```

Un sitemap images spécifique pourra être ajouté plus tard si le pipeline image et les métadonnées sont suffisamment propres.

---

## 17. Règles de cache et performance

Les endpoints publics stables devront être compatibles avec un cache court/prudent.

Candidats au cache :

- ParkDetailSummary ;
- stats parc ;
- image principale ;
- galerie paginée ;
- map markers ;
- sitemaps.

Règle : les robots ne doivent pas forcer le recalcul complet de données identiques à chaque hit.

---

## 18. Accessibilité et sémantique

La nouvelle page `ParkDetailLight` doit conserver :

- un seul H1 ;
- des sections nommées ;
- des liens compréhensibles hors contexte ;
- des textes alternatifs pour les images ;
- des états de chargement accessibles ;
- une hiérarchie de titres claire ;
- une expérience mobile-first.

---

## 19. Pattern réutilisable pour ParkItemDetail

La refonte de ParkDetail doit préparer le modèle suivant :

```text
DetailPage = résumé SEO fort + image principale + infos essentielles + CTA
Sous-pages = galerie, carte, exploration complète, données avancées
```

Pour la future phase `ParkItemDetailLight`, il faudra appliquer la même logique :

À garder sur la fiche item :

- nom ;
- parc parent ;
- catégorie ;
- statut ;
- description ;
- specs utiles ;
- conditions d'accès ;
- image principale ;
- CTA vers galerie/carte/parc.

À déplacer :

- galerie complète ;
- carte complète ;
- données parent lourdes ;
- related items si coûteux ;
- appels images multiples.

Contrat futur probable :

```text
GET /park-items/{id}/detail-summary
```

---

## 20. Non-objectifs de PD0

PD0 ne demande pas encore d'implémenter :

- la page images complète ;
- la carte des photos ;
- les filtres images avancés ;
- le tri par date de captation ;
- le sitemap images complet ;
- la page items SEO indexable ;
- ParkItemDetailLight ;
- le cache complet ;
- les mesures Lighthouse finales.

PD0 valide uniquement le cadre et les décisions à respecter.

---

## 21. Checklist obligatoire avant toute implémentation ParkDetail Light

Avant de coder, vérifier :

- [ ] La modification respecte la règle “ParkDetail = synthèse légère”.
- [ ] Aucun composant lourd n'est seulement masqué sans suppression des appels associés.
- [ ] Les appels API initiaux restent limités.
- [ ] La carte détaillée des items n'est pas chargée sur ParkDetail.
- [ ] La carte Leaflet générale du parc reste présente si les coordonnées du parc sont disponibles.
- [ ] La galerie complète n'est pas chargée sur ParkDetail.
- [ ] Les images par item ne sont pas chargées sur ParkDetail.
- [ ] Les tags admin ne sont pas demandés par une page publique.
- [ ] Les blocs `coming soon` inutiles ne reviennent pas.
- [ ] La page images reste indexable mais paginée et maîtrisée.
- [ ] Les filtres combinatoires images ne sont pas indexables automatiquement.
- [ ] La page map reste noindex et hors sitemap.
- [ ] La page items reste noindex au départ, sauf décision ultérieure documentée.
- [ ] Les nouvelles routes n'augmentent pas le crawl sans valeur SEO autonome.
- [ ] Le pattern reste réutilisable pour ParkItemDetail.

---

## 22. Décision finale PD0

PD0 est validé avec les décisions suivantes :

1. `ParkDetail` devient une landing page légère, éditoriale et indexable.
2. Une seule image principale est chargée sur `ParkDetail`.
3. La carte Leaflet générale du parc reste sur `ParkDetail` avec un seul marqueur.
4. La carte complète/détaillée des items sort vers `/map`.
5. La galerie complète sort vers `/images`.
6. La page `/images` est indexable, sous condition de pagination, lazy loading, contenu SEO et gestion stricte des filtres.
7. La liste complète des items sort vers `/items`.
8. `/items` reste noindex temporairement.
9. `/map` reste noindex et hors sitemap.
10. Les blocs `coming soon` sans données réelles sont supprimés de ParkDetail.
11. Les données lourdes ne doivent plus être demandées par la facade principale ParkDetail.
12. Un endpoint `ParkDetailSummary` devra être créé pour porter la refonte proprement.
13. Le pattern doit préparer l'allègement futur de `ParkItemDetail`.
