# AmusementPark — Règles de visibilité publique et de qualité SEO

Version : **2026-07-04-r1**  
Projet : **amusement-parks.fun**  
Usage : règle éditoriale et technique pour décider si un parc ou un parkItem peut être visible publiquement.

## Principe directeur

Une entité qui n'est pas prête SEO ne doit pas être visible.

```text
isVisible = true
=> l'entité est publiable
=> elle peut apparaître dans les listes publiques
=> elle peut être liée depuis le site public
=> elle peut entrer dans les sitemaps
=> elle peut être indexée
```

```text
isVisible = false
=> l'entité n'est pas prête pour le public
=> elle ne doit pas apparaître dans les listes publiques
=> elle ne doit pas entrer dans les sitemaps
=> elle ne doit pas générer une page indexable
```

Cette règle remplace l'ancienne logique de test où des parcs pouvaient être rendus visibles simplement parce qu'ils avaient des coordonnées GPS. La qualité des données prime désormais sur le volume de pages publiées.

## Portée

Ces règles s'appliquent à toutes les entités publiques du site :

- parcs ;
- zones quand elles ont une visibilité propre ;
- parkItems ;
- contenus liés, notamment images, descriptions, horaires et historiques.

Un parent non visible rend ses enfants non publiables côté public. Par exemple, un parkItem ne doit pas être visible publiquement si son parc parent ne l'est pas.

## Statuts admin et visibilité

| Situation | `isVisible` | `adminReviewStatus` |
| --- | ---: | --- |
| Entité pertinente, fiable, suffisamment renseignée et prête SEO | `true` | `Validated` |
| Entité pertinente mais pas encore prête SEO | `false` | `ToProcessLater` |
| Entité douteuse qui demande une vérification humaine | `false` | `ToReview` |
| Entité hors cible du site | `false` | `NotRelevant` |

Règle bloquante recommandée :

```text
adminReviewStatus != Validated => isVisible doit rester false
```

Une exception manuelle est possible uniquement pour une opération temporaire d'administration, jamais comme cible SEO de production.

## Règles de visibilité des parcs

### Conditions obligatoires

Un parc ne peut rester visible que s'il vérifie toutes les conditions suivantes :

1. **Pertinence confirmée**  
   Le parc appartient clairement au périmètre du site : parc d'attractions, parc à thème, parc aquatique, parc familial avec attractions fixes, zoo ou parc animalier visitable, ancien parc documenté, ou lieu stable de loisirs avec éléments visiteurs nommables.

2. **Identité fiable**  
   Le nom, le pays, l'emplacement et les coordonnées doivent identifier une entité réelle et distincte. Les doublons, variantes de nom et noms trop génériques doivent être résolus avant publication.

3. **Localisation exploitable**  
   Les coordonnées GPS doivent être valides. Le pays doit être renseigné. La ville, l'adresse ou un repère fiable doivent être ajoutés dès que possible.

4. **Données métier minimales**  
   Le type de parc doit être renseigné. Le statut doit être cohérent. Un site officiel, une source institutionnelle, une source spécialisée ou une source historique fiable doit permettre de confirmer l'existence et la nature du parc.

5. **Contenu visiteur suffisant**  
   La fiche doit contenir une description spécifique et utile, ainsi que des contenus visiteurs nommables quand ils sont disponibles : attractions, zones, restaurants, boutiques, spectacles, hôtels, services, animaux, enclos, parkings ou accès.

### Parc vivant classique

Un parc vivant classique doit rester invisible tant qu'il n'a pas au minimum :

- une localisation fiable ;
- un type renseigné ;
- un statut cohérent ;
- une description spécifique ;
- au moins une source fiable ;
- idéalement 1 à 3 contenus visiteurs fiables, ou davantage pour les grands parcs.

Un parc avec seulement `nom + pays + coordonnées` doit être masqué en `ToProcessLater`.

### Petit parc local

Un petit parc local peut être visible avec une fiche plus courte, mais seulement si elle est réellement utile :

- identité claire ;
- coordonnées fiables ;
- description spécifique au lieu ;
- contenus visiteurs minimum confirmés ;
- absence de doute sur la pertinence.

Ne pas publier un petit parc local avec une description générique ou un inventaire vide.

### Parc majeur

Un parc majeur peut être prioritaire, mais il ne doit pas être visible s'il est vide.

```text
Grand parc pauvre = ToProcessLater + invisible
Grand parc enrichi = Validated + visible
```

La liste des grands parcs sert à prioriser l'enrichissement, pas à contourner les critères de qualité. Avant publication, un parc majeur doit au moins avoir :

- description spécifique ;
- pays, ville ou emplacement fiable ;
- coordonnées ;
- type ;
- statut ;
- quelques parkItems principaux ;
- title/meta exploitables via le front ;
- idéalement logo ou image principale.

### Parc historique fermé

Un parc historique fermé peut être visible si la fiche est éditorialement utile. Il faut au minimum :

- `status: "ClosedDefinitively"` ;
- ancien emplacement fiable ;
- période d'activité ou dates connues avec précision honnête ;
- description historique spécifique ;
- sources historiques fiables ;
- événements ou contexte de fermeture si disponibles.

Un ancien parc réduit à un nom et un pays doit être `ToProcessLater` et invisible.

## Règles de visibilité des parkItems

Un parkItem visible doit être publiable seul et cohérent avec son parc parent.

### Conditions obligatoires

Un parkItem ne peut être visible que si :

- son parc parent est visible ;
- son identité est fiable ;
- son nom est spécifique ;
- son `category` et son `type` sont cohérents ;
- son statut est cohérent ;
- il n'est pas un doublon ;
- il n'est pas une attraction itinérante hors contexte durable ;
- il possède une description spécifique ou des données structurées suffisantes ;
- ses rattachements (`zoneId`, constructeur, coordonnées, conditions d'accès) sont fiables quand ils sont renseignés.

### ParkItems à masquer

Masquer en `ToProcessLater` ou `ToReview` les items qui ressemblent à des placeholders :

```text
Roller Coaster
Mini Train
Restaurant
Parking
Playground
Shop
Food Court
Carousel
Ferris Wheel
```

Ces noms peuvent devenir visibles seulement après identification précise : nom officiel, localisation, catégorie, statut et rattachement correct.

### ParkItems fermés

Un parkItem fermé peut rester visible s'il est confirmé et utile à l'histoire ou à la compréhension du parc. Il doit alors avoir un statut cohérent, des dates ou une période fiable si possible, et une description qui explique sa place dans le parc sans survente artificielle.

## Règles d'exclusion immédiate

### Hors cible

Passer en `NotRelevant` et `isVisible: false` :

- forains itinérants ;
- exploitants sans parc fixe ;
- personnes physiques ;
- familles ou entreprises listées comme parcs par erreur ;
- foires temporaires ;
- festivals ;
- événements saisonniers ;
- centres commerciaux sans parc clairement identifié ;
- hôtels ou resorts sans attraction fixe nommable ;
- stations de ski sans attraction estivale ou attraction de loisirs pertinente ;
- simples playgrounds publics ;
- lieux génériques impossibles à identifier.

### Pertinent mais trop pauvre

Passer en `ToProcessLater` et `isVisible: false` :

- vrai parc sans type ;
- vrai parc sans description ;
- vrai parc sans source ;
- vrai parc sans contenu visiteur ;
- parc avec seulement coordonnées GPS ;
- ancien parc intéressant mais sans histoire ;
- parc majeur connu mais fiche encore vide.

### Doute ou doublon

Passer en `ToReview` ou `ToProcessLater` et `isVisible: false` :

- variantes de nom ;
- anciennes appellations ;
- parcs déplacés ;
- doublons ville/pays ;
- noms traduits séparés ;
- identités génériques non résolues.

Exemples de noms à traiter avec prudence tant que l'identité n'est pas résolue :

```text
Dream Land
Family Park
City Park
Dino Parc
Fantasy Island
Children's Park
Central Park
Fun Park
```

## Score qualité indicatif

Le score ne remplace pas la validation humaine, mais il aide à automatiser les gros lots.

| Critère parc | Points |
| --- | ---: |
| Pays renseigné | +1 |
| Coordonnées GPS valides | +2 |
| Type renseigné | +2 |
| Statut cohérent | +1 |
| Ville ou adresse | +1 |
| Site officiel ou source fiable | +2 |
| Description spécifique | +3 |
| Au moins 3 parkItems visibles | +3 |
| Image ou logo | +1 |
| `adminReviewStatus: "Validated"` | +3 |

Décision recommandée :

| Score | Action |
| ---: | --- |
| 0 à 7 | Invisible |
| 8 à 11 | Invisible + `ToProcessLater`, sauf validation manuelle documentée |
| 12+ | Visible possible si aucun blocage de pertinence |
| 14+ | Visible recommandé |

Règle bloquante : un score élevé ne doit jamais rendre visible une entité `NotRelevant`, douteuse ou non validée.

## Sitemaps et indexation

La règle sitemap doit rester simple :

```text
Sitemap = uniquement les entités visibles
```

Ne pas ajouter au sitemap :

- parcs invisibles ;
- parkItems invisibles ;
- entités `ToReview` ;
- entités `ToProcessLater` ;
- entités `NotRelevant` ;
- pages sans description spécifique ;
- pages sans parent visible.

Une page publique correspondant à un parc ou parkItem invisible doit répondre comme une page non publique, de préférence en `404` côté public. L'entité reste consultable côté admin.

## Liens internes publics

Les listes, cartes, blocs de recommandations, liens contextuels et pages de détail ne doivent lier que des entités visibles.

Ne pas créer de liens publics massifs vers des fiches en dette de qualité. Un lien interne public vers une entité pauvre augmente le risque de crawl inutile et de contenu faible.

## Bulk JSON upsert

En mode bulk, respecter strictement le contrat d'export : ne modifier que les propriétés déjà présentes dans le JSON exporté.

Pour un bulk de nettoyage SEO, demander au minimum les sections :

```text
ParkBasics
ParkAdministration
ParkLocation
ParkAudience
```

Pour une décision plus fiable, demander aussi les compteurs ou sections équivalentes :

```text
ParkCounts
ParkSeo
```

Les compteurs utiles sont notamment :

- nombre de parkItems ;
- nombre de parkItems visibles ;
- nombre d'images ;
- présence d'une description ;
- présence d'horaires ;
- présence d'un site officiel ;
- statut admin ;
- visibilité actuelle.

Si une propriété nécessaire n'est pas présente dans l'export bulk, demander un nouvel export plutôt que l'ajouter manuellement.

## Workflow de nettoyage recommandé

### Passe 1 — Hors cible

Mettre en `NotRelevant` et invisible les entités hors périmètre : forains, personnes, exploitants, foires, centres commerciaux sans parc clair, hôtels sans attraction fixe, doublons hors usage.

### Passe 2 — Pertinent mais pas prêt

Mettre en `ToProcessLater` et invisible les vrais parcs ou parkItems qui doivent être enrichis avant publication.

### Passe 3 — Validation SEO

Garder ou remettre visible uniquement les entités `Validated` qui apportent une vraie valeur visiteur.

### Passe 4 — Audit sitemap

Vérifier que les sitemaps ne contiennent que des entités visibles et validées.

## Règle finale

```text
Une entité non prête SEO ne doit pas être visible.
Visible = validée + pertinente + utile + suffisamment renseignée.
Tout le reste est invisible : ToProcessLater, ToReview ou NotRelevant selon le cas.
```
