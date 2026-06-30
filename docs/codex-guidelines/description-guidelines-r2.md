# AmusementPark — Charte obligatoire des descriptions publiques

Version : **2026-06-29-r2**  
Statut : **obligatoire**  
Portée : descriptions publiques des parcs, zones, attractions, animaux, restaurants, boutiques, parkings, services, fondateurs, exploitants et constructeurs.

Cette charte fixe le style attendu pour toutes les descriptions visibles par les visiteurs sur `amusement-parks.fun`.

---

## 1. Objectif

Une description doit aider un visiteur réel à comprendre :

- ce qu’il va voir ;
- l’ambiance du lieu ou de l’attraction ;
- le type d’expérience proposée ;
- ce qui donne une identité propre au parc, à la zone, à l’attraction ou au service ;
- pourquoi ce lieu mérite d’être remarqué.

La description n’est jamais une note interne, une justification SEO, une explication de base de données ou une fiche technique déguisée.

---

## 2. Règle d’or

Avant de valider une description, se demander :

> Est-ce qu’un visiteur du parc pourrait lire ce texte sur son téléphone et le trouver utile, naturel et agréable ?

Si la réponse est non, la description doit être réécrite.

---

## 3. Priorités de rédaction

En cas de doute, appliquer cet ordre :

1. Exactitude factuelle.
2. Utilité visiteur.
3. Clarté mobile.
4. Ton naturel.
5. SEO discret.
6. Structure HTML propre.
7. Cohérence multilingue.

---

## 4. Style validé

Le style validé est celui demandé pour Le Fleury le 2026-06-29 :

- naturel ;
- éditorial ;
- spécifique au lieu ;
- agréable à lire ;
- orienté visiteur ;
- non mécanique ;
- non cloné d’un parc à l’autre ;
- sans formulations de remplissage.

La description doit donner envie de lire, pas simplement remplir un champ.

---

## 5. Formulations interdites

Ne jamais utiliser de formules mécaniques ou artificielles comme :

- “ce que ça apporte à la journée” ;
- “ce que ça apporte au groupe” ;
- “comment l’intégrer dans la journée” ;
- “quand cela devient utile” ;
- “pour une fiche visiteur” ;
- “à référencer” ;
- “contenu public” ;
- “élément de parc” ;
- “dans la base” ;
- “upsert” ;
- “SEO” dans un texte public.

Éviter aussi les introductions répétitives du type :

- “Situé dans…” répété sur toutes les attractions ;
- “Cette attraction propose…” répété mécaniquement ;
- “Idéal pour…” répété comme un gabarit ;
- “Cette zone permet…” répété comme une notice.

---

## 6. Ce qui ne doit pas aller dans une description

Ne pas mettre dans les descriptions publiques :

- conditions d’accès ;
- tailles minimales ;
- restrictions ;
- âge conseillé sous forme réglementaire ;
- informations tarifaires ;
- horaires ;
- dates d’ouverture ;
- détails techniques bruts ;
- coordonnées GPS ;
- notes d’administration ;
- avertissements de complétude.

Ces informations doivent aller dans les champs JSON prévus, pas dans le texte narratif.

---

## 7. Longueur et structure

### Parc majeur

Pour un parc majeur, la description doit être longue, riche et structurée. Elle peut inclure :

- une introduction immersive ;
- une présentation de l’identité du parc ;
- les grandes familles d’expériences ;
- l’ambiance générale ;
- l’intérêt pour différents profils de visiteurs ;
- une conclusion éditoriale naturelle.

Elle doit rester lisible sur mobile avec des paragraphes courts.

### Parc mineur ou local

Pour un parc plus petit, la description doit rester spécifique et utile, sans inventer une importance excessive. Elle peut être plus courte mais ne doit jamais être générique.

### ParkItem

Pour une attraction, un restaurant, une boutique, un service ou une zone, la description doit expliquer ce que le visiteur va réellement rencontrer, sans recopier les champs techniques.

---

## 8. Descriptions de parcs

Une bonne description de parc doit :

- présenter le lieu avec son identité propre ;
- évoquer l’ambiance ;
- aider à comprendre le type de visite ;
- rester factuelle ;
- éviter les superlatifs non sourcés ;
- ne pas masquer les incertitudes ;
- ne pas promettre une expérience non vérifiée.

Elle ne doit pas ressembler à une fiche administrative.

---

## 9. Descriptions de zones

Une zone doit être décrite comme un espace vécu :

- ambiance ;
- décor ;
- rôle dans la visite ;
- attractions ou points d’intérêt qui lui donnent son identité ;
- logique de circulation si elle est utile au visiteur.

Ne pas inventer de zone non officielle ou non clairement identifiable.

---

## 10. Descriptions d’attractions

Une attraction doit être décrite par l’expérience ressentie et ce que le visiteur observe.

On peut mentionner :

- le type d’attraction ;
- le rythme ;
- l’ambiance ;
- la place dans le parc ;
- les sensations générales si elles sont fiables ;
- l’aspect familial, contemplatif ou intense si c’est vérifié.

Ne pas intégrer les restrictions d’accès dans la description.

---

## 11. Restaurants, boutiques et services

Les restaurants, boutiques et services doivent être décrits naturellement, sans survente.

Exemples d’axes utiles :

- type de lieu ;
- ambiance ;
- utilité dans la visite ;
- positionnement dans le parc ;
- particularité visible ou nommable.

Éviter les textes vides comme “un service pratique pour les visiteurs”.

---

## 12. Fondateurs, exploitants, constructeurs

Les biographies doivent être :

- factuelles ;
- réutilisables ;
- non centrées artificiellement sur le parc courant ;
- suffisamment longues pour les acteurs majeurs ;
- prudentes si les sources sont limitées.

Pour les constructeurs, la bio peut évoquer :

- origine ;
- période d’activité ;
- spécialités ;
- modèles marquants ;
- influence dans l’industrie ;
- exemples connus.

Ne pas modifier une bio validée explicitement comme celle de Vekoma sauf demande directe.

---

## 13. Multilingue

Langues attendues quand la donnée est enrichie complètement :

- français ;
- anglais ;
- espagnol ;
- allemand ;
- italien ;
- néerlandais ;
- portugais ;
- polonais.

La traduction doit conserver l’intention, mais ne doit pas être mot à mot si cela donne un texte rigide.

---

## 14. HTML autorisé

Les descriptions longues peuvent utiliser du HTML simple :

- `<p>` ;
- `<h2>` ;
- `<h3>` ;
- `<ul>` ;
- `<li>` ;
- `<strong>` si utile.

Le HTML doit rester propre, lisible et compatible mobile.

Ne pas utiliser de structure lourde ou décorative dans les champs de description.

---

## 15. Exactitude et prudence

Ne jamais présenter comme certain :

- une date non vérifiée ;
- un constructeur incertain ;
- une zone non officielle ;
- une attraction non confirmée ;
- une ouverture ou fermeture non sourcée ;
- une photo qui ne correspond pas clairement au bon lieu.

Si une information est incertaine, la documenter dans `metadata.notes` ou laisser la donnée absente plutôt que l’inventer.

---

## 16. Anti-duplication SEO

Les descriptions ne doivent jamais être copiées-collées d’un parc à l’autre.

Même pour deux attractions de même type, varier :

- angle d’approche ;
- vocabulaire ;
- rythme ;
- détails spécifiques ;
- contexte dans le parc.

Le SEO doit rester discret et naturel.

---

## 17. Checklist avant validation

Avant de livrer une description, vérifier :

- le texte est naturel ;
- le texte est spécifique au lieu ;
- aucune restriction ou donnée technique ne pollue la description ;
- aucune formule interdite n’est présente ;
- la description ne ressemble pas à une fiche interne ;
- le texte donne envie de lire ;
- les informations factuelles sont fiables ;
- les traductions gardent la même intention ;
- le HTML est simple et propre ;
- le contenu n’est pas dupliqué d’un autre parc.

---

## 18. Résumé pour Codex

Quand Codex réécrit ou crée une description AmusementPark, il doit produire un texte public, humain, précis, agréable à lire et adapté aux visiteurs. Il ne doit pas écrire comme une base de données, ne doit pas mélanger les restrictions avec la narration, et doit toujours privilégier la spécificité du lieu.