# QUAL-06 — Tableaux de bord orientés décision

> Date : 29 septembre 2026
>
> Statut : livré
> Portée : `RANK`, `PASS`, `SHARE`, `FIT`, `WATCH`, `TRIP`, `HIST`, `LIVE`

## Résultat métier

L'administration dispose d'un observatoire commun qui répond à trois questions
avant d'ouvrir un panneau spécialisé :

1. quelle décision métier le module doit-il éclairer ?
2. quelles preuves sont réellement disponibles aujourd'hui ?
3. quelle limite interdit de tirer une conclusion trop forte ?

L'écran couvre les huit programmes, puis renvoie vers le panneau opérationnel
existant de chacun. Il ne reproduit aucun indicateur et ne crée pas un neuvième
moteur de calcul. Les équipes gardent ainsi une entrée commune sans construire le
« gigantesque dashboard » exclu par la roadmap.

## Lecture proposée

```text
Question métier
  ├─ niveau de maturité du signal
  │    ├─ décision outillée
  │    ├─ sources séparées
  │    └─ suivi opérationnel
  ├─ preuves actuellement disponibles
  ├─ limite d'interprétation explicite
  └─ canal de mesure
       ├─ agrégats internes minimisés
       ├─ analytics consenti + suivi interne séparés
       └─ diagnostics / métriques techniques
            └─ ouverture du panneau spécialisé
```

Les trois états ne constituent pas une note de qualité :

- **décision outillée** signifie qu'un agrégat métier existant peut soutenir une
  analyse, jamais qu'il décide automatiquement ;
- **sources séparées** rappelle notamment que le funnel consenti de `SHARE` et la
  modération interne ne doivent pas être reliés par un identifiant ;
- **suivi opérationnel** signifie que les signaux disponibles décrivent surtout la
  santé et la fiabilité du système, pas encore le comportement d'une cohorte.

## Routage vers les panneaux existants

| Programme | Question centrale | Panneau spécialisé |
|---|---|---|
| RANK | les classements restent-ils honnêtes avec peu de preuves ? | diagnostic des classements |
| PASS | le passeport conduit-il à une deuxième visite enregistrée ? | pilote de la bêta passeport |
| SHARE | le partage reste-t-il volontaire et révocable ? | modération des partages |
| FIT | la recommandation aboutit-elle malgré les inconnues ? | pilote Park Fit |
| WATCH | l'alerte arrive-t-elle à temps et une seule fois ? | pilote des alertes |
| TRIP | le groupe atteint-il une décision exploitable ? | pilote des voyages |
| HIST | la couverture historique est-elle suffisamment sourcée ? | diagnostic historique |
| LIVE | la donnée reste-t-elle fraîche et fiable ? | opérations live |

## Architecture

```text
AdminProductQualityComponent
  └─ PRODUCT_QUALITY_DASHBOARDS (manifeste statique typé)
       ├─ code programme
       ├─ état de preuve
       ├─ canal
       └─ route du panneau spécialisé

Textes métier et limites
  └─ fichiers i18n source (8 langues)
       └─ génération des dictionnaires Angular
```

Le manifeste ne contient que de la navigation et des classifications fermées. Les
questions, preuves et limites sont localisées. Le composant n'injecte aucun port
de données, n'appelle aucune API et ne dépend d'aucun document MongoDB.

## Confidentialité et performance

- aucun identifiant de membre, visite, parc, partage ou voyage ;
- aucune mesure brute ou valeur exacte réunie sur cette page ;
- aucune corrélation entre Matomo et les données first-party ;
- aucune requête réseau, souscription, cache ou calcul supplémentaire ;
- chargement différé avec le reste des routes d'administration ;
- accès protégé par les gardes d'authentification et d'administration existants.

## Responsive et accessibilité

La grille passe de deux colonnes à une colonne sous `880px`. À `560px`, les
libellés de preuve deviennent verticaux ; à `360px`, la légende et le rappel de
confidentialité s'empilent. Tous les conteneurs peuvent rétrécir, les textes longs
peuvent revenir à la ligne et la page interdit le débordement horizontal.

Les niveaux de preuve sont toujours écrits en clair et ne reposent pas uniquement
sur la couleur. Les cartes sont structurées par titres, listes de définition et
liens explicites.

## Vérification

- couverture exacte et sans doublon des huit programmes ;
- absence d'identifiants et de mesures brutes dans le manifeste ;
- rendu de huit cartes et de huit liens vers les panneaux spécialisés ;
- contrat responsive incluant mobile étroit et paysage de faible hauteur ;
- génération des huit dictionnaires i18n ;
- route et raccourci partagés entre tableau de bord et navigation admin.

## Limites assumées

L'observatoire ne remplace ni Matomo, ni les agrégats first-party, ni les panneaux
opérationnels. Il ne prétend pas non plus que les signaux réservés de `RANK`,
`HIST` ou `LIVE` sont déjà collectés comme analytics produit. Leur activation
nécessitera une question, une finalité et une preuve de minimisation propres.
