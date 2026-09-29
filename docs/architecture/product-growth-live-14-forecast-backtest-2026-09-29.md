# LIVE-14 — Étude reproductible des prévisions d’attente

## Finalité métier

LIVE-14 répond à une question avant toute promesse au visiteur : une méthode qui
connaît le jour de la semaine fait-elle réellement mieux qu’une référence très
simple ? Le produit peut désormais répondre avec des mesures et recommander
explicitement l’abandon si la réponse est non.

Le jalon ne publie aucune attente future. Il ajoute au pilotage administrateur
un test par attraction, avec trois verdicts possibles :

- `Données insuffisantes` : il faut continuer à observer ;
- `Abandon recommandé` : la méthode n’apporte pas assez de valeur ou son
  incertitude se dégrade ;
- `Éligible à un pilote` : les preuves franchissent les seuils, sans autoriser à
  elles seules une publication.

## Données évaluées

Le calcul est borné à une attraction et à une période d’évaluation comprise
entre 14 et 180 jours. La période par défaut couvre 90 jours. Le serveur charge
en plus 84 jours antérieurs uniquement pour entraîner chaque pli temporel.

Avant le calcul :

1. la source configurée doit toujours autoriser le stockage historique et
   fournir sa politique de rétention courante ;
2. le parc et l’attraction sont résolus dans les référentiels applicatifs ;
3. seuls les identifiants externes encore rattachés par un mapping humain
   vérifié sont conservés ;
4. les doublons au même instant gardent la réception la plus récente ;
5. les buckets tronqués, états non opérationnels et attentes `Standby` absentes
   sont exclus ;
6. les observations valides d’une même heure locale deviennent un seul point :
   leur médiane. Cette agrégation évite de compter douze polls très proches
   comme douze preuves indépendantes.

Zéro minute reste une vraie valeur. Une lacune ne devient jamais zéro.

## Méthodes comparées

Pour chaque point horaire de la période d’évaluation, le calcul n’utilise que
les 84 jours qui le précèdent :

| Élément | Méthode versionnée |
|---|---|
| Référence | médiane des points antérieurs de la même heure, tous jours confondus |
| Candidate | médiane des points antérieurs de la même heure et du même jour de semaine |
| Intervalle | percentiles 10 % et 90 % du même jour de semaine et de la même heure |

La référence exige 28 journées d’apprentissage. La candidate et son intervalle
en exigent 8 pour le segment concerné. Le point courant est ajouté à
l’apprentissage seulement après son évaluation : aucune valeur future ne peut
donc fuiter dans la prévision testée.

```text
84 jours strictement antérieurs                         point à vérifier
┌──────────────────────────────────────────────────────┐      │
│ même heure, tous jours ───────────────► référence    │      │
│ même heure + même jour ───────────────► candidate    │      │
│ même segment, P10–P90 ────────────────► intervalle   │      │
└──────────────────────────────────────────────────────┘      ▼
                                                  erreurs et couverture
                                                            │
                                                            ▼
                                               le point rejoint l’historique
```

## Mesures et décision

La comparaison porte exactement sur les mêmes plis pour les deux méthodes :

- erreur absolue moyenne (`MAE`) ;
- erreur absolue médiane ;
- percentile 90 de l’erreur absolue ;
- gain de MAE face à la référence ;
- couverture observée de l’intervalle central nominal à 80 % ;
- largeur médiane de cet intervalle ;
- dérive entre la première et la seconde moitié des plis.

Un verdict ne peut être prononcé avant 100 points répartis sur au moins 14
jours. Après ce seuil, la candidate n’est éligible que si :

- son MAE baisse d’au moins 5 % ;
- l’intervalle couvre au moins 70 % des valeurs observées ;
- sa largeur médiane ne dépasse pas 60 minutes ;
- le MAE récent n’augmente pas simultanément d’au moins 3 minutes et 25 %.

Tout échec après couverture suffisante produit `Abandon recommandé`, avec une
ou plusieurs causes explicites. Les seuils et les noms de méthode sont renvoyés
avec le rapport afin que deux exécutions soient comparables.

Il n'existe pas de modèle persistant à réentraîner silencieusement : les fenêtres
roulantes sont recalculées à chaque étude avec la version de méthode affichée.
Un changement de méthode impose une nouvelle version et un nouveau backtest.
Les vacances, jours fériés, météo et changements majeurs du parc ne sont pas
encore des variables de la candidate ; cette limite empêche de confondre une
éligibilité technique avec une autorisation de publication.

## Flux applicatif

```text
administrateur choisit une attraction par son nom
                     │
                     ▼
GET /admin/live/forecast-backtests/{parkItemId}
                     │
                     ▼
Application : période + droits + entités + mapping courant
                     │
                     ▼
Infrastructure : buckets Mongo bornés à la cible et aux politiques
                     │
                     ▼
Core : points horaires + plis chronologiques + mesures + verdict
                     │
                     ▼
WebAPI : contrat admin sans échantillon ni identifiant externe
                     │
                     ▼
écran responsive : preuves, seuils, causes et méthode
```

## Architecture et sécurité

- `Core` possède la politique versionnée, les métriques et la décision pure.
- `Application` borne la période, applique les droits et filtre la provenance.
- `Infrastructure` réutilise la lecture Mongo déjà bornée de LIVE-13 ; aucun
  schéma ni index supplémentaire n’est requis.
- `WebAPI` expose une route `admin` authentifiée, activée, non mise en cache et
  soumise au rate limiting du pilotage live.
- `Angular` appelle cette route derrière le port de la façade admin. Les noms
  métier sont affichés ; les identifiants fournisseur restent absents.

Le calcul n’est jamais inclus dans le SSR ni dans le bundle public initial. Sa
charge maximale reste bornée à 264 jours de buckets (180 jours évalués et 84
jours d’apprentissage) pour une seule attraction. Les observations sont
réduites à un point par heure avant le backtest.

## Responsive et accessibilité

La grille comporte trois colonnes sur grand écran, puis une seule sous 640 px.
Chaque conteneur utilise `min-width: 0`, les libellés reviennent à la ligne et
les actions deviennent pleine largeur sur mobile. Le verdict est à la fois
coloré et écrit ; les causes restent une liste textuelle lisible sans couleur.

## Limite avant LIVE-15

LIVE-15 pourra implémenter une prévision publique conditionnelle, mais ne devra
rien afficher pour une attraction dont le rapport courant n’est pas
`EligibleForPilot`. Toute valeur publiée devra conserver sa fourchette, sa
méthode, sa date de calcul et son erreur mesurée.
