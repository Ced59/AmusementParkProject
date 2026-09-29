# LIVE-13 — Statistiques descriptives des attentes

## Finalité métier

LIVE-13 transforme la mémoire autorisée de LIVE-12 en repères compréhensibles
sur la fiche d'une attraction. Le visiteur peut voir à quelles heures l'attente
a habituellement été plus faible ou plus élevée, mais aussi combien de jours et
d'observations soutiennent ce constat.

Ce jalon ne fait aucune prévision. Une tranche sans preuve suffisante reste vide
et l'écran rappelle que les observations passées ne garantissent pas une attente
future.

## Ce qui est publié

La route publique bornée est :

```text
GET /public/live/items/{itemId}/history?from=&to=&bucket=hour
```

- la période par défaut est de 30 jours et ne peut dépasser 90 jours ;
- seul le bucket horaire est accepté ;
- la source, la cible, la politique d'usage et la politique de rétention doivent
  encore être celles autorisées au moment de la lecture ;
- le mapping humain et tous les kill switches publics sont revérifiés ;
- chaque observation doit encore correspondre à l'une des cibles externes
  actuellement validées pour l'attraction interne ;
- la réponse ne contient ni observation brute, ni identifiant externe, ni hash,
  ni corrélation, ni texte privé ;
- le calcul n'est pas exécuté pendant le SSR et n'alourdit donc pas les robots ou
  le HTML initial ; le navigateur le charge après la fiche principale.

## Méthode statistique

Pour chaque heure locale incluse dans la fenêtre active du pilote :

1. les corrections partageant le même instant observé sont dédupliquées en
   conservant la réception la plus récente ;
2. les observations hors fenêtre active sont comptées puis exclues ;
3. seuls les états `Open` et `OperatingWithLimitations` sont admissibles ;
4. seule une attente renseignée pour la file `Standby` entre dans les
   statistiques ; zéro minute reste une vraie valeur ;
5. les valeurs sont triées pour calculer la médiane, les quartiles 25 % et 75 %,
   puis les percentiles 10 % et 90 % utilisés comme minimum et maximum robustes ;
6. les volumes exclus et toute troncature d'un bucket restent visibles.

L'interpolation linéaire entre deux rangs est utilisée pour les percentiles. La
couverture compare le nombre d'observations reçues au nombre théorique de polls
dans la fenêtre active, selon l'intervalle configuré. Le calcul itère au plus sur
90 jours et tient compte du fuseau ainsi que des changements d'heure.

### Statuts de données

| Statut | Règle |
|---|---|
| `Unavailable` | aucune observation dans la fenêtre |
| `Insufficient` | moins de 12 attentes utilisables au total, moins de 5 dans une heure, ou moins de 2 jours comparables |
| `Sparse` | couverture inférieure à 60 %, moins de 5 jours comparables ou bucket tronqué |
| `Usable` | les seuils précédents sont franchis sans troncature |

Ces seuils qualifient uniquement une description. Ils ne constituent pas la gate
statistique beaucoup plus exigeante d'une future prévision.

## Flux applicatif

```text
fiche attraction dans le navigateur
           │
           ▼
requête agrégée (30 jours par défaut)
           │
           ▼
source active + droit historique + rétention courante
           │
           ▼
entités publiques + mapping vérifié + kill switches
           │
           ▼
lecture Mongo bornée par cible, politique et période
           │
           ▼
filtre de provenance par mapping actuellement validé
           │
           ▼
Core : déduplication, exclusions, couverture, percentiles
           │
           ▼
DTO agrégé + attribution, jamais les échantillons bruts
           │
           ▼
barres horaires séparées, lacunes visibles sur mobile
```

## Architecture

- `Core` possède les observations historiques minimales, les seuils, les
  exclusions et tous les calculs purs.
- `Application` orchestre les autorisations, borne la période et assemble les
  libellés publics et l'attribution.
- `Infrastructure` lit uniquement les buckets Mongo correspondant à la source,
  à la cible, à la version juridique, à la clé de rétention et à la période.
  Chaque échantillon porte aussi sa cible externe et sa version de mapping.
- `WebAPI` publie un contrat agrégé avec ETag et le cache live existant.
- `Angular` passe exclusivement par un port et une façade dédiée. Un échec de
  l'historique ne masque jamais l'état live actuel.

L'index `idx_live_history_bucket_statistics_v1` est ajouté sans remplacer
l'index LIVE-12 déjà déployé. Son initialisation est idempotente ; aucune
migration MongoDB manuelle n'est nécessaire. Au démarrage, une migration
supprime les buckets transitoires LIVE-12 qui ne contiennent pas encore ces
preuves de mapping. Les données ambiguës ne sont donc ni adaptées ni mélangées
au nouveau schéma ; la collecte autorisée les reconstitue ensuite naturellement.

## Représentation responsive et accessible

Le graphique n'utilise aucune dépendance lourde. Chaque heure est une ligne
indépendante avec :

- un trait pour la plage robuste 10–90 % ;
- un bloc pour la moitié centrale 25–75 % ;
- un repère pour la médiane ;
- les nombres d'attentes et de jours en texte ;
- un vrai emplacement « données insuffisantes » au lieu d'une interpolation.

Sous 760 px, les preuves passent sous le graphique. Sous 430 px, toutes les
zones deviennent une seule colonne, avec `min-width: 0`, retour à la ligne des
libellés et aucun défilement horizontal. Les valeurs du graphique possèdent une
description textuelle pour les lecteurs d'écran.

## Limites avant LIVE-14

- aucun calendrier d'affluence ;
- aucune valeur future ;
- aucune courbe lissée ou interpolation des lacunes ;
- aucune promesse à partir des seules statistiques descriptives ;
- LIVE-14 devra définir une étude reproductible, une baseline et un backtest,
  puis pourra conclure explicitement qu'une prévision fiable doit être abandonnée.
