# LIVE-12 — Historique live autorisé et borné

## Finalité métier

LIVE-12 donne au produit une mémoire factuelle des informations en direct déjà
acceptées par la chaîne de qualité. Cette mémoire ne constitue ni un miroir du
fournisseur ni une nouvelle API publique : elle prépare les statistiques
descriptives de `LIVE-13`, qui devront montrer leurs volumes et leurs lacunes.

Une panne ou une absence de collecte reste une absence. Aucun document n'est
créé pour combler un intervalle et aucune valeur manquante ne devient une attente
de zéro minute.

## Revue juridique du 29 septembre 2026

La revue a été renouvelée avant toute conservation historique :

- les [conditions ThemeParks.wiki](https://www.themeparks.wiki/terms) autorisent
  l'enregistrement de son propre historique issu du flux live ainsi que les
  analyses, tendances, agrégats et prévisions dérivés ;
- elles interdisent en revanche de republier un flux équivalent, un miroir ou
  un export massif des données brutes ;
- l'attribution reste obligatoire pour l'offre gratuite ;
- la [grille d'offre](https://www.themeparks.wiki/pricing) confirme que
  l'historique fourni directement par le service est limité, mais LIVE-12
  n'appelle pas cet endpoint : il conserve seulement les observations obtenues
  par le polling first-party déjà autorisé.

La version de politique `themeparks-wiki-terms-2026-09-28` reste inscrite dans
chaque observation, car le texte contractuel n'a pas changé. Sa date de revue
est actualisée. Si la source est suspendue, si le droit de stockage est retiré,
si aucune politique de rétention n'est configurée ou si la version portée par
l'observation ne correspond plus à la politique courante, l'historisation
s'arrête automatiquement.

## Politique de conservation

| Niveau | Contenu | Rétention | Usage |
|---|---|---:|---|
| brut normalisé | observation complète, provenance, files typées | 7 jours | diagnostic court et reconstruction contrôlée |
| bucket horaire | échantillons minimisés, statut et files | 400 jours | statistiques descriptives futures |

Le domaine limite structurellement le brut à 30 jours, les agrégats à 730 jours
et impose une durée de bucket qui divise exactement une journée UTC. La source
pilote choisit volontairement des limites plus basses. Les deux collections ont
leur propre index TTL ; le nettoyage ne dépend donc pas d'un job applicatif.

## Flux d'écriture

```text
poll autorisé
      │
      ▼
mapping vérifié + quarantaine + kill switches
      │
      ▼
écriture latest monotone
      │
      ▼
relecture de la valeur réellement retenue
      │
      ├── politique courante autorise l'historique ? ── non ──► arrêt sûr
      │ oui
      ▼
upsert brut par source/cible/instant observé
      │
      ▼
upsert borné dans le bucket UTC de la cible
      │
      ▼
évaluation des alertes sur le latest déjà validé
      │
      ▼
fin du poll ou échec signalé si l'historique n'a pas été conservé
```

Les identifiants naturels rendent un rejeu idempotent. Une même observation
remplace sa version de bucket seulement si elle est reçue de nouveau, sans
multiplier les documents bruts ni les échantillons. Si l'écriture
historique échoue après le latest, le poll reste en échec et son ETag n'est pas
validé : le fournisseur peut être relu au passage suivant sans faire régresser
le latest déjà disponible. L'évaluation des alertes reçoit tout de même cette
valeur validée avant que l'échec historique soit remonté : une réouverture ou un
franchissement bref ne disparaît donc pas du seul fait d'une panne du stockage
historique.

## Modèle MongoDB

```text
live-history-raw                    live-history-buckets
├─ _id cible/temps/politiques       ├─ _id cible/temps/politiques
├─ sourceId                         ├─ sourceId
├─ target                           ├─ target
├─ status                           ├─ bucketStartUtc / bucketEndUtc
├─ queues                           ├─ bucketDurationMilliseconds
├─ provenance complète              ├─ usagePolicyVersion
├─ fraîcheur/rétention dans la clé  ├─ retentionPolicyKey
├─ freshnessPolicy                  ├─ samples[] (24 maximum)
├─ payloadSha256                    │  ├─ sampleId
└─ expiresAtUtc (TTL, 7 j)          │  ├─ observedAt / receivedAt exacts
                                    │  ├─ status
                                    │  └─ queues
                                    ├─ isTruncated
                                    └─ expiresAtUtc (TTL, 400 j)
```

Le brut reste interne et n'a aucun contrôleur. Le bucket ne conserve ni hash de
payload, ni corrélation, ni identifiant externe : seulement ce qui sera utile au
calcul descriptif. Les noms de cible sont des photographies d'affichage et les
clés internes ne sont jamais un libellé de repli destiné au visiteur.
Un bucket conserve au plus 24 échantillons, soit deux fois le volume nominal du
polling à cinq minutes. Au-delà, les plus récents sont gardés et `isTruncated`
rend explicitement cette perte visible aux statistiques futures.
La clé de stockage contient à la fois la version juridique et une empreinte des
trois durées de rétention. Un changement de contrat, de durée brute, de durée
d'agrégat ou de largeur de bucket ouvre donc un nouveau document au lieu de
réétiqueter, prolonger ou mélanger les observations déjà conservées.

## Architecture et exploitation

- `Core` définit les bornes et le calcul déterministe des tranches UTC.
- `Application` vérifie la source, son état, le droit historique et la version
  contractuelle avant de franchir le port de persistance.
- `Infrastructure` réalise les écritures Mongo groupées, les clés idempotentes
  et les TTL ; deux nouvelles collections évitent toute migration de données
  existantes.
- aucune route WebAPI et aucun composant Angular ne sont ajoutés par ce jalon.

L'initialiseur MongoDB crée les collections et index de façon idempotente au
déploiement. Aucune opération manuelle sur MongoDB n'est nécessaire.

## Limites avant LIVE-13

- aucun graphique, médiane, quartile ou minimum/maximum n'est encore publié ;
- aucun accès à l'historique brut n'est exposé ;
- aucune interpolation des périodes manquantes ;
- aucune prévision ni recommandation d'affluence ;
- une future statistique devra dédupliquer les corrections partageant le même
  instant observé en retenant la réception la plus récente et afficher sa
  couverture réelle.
