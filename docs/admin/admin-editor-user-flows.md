# Admin editor user flows

M25 fige trois parcours cibles pour eviter que le workbench rapide remplace les ecrans complets ou l'import assiste.

## Creation rapide manuelle

Objectif: creer beaucoup de park items minimaux depuis un parc donne.

- Ecran cible: workbench ParkItems.
- Donnees minimales: parc, nom, categorie, type, zone optionnelle, visibilite, statut de review.
- Defauts: item masque, statut `ToReview`, categorie `Attraction`, type `Attraction`, descriptions vides.
- Sortie attendue: l'item existe vite, reste controlable, puis peut etre enrichi ensuite.

## Enrichissement complet

Objectif: completer une fiche avec descriptions, details attraction, conditions d'acces, photos et localisations.

- Ecran cible: edition complete existante du park item.
- Donnees: tous les champs editoriaux et techniques deja supportes.
- Regle: ne pas surcharger le workbench avec des onglets complets.
- Sortie attendue: une fiche riche, validee editorialement.

## Import assiste

Objectif: transformer des donnees structurees ou semi-structurees en entites controlees.

- Ecran cible: import JSON existant, puis futurs assistants CSV/lignes rapides.
- Donnees: payloads partiels, mappings, previews et corrections.
- Regle: l'import garde une etape de validation avant application quand le volume ou l'ambiguite augmentent.
- Sortie attendue: appliquer vite, mais jamais en aveugle.

## Decision d'usage

- Besoin de saisir une zone entiere: utiliser le workbench.
- Besoin de corriger quelques champs simples: utiliser l'inline ou le bulk du workbench quand disponible.
- Besoin de renseigner du contenu riche: ouvrir l'edition complete.
- Besoin de reprendre une source externe: utiliser l'import assiste avec preview.
