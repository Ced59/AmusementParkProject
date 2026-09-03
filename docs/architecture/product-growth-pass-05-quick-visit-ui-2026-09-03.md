# PASS-05 — Création rapide d'une visite depuis un parc ou le profil

Date : 2026-09-03

Roadmap : `docs/roadmaps/product-growth/02-visit-passport-and-ride-log-roadmap.md`

## Résultat

Une personne peut désormais ouvrir le même formulaire de création rapide depuis :

- le bandeau d'une fiche parc, avec le parc déjà sélectionné ;
- son profil, avec une recherche limitée aux parcs publics visibles.

Le formulaire crée exclusivement un brouillon privé via le contrat propriétaire de PASS-04 :

```text
POST /api/me/passport/visits
Idempotency-Key: <identifiant stable de l'opération>
```

Il ne publie aucune donnée, ne modifie aucune note communautaire et n'expose pas les données de la visite dans le HTML public ou `TransferState`.

## Dates fidèles au souvenir

L'utilisateur choisit explicitement l'un des trois niveaux de précision :

```text
jour exact -> année + mois + jour
mois       -> année + mois, aucun jour inventé
année      -> année seule, aucun mois ni jour inventé
```

Le raccourci « aujourd'hui » reste une action volontaire. Le formulaire n'insère jamais la date courante par défaut pour une visite passée. Le marqueur « approximative » reste indépendant de la précision.

Le fuseau IANA est facultatif et n'est jamais déduit du navigateur pour une visite historique : l'utilisateur ne le renseigne que s'il le connaît. Le backend demeure la source d'autorité pour sa validation. Le titre et la note privée sont facultatifs et repliés dans une section secondaire afin de conserver le parcours court.

## Reprise sans doublon

La façade normalise et valide le brouillon avec un mapper pur, puis calcule une empreinte locale du payload envoyé :

```text
brouillon
  -> mapper typé
  -> payload normalisé
  -> empreinte locale
  -> clé d'idempotence stable
  -> POST propriétaire
```

- après une erreur réseau, un nouvel envoi du même payload réutilise la même clé ;
- si un champ du payload change, une nouvelle clé est générée ;
- le double clic est bloqué pendant la requête ;
- les champs restent dans le formulaire après une erreur ou une demande de connexion ;
- le backend rejoue la création initiale si la première réponse a été perdue.

## Frontières d'architecture

- `models/passport` décrit le contrat Angular sans importer de DTO WebAPI ;
- `data-access/passport` possède l'URL HTTP, les headers et la génération cryptographique de l'identifiant d'opération ;
- le port de la feature abstrait les services de visites, de parcs et d'identifiants ;
- le mapper pur possède la normalisation et la validation locale des dates ;
- la façade orchestre authentification, recherche temporisée, état, reprise et messages ;
- le composant possède uniquement le formulaire réactif typé et les interactions de présentation ;
- les vues parc et profil émettent une intention d'ouverture sans appeler l'API.

La feature reste sous `features/profile/passport` conformément à la roadmap. Son composant autonome est chargé uniquement avec les routes paresseuses qui l'utilisent ; aucun code de passeport n'est ajouté au bundle public initial.

## Contrat responsive bloquant

Le dialogue est borné par le viewport dynamique et ne peut pas créer de débordement horizontal :

```text
desktop/tablette : largeur <= 100vw - marges, contenu scrollé dans le dialogue
<= 520 px        : feuille basse, largeur 100vw, date et actions sur une colonne
<= 360 px        : choix de précision sur une colonne
```

Les conteneurs utilisent `min-width: 0`, les textes longs peuvent se replier, les champs et zones de texte sont limités à `100%`, et les paddings respectent les safe areas gauche, droite, haute et basse. Les actions restent accessibles au clavier et au-dessus de la safe area pendant le défilement.

Les tests de régression vérifient explicitement `100dvh`, `100vw`, les safe areas et les ruptures à 520 et 360 pixels. La validation finale après déploiement doit contrôler les largeurs réelles 320, 360 et 390 pixels sur les deux points d'entrée.

## Preuves automatisées

- mapper : année seule, date approximative, année bissextile, jour impossible, textes optionnels et identifiants opaques ;
- data access : route propriétaire et header `Idempotency-Key` ;
- façade : retry identique, changement de payload, session absente et date partielle invalide ;
- composant : bornes viewport, safe areas et repli mobile ;
- intégration : suites existantes du profil et des parcs.

## Limites de la tranche

PASS-05 crée une visite simple et confirme son enregistrement sur place. La timeline, la sélection multiple d'éléments et l'URL privée de détail arriveront dans PASS-08 après les contrats `RideOccurrence` de PASS-06 et PASS-07. Aucune redirection vers une page inexistante n'est simulée dans cette tranche.
