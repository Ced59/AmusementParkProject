# Roadmap 08 — Données live, temps d’attente et intelligence d’affluence

> Code programme : `LIVE`
>
> Statut : `LIVE-01` à `LIVE-15` livrés au 29 septembre 2026. La source pilote,
> sa provenance, ses mappings, son affichage first-party, ses contrôles
> opérationnels, ses alertes temporaires et son historique borné sont
> implémentés. La prévision de l'heure suivante reste conditionnée par un
> backtest favorable et un état live actuel ouvert. La collecte et la lecture publique restent pilotées par deux
> interrupteurs d'exploitation indépendants.
>
> Dépendances : `RANK`, `PASS`, `WATCH`, qualité/observabilité transverse et contrats de source validés.
>
> Principe : une donnée live affiche toujours sa source, son âge et son état. Une prévision affiche une fourchette, une méthode et un niveau de confiance. L’absence de donnée n’est jamais transformée en zéro minute.

## État d'implémentation au 29 septembre 2026

`LIVE-01` a sélectionné ThemeParks.wiki REST v1 comme source pilote et
Phantasialand comme parc du futur spike borné. La décision, les droits, les
empreintes documentaires, l'attribution, les quotas, la politique de stockage, les
conditions d'arrêt et le budget VPS sont consignés dans
[`product-growth-live-01-source-inventory-2026-09-28.md`](../../architecture/product-growth-live-01-source-inventory-2026-09-28.md).

La gate `LIVE-A` est franchie uniquement pour un spike interne sur le latest :

- une source, un parc, un poll au plus toutes les cinq minutes ;
- aucun endpoint ni affichage public ;
- aucun historique, export ou miroir du flux ;
- état sûr « indisponible » lorsque la source est suspendue ;
- nouvelle revue obligatoire avant `LIVE-D` et avant tout stockage historique.

`LIVE-02` a livré dans le domaine pur les sources et leurs politiques d'usage, la
provenance versionnée de chaque observation, les niveaux de confiance et la
qualification déterministe `Fresh`, `Aging`, `Stale`, `Expired` ou `Unavailable`.
Les trente tests ciblés couvrent notamment les frontières temporelles, l'absence
d'horodatage et les dérives d'horloge. La décision est détaillée dans
[`product-growth-live-02-provenance-freshness-2026-09-28.md`](../../architecture/product-growth-live-02-provenance-freshness-2026-09-28.md).

`LIVE-03` a livré le mapping append-only des identifiants fournisseur vers les
parcs et attractions internes, les transitions humaines vérifiées, la protection
contre les révisions concurrentes et un écran d'administration responsive. Le
détail est consigné dans
[`product-growth-live-03-verified-target-mapping-2026-09-28.md`](../../architecture/product-growth-live-03-verified-target-mapping-2026-09-28.md).

`LIVE-04` a livré l'adaptateur ThemeParks.wiki derrière un port Application et un
modèle Core indépendant du fournisseur. Les statuts, les six familles de files,
les fenêtres de retour, groupes virtuels et tarifs sont normalisés sans confondre
zéro et absence. Les valeurs nouvelles deviennent inconnues et produisent un
diagnostic au lieu d'être assimilées à une ouverture. Le détail est consigné dans
[`product-growth-live-04-provider-adapter-2026-09-28.md`](../../architecture/product-growth-live-04-provider-adapter-2026-09-28.md).

`LIVE-05` a livré la boucle séquentielle dédiée, le lease Mongo distribué, la
fenêtre horaire locale, le jitter, le backoff, le respect de `Retry-After` et le
circuit breaker. La configuration versionnée reste désactivée et sans cible : le
déploiement ne déclenche donc aucun appel externe. Le détail est consigné dans
[`product-growth-live-05-bounded-scheduler-2026-09-29.md`](../../architecture/product-growth-live-05-bounded-scheduler-2026-09-29.md).

`LIVE-06` a livré le stockage interne du dernier état normalisé relié aux seuls
mappings vérifiés. Une écriture Mongo atomique compare d'abord l'heure de la
source puis l'heure de réception : une réponse retardée ne peut donc jamais
écraser une observation plus récente. Le détail est consigné dans
[`product-growth-live-06-latest-store-2026-09-29.md`](../../architecture/product-growth-live-06-latest-store-2026-09-29.md).

`LIVE-07` a livré le sas de qualité : mappings absents ou inéligibles,
horodatages impossibles, contradictions métier et diagnostics fournisseur sont
conservés temporairement hors du latest. Après correction d'un mapping, une
commande administrateur bornée et auditée peut rejouer le lot sans réintroduire
une donnée ancienne. Le détail est consigné dans
[`product-growth-live-07-quarantine-anomalies-2026-09-29.md`](../../architecture/product-growth-live-07-quarantine-anomalies-2026-09-29.md).

`LIVE-08` livre les trois lectures first-party bornées par parc ou élément. Le
contrat distingue courant, expiré, indisponible et absent, conserve `0` contre
`null`, fournit source, attribution, âge et confiance, et masque les faits
opérationnels expirés. Le cache de 30 secondes et son ETag ne dépassent jamais la
fraîcheur métier. Le détail est consigné dans
[`product-growth-live-08-latest-api-cache-2026-09-29.md`](../../architecture/product-growth-live-08-latest-api-cache-2026-09-29.md).

`LIVE-14` livre la comparaison chronologique d'une candidate « même jour de la
semaine » avec une référence horaire simple. Le rapport mesure les erreurs,
l'intervalle et la dérive, puis conclut données insuffisantes, abandon ou
éligibilité à un pilote. Le détail est consigné dans
[`product-growth-live-14-forecast-backtest-2026-09-29.md`](../../architecture/product-growth-live-14-forecast-backtest-2026-09-29.md).

`LIVE-15` livre la prévision publique strictement conditionnelle. Une carte ne
peut apparaître que lorsque le backtest courant reste éligible et que la
prochaine heure appartient au créneau couvert de la journée. Elle affiche la
valeur, la fourchette, la méthode, la date, l'erreur mesurée et la source. Le
détail est consigné dans
[`product-growth-live-15-public-conditional-forecast-2026-09-29.md`](../../architecture/product-growth-live-15-public-conditional-forecast-2026-09-29.md).

## 0. Avenant technique FOUNDATION

`LIVE` peut réutiliser les primitives de lease, retry, dead-letter et réconciliation de FOUNDATION, mais possède un budget de concurrence distinct. L’ingestion externe ne doit jamais affamer les jobs de classement, d’export, de purge ou de notification.

- natural key par source et fenêtre de collecte ;
- un seul poll actif par source ;
- payload brut hors job lorsqu’il est volumineux ;
- mapping et provenance référencés par identifiant/version ;
- lease plus courte que l’intervalle de polling, renouvelable ;
- kill switch par source ;
- backlog et CPU surveillés ;
- aucun passage automatique au replica set ou à un broker sans ADR et besoin démontré.

## 1. Vision produit

À terme, le site peut aider avant et pendant une visite avec :

- statut opérationnel d’un parc ou élément ;
- temps d’attente récent ;
- historique par tranche horaire ;
- alerte factuelle de réouverture ou baisse sous un seuil ;
- calendrier d’affluence prudent ;
- comparaison avec des journées historiques similaires ;
- suggestion « que faire maintenant ? » explicable.

Ces fonctions peuvent créer une forte récurrence, mais aussi détruire la confiance si elles sont obsolètes, juridiquement fragiles ou pseudo-précises. La roadmap place donc les conditions d’arrêt avant les fonctionnalités.

## 2. Objectifs

- Inventorier les sources et leurs droits.
- Définir un modèle commun de provenance et fraîcheur.
- Mapper durablement les identifiants externes aux entités internes.
- Distinguer statuts, temps d’attente, horaires et capacité.
- Ingérer avec polling borné, cache, retries et circuit breaker.
- Afficher la dernière observation et son âge.
- Stocker un historique limité et documenté.
- Produire des alertes opt-in via `WATCH`.
- N’introduire des statistiques/prévisions qu’après seuils de couverture.
- Protéger le VPS par budgets, kill switches et backpressure.
- Permettre correction d’un mapping sans perdre la traçabilité.

## 3. Non-objectifs de la première phase

- prédire précisément chaque minute ;
- garantir un temps d’attente ;
- reprendre une API sans vérifier ses conditions ;
- contourner une protection technique ;
- crowdsourcing public non modéré ;
- localisation obligatoire ;
- itinéraire virage par virage ;
- collecte de fréquentation individuelle ;
- affichage live pour tous les parcs immédiatement ;
- SignalR par défaut ;
- conserver indéfiniment toutes les observations brutes ;
- vendre une meilleure position dans les suggestions.

## 4. Gate préalable de source `LIVE-A`

Pour chaque source candidate, documenter :

- propriétaire ;
- type : officielle, opérateur, partenaire, agrégateur autorisé, contribution ;
- conditions d’utilisation ;
- attribution ;
- fréquence permise ;
- stockage historique autorisé ou interdit ;
- redistribution ;
- usage commercial ;
- limitations territoriales ;
- durée du contrat ;
- mécanisme de contact ;
- fiabilité observée ;
- quotas et coûts ;
- méthode de retrait ;
- données personnelles éventuelles.

### Conditions de sortie

- avis juridique/contractuel proportionné ;
- preuve conservée ;
- attribution conçue ;
- aucune dépendance à une source instable sans repli ;
- possibilité de désactiver immédiatement ;
- absence de scraping non autorisé.

Si cette gate échoue, la source n’est pas intégrée.

## 5. Modèle de provenance

## 5.1 `LiveDataSource`

```csharp
public sealed class LiveDataSource
{
    public string Id { get; }
    public LiveDataSourceType Type { get; }
    public string DisplayName { get; }
    public SourceUsagePolicy UsagePolicy { get; }
    public TimeSpan MinimumPollingInterval { get; }
    public TimeSpan DefaultTtl { get; }
    public bool HistoricalStorageAllowed { get; }
    public bool RedistributionAllowed { get; }
    public string AttributionTemplateKey { get; }
    public DateTime PolicyReviewedAtUtc { get; }
    public LiveDataSourceStatus Status { get; }
}
```

## 5.2 `LiveObservationProvenance`

- source ;
- identifiant externe ;
- heure annoncée par la source ;
- heure de réception ;
- heure de normalisation ;
- trace/corrélation ;
- version d’adaptateur ;
- mapping version ;
- confiance ;
- licence/règle applicable ;
- éventuelle transformation.

Le public voit au minimum source, âge et état de confiance. L’administration voit la chaîne complète.

## 6. Mapping des entités

## 6.1 `ExternalLiveTargetMapping`

```csharp
public sealed class ExternalLiveTargetMapping
{
    public string SourceId { get; }
    public string ExternalTargetId { get; }
    public RatingTargetType/InternalLiveTargetType TargetType { get; }
    public Guid InternalTargetId { get; }
    public MappingStatus Status { get; }
    public MappingConfidence Confidence { get; }
    public DateTime ValidFromUtc { get; }
    public DateTime? ValidToUtc { get; }
    public int Revision { get; }
}
```

`MappingStatus` :

- `Candidate` ;
- `Verified` ;
- `Suspended` ;
- `Superseded` ;
- `Rejected`.

### 6.1.1 Règles

- aucun mapping publié sur simple similitude de nom ;
- validation humaine initiale ;
- parc et pays cohérents ;
- statut/cycle de vie cohérent ;
- identifiant externe réutilisé détecté ;
- historique de mapping ;
- un mapping incorrect peut être corrigé sans réécrire la provenance brute ;
- les observations mal mappées sont mises en quarantaine puis réattribuées par job audité si autorisé.

## 6.2 Diagnostics

- identifiants externes inconnus ;
- doublons ;
- un externe vers plusieurs internes actifs ;
- plusieurs externes vers un interne ;
- nom changé ;
- cible fermée ;
- parc incohérent ;
- volume anormal ;
- source silencieuse.

## 7. Modèle de statut

## 7.1 États internes

- `Open` ;
- `Closed` ;
- `TemporarilyClosed` ;
- `Delayed` ;
- `Down` ;
- `WeatherClosed` ;
- `Maintenance` ;
- `OperatingWithLimitations` ;
- `Unknown` ;
- `NotOperatingToday` ;
- `Removed`.

Chaque adaptateur mappe explicitement les états source. Une valeur non reconnue devient `Unknown`, jamais `Open`.

## 7.2 `LiveStatusObservation`

- target ;
- status ;
- wait time facultatif ;
- source time ;
- received time ;
- expires at ;
- provenance ;
- raw hash ;
- validation state ;
- anomaly flags.

## 8. Temps d’attente

### 8.1 Valeur

- entier en minutes ;
- minimum 0 ;
- maximum technique borné ;
- `null` pour inconnu ;
- 0 signifie explicitement zéro fourni par la source, pas absence ;
- statut fermé + temps non nul déclenche diagnostic ;
- valeur estimée/officielle distinguée si la source l’indique.

### 8.2 Fraîcheur

États publics :

- `Fresh` ;
- `Aging` ;
- `Stale` ;
- `Expired` ;
- `Unavailable`.

Les seuils dépendent de la source et sont versionnés. Exemple de cadrage :

- Fresh jusqu’à 10 minutes ;
- Aging 10–20 ;
- Stale 20–30 ;
- Expired après 30.

Ce ne sont pas des valeurs universelles ; elles sont validées source par source.

### 8.3 Affichage

Toujours :

- valeur ;
- statut ;
- « mis à jour il y a… » ;
- source ;
- indication officiel/estimé ;
- avertissement si vieillissant ;
- disparition ou section historique si expiré ;
- jamais une ancienne valeur présentée comme actuelle.

## 9. Architecture d’ingestion

```text
scheduler borné
→ source adapter
→ validation transport/schéma
→ stockage brut temporaire autorisé
→ normalisation
→ mapping
→ validation métier/anomalies
→ latest snapshot atomique
→ historique selon politique
→ événement factuel/outbox
→ cache/API
```

### 9.1 Adaptateurs

Interface :

```csharp
ILiveSourceAdapter
{
    Task<LiveSourceBatch> FetchAsync(LiveFetchContext context, CancellationToken ct);
}
```

Responsabilités :

- auth source ;
- HTTP ;
- quotas ;
- parsing ;
- métadonnées ;
- aucune règle de parc interne.

### 9.2 Scheduler

- intervalles par source ;
- jitter ;
- verrou distribué ou single leader ;
- pas de chevauchement ;
- timeout ;
- cancellation ;
- backoff ;
- circuit breaker ;
- désactivation ;
- priorité aux parcs actifs ;
- aucun polling la nuit si inutile et non requis ;
- budget global VPS.

### 9.3 Backpressure

- batch borné ;
- file limitée ;
- abandon contrôlé des observations intermédiaires si seule la dernière compte, selon politique ;
- jamais saturation mémoire ;
- métrique de retard ;
- kill switch automatique si erreurs/CPU ;
- pas de retry infini.

## 10. Persistance

Collections possibles :

- `live-data-sources` ;
- `external-live-target-mappings` ;
- `live-latest-observations` ;
- `live-observation-history` ;
- `live-ingestion-runs` ;
- `live-quarantine` ;
- `live-anomaly-events`.

### 10.1 Latest

Index unique `(SourceId, InternalTargetId)` ou stratégie de source prioritaire. Mise à jour atomique avec comparaison de timestamp pour éviter qu’un batch ancien écrase un récent.

### 10.2 Historique

- stockage seulement si autorisé ;
- partition/bucket temporel ;
- compression ;
- rétention brute courte ;
- agrégats horaires/journaliers plus longs ;
- TTL documenté ;
- suppression source par politique ;
- conservation des agrégats seulement si conforme.

### 10.3 Quarantaine

- payload minimisé ;
- raison ;
- source ;
- durée courte ;
- accès admin ;
- résolution ;
- pas d’exposition publique.

## 11. Sélection de source et conflits

Si plusieurs sources couvrent une cible :

- priorité configurée et publique si pertinent ;
- ne pas moyenner des statuts ;
- conserver chaque provenance ;
- source officielle prioritaire sauf preuve de panne et politique explicite ;
- divergence visible admin ;
- public reçoit source retenue et éventuellement « sources divergentes » si nécessaire ;
- bascule auditable ;
- pas de fusion secrète.

## 12. API publique

```text
GET /api/public/live/parks/{parkId}
GET /api/public/live/items/{itemId}
GET /api/public/live/parks/{parkId}/items
GET /api/public/live/items/{itemId}/history?from=&to=&bucket=
GET /api/public/live/sources
GET /api/public/live/methodology/current
```

Réponse latest :

```json
{
  "targetId": "...",
  "status": "Open",
  "waitTimeMinutes": 35,
  "observedAtUtc": "...",
  "receivedAtUtc": "...",
  "freshness": "Fresh",
  "expiresAtUtc": "...",
  "source": {
    "id": "...",
    "displayName": "...",
    "type": "Official"
  },
  "confidence": "High"
}
```

### 12.1 Cache

- output cache plus court que TTL ;
- ETag ;
- stale-while-revalidate uniquement si l’UI conserve l’âge exact ;
- purge source ;
- CDN prudent ;
- aucun cache qui ressuscite une donnée expirée sans libellé.

## 13. Interface Web

### 13.1 Fiche parc

- statut général ;
- heure locale ;
- dernière mise à jour ;
- liste des éléments ;
- filtres ;
- source ;
- données indisponibles ;
- lien méthodologie ;
- mode compact responsive ;
- pas d’auto-refresh agressif en arrière-plan.

### 13.2 Fiche élément

- statut ;
- temps ;
- âge ;
- historique simple ;
- seuil d’alerte via `WATCH` ;
- note personnelle et Ride Log séparés visuellement ;
- aucune confusion entre popularité, qualité et attente.

### 13.3 Polling client

- seulement lorsque page visible ;
- intervalle adapté à TTL ;
- arrêt onglet caché ;
- ETag ;
- bouton actualiser ;
- indication réseau ;
- pas de WebSocket sans besoin mesuré.

## 14. Alertes live

Types initiaux :

- attraction rouverte ;
- temps sous seuil ;
- temps au-dessus d’un seuil, si l’utilisateur le demande ;
- statut dégradé ;
- donnée devenue indisponible, pas nécessairement notifiée.

Règles :

- opt-in par visite/session ou durée limitée ;
- expiration automatique fin de journée ;
- cooldown ;
- hystérésis pour éviter oscillations ;
- source/fraîcheur ;
- Web/e-mail non adapté aux alertes minute par minute : commencer par Web et ne promettre pas l’immédiateté ;
- push reporté au mobile ;
- aucune alerte si donnée vieillissante/expirée.

## 15. Historique descriptif

Avant toute prévision, offrir :

- médiane par tranche horaire ;
- quartiles ;
- minimum/maximum robustes ;
- nombre d’observations ;
- nombre de jours couverts ;
- couverture ;
- jours comparables ;
- statut des données ;
- exclusions ;
- période.

Ne pas afficher une courbe continue lorsque les observations sont rares. Montrer les lacunes.

## 16. Calendrier d’affluence

### 16.1 Gate statistique `LIVE-F`

Minimum à définir après exploration, par exemple :

- deux saisons complètes ;
- nombre minimal de jours comparables ;
- couverture horaire ;
- stabilité de la source ;
- changements majeurs du parc annotés ;
- vacances/jours fériés documentés ;
- backtest ;
- erreur publiée.

### 16.2 Sortie

Pas « affluence 63 % » sans définition.

Préférer :

- niveau qualitatif ;
- médiane historique ;
- intervalle ;
- nombre de journées ;
- contexte ;
- confiance ;
- facteurs connus ;
- date du modèle.

### 16.3 Backtesting

- fenêtres temporelles sans fuite future ;
- baseline simple ;
- MAE/erreur adaptée ;
- calibration des intervalles ;
- comparaison à « même jour de semaine » ;
- seuil d’arrêt si le modèle ne bat pas la baseline ;
- monitoring dérive ;
- réentraînement documenté ;
- aucune IA générative nécessaire.

## 17. « Que faire maintenant ? »

Phase encore ultérieure, seulement avec données fiables.

Facteurs possibles :

- liste personnelle ;
- compatibilité groupe ;
- statut ;
- temps et fraîcheur ;
- distance approximative si l’utilisateur choisit sa position au premier plan ;
- élément déjà fait dans la visite ;
- préférence ;
- fermeture prochaine officielle.

Sortie :

- plusieurs options ;
- facteurs ;
- incertitudes ;
- contrôle des poids ;
- aucune promesse d’optimisation ;
- aucune collecte de position sans action visible ;
- recommandation non sponsorisée.

## 18. Administration et exploitation

Dashboard :

- source status ;
- dernier succès ;
- latence ;
- quota ;
- erreurs ;
- mappings candidats ;
- quarantaine ;
- anomalies ;
- couverture ;
- données expirées ;
- charge ;
- stockage ;
- kill switches ;
- version adaptateur ;
- politique/licence.

Actions :

- suspendre source/parc/cible ;
- corriger mapping ;
- rejouer batch borné ;
- purger ;
- prévisualiser impact ;
- changer TTL avec version ;
- révoquer une alerte ;
- exporter diagnostics.

## 19. Sécurité

- secrets source hors repo ;
- rotation ;
- egress limité ;
- validation stricte JSON/XML ;
- taille de réponse ;
- timeout ;
- protection SSRF ;
- URL source configurée, pas fournie par utilisateur ;
- logs sans token ;
- rate limiting API publique ;
- protection admin ;
- dépendances inspectées ;
- sandbox parser si format complexe ;
- aucun rendu HTML non sûr fourni par source.

## 20. Tests obligatoires

### Adaptateurs

- payload normal ;
- champ absent ;
- nouvel enum ;
- réponse vide ;
- timeout ;
- 429 ;
- 500 ;
- ordre temporel ;
- horloge source erronée ;
- gros payload ;
- token expiré.

### Domaine/Application

- mapping ;
- statut inconnu ;
- 0 vs null ;
- closed + wait ;
- freshness ;
- priorité sources ;
- divergence ;
- batch ancien ;
- déduplication ;
- rétention ;
- alerte hystérésis ;
- seuil statistique.

### Infrastructure

- scheduler concurrent ;
- verrou ;
- circuit breaker ;
- backpressure ;
- outbox ;
- TTL ;
- cache ;
- kill switch ;
- correction mapping ;
- stockage volumique.

### Web/API/E2E

1. ingest frais ;
2. afficher source/âge ;
3. laisser expirer ;
4. vérifier que la valeur n’apparaît plus comme live ;
5. source en panne ;
6. mapping suspendu ;
7. alerte sous seuil avec hystérésis ;
8. aucune alerte sur donnée stale ;
9. historiques avec lacunes ;
10. kill switch sans erreur publique trompeuse.

## 21. Observabilité et budgets

- CPU/mémoire par source ;
- appels/quota ;
- durée batch ;
- payload ;
- observations ;
- retard ;
- mapping inconnus ;
- fraîcheur réelle ;
- couverture ;
- cache hit ;
- API latency ;
- stockage/jour ;
- alertes ;
- erreurs ;
- circuit ouvert ;
- données expirées affichées par bug : métrique critique.

Budgets bloquants à définir avant prod :

- CPU maximal ;
- mémoire ;
- requêtes/minute ;
- stockage mensuel ;
- coût source ;
- temps d’intervention ;
- nombre de sources/parcs pilotes.

## 22. Déploiement par gates

### `LIVE-A` — droits/source

Contrat et attribution.

### `LIVE-B` — spike sans public

Un parc, latest uniquement, admin, charge mesurée.

### `LIVE-C` — mapping et qualité

Mapping vérifié, anomalies, quarantaine.

### `LIVE-D` — public latest

Source/âge/statut, pas d’historique ni alerte.

### `LIVE-E` — watch

Alertes limitées, expiration et cooldown.

### `LIVE-F` — historique

Rétention autorisée, couverture et agrégats descriptifs.

### `LIVE-G` — prévision

Backtest, baseline battue, intervalles et méthode publique.

Chaque gate peut arrêter définitivement la phase suivante.

## 23. Découpage recommandé en PR

| PR | Contenu | Critère |
|---|---|---|
| `LIVE-01` | ✅ Inventaire juridique/technique des sources | ThemeParks.wiki autorisée pour un spike interne latest-only |
| [`LIVE-02`](../../architecture/product-growth-live-02-provenance-freshness-2026-09-28.md) | ✅ Modèle provenance/fraîcheur | Sémantique de domaine testée, sans exposition publique |
| [`LIVE-03`](../../architecture/product-growth-live-03-verified-target-mapping-2026-09-28.md) | ✅ Mapping et admin | Aucun mapping heuristique public |
| [`LIVE-04`](../../architecture/product-growth-live-04-provider-adapter-2026-09-28.md) | ✅ Adaptateur pilote | Fixtures complètes |
| [`LIVE-05`](../../architecture/product-growth-live-05-bounded-scheduler-2026-09-29.md) | ✅ Scheduler/circuit breaker/budgets | Charge bornée |
| [`LIVE-06`](../../architecture/product-growth-live-06-latest-store-2026-09-29.md) | ✅ Latest store | Pas d’écrasement ancien |
| [`LIVE-07`](../../architecture/product-growth-live-07-quarantine-anomalies-2026-09-29.md) | ✅ Quarantaine/anomalies | Données douteuses isolées |
| [`LIVE-08`](../../architecture/product-growth-live-08-latest-api-cache-2026-09-29.md) | ✅ API latest/cache | Source et âge obligatoires |
| [`LIVE-09`](../../architecture/product-growth-live-09-public-ui-2026-09-29.md) | ✅ UI pilote | 0/unknown/closed distincts |
| [`LIVE-10`](../../architecture/product-growth-live-10-operational-controls-2026-09-29.md) | ✅ Kill switches/ops | Arrêt immédiat possible |
| [`LIVE-11`](../../architecture/product-growth-live-11-temporary-alerts-2026-09-29.md) | ✅ Alertes temporaires | Hystérésis/expiration |
| [`LIVE-12`](../../architecture/product-growth-live-12-authorized-history-2026-09-29.md) | ✅ Historique autorisé | Rétention et buckets |
| [`LIVE-13`](../../architecture/product-growth-live-13-descriptive-statistics-2026-09-29.md) | ✅ Statistiques descriptives | Volumes/lacunes visibles |
| [`LIVE-14`](../../architecture/product-growth-live-14-forecast-backtest-2026-09-29.md) | ✅ Étude prévision/backtest | Peut conclure à l’abandon |
| [`LIVE-15`](../../architecture/product-growth-live-15-public-conditional-forecast-2026-09-29.md) | ✅ Prévision publique conditionnelle | Intervalle et erreur publiés |

### Implémentation `LIVE-02` — 28 septembre 2026

La version `5.4.0` introduit un langage commun indépendant du fournisseur pour
qualifier toute future donnée live. Une observation conserve son origine, les
heures d'observation, de réception et de normalisation, les versions du contrat,
de l'adaptateur, du mapping et de la transformation ainsi qu'un niveau de
confiance explicite. La licence et l'attribution restent donc auditables après
normalisation.

Une politique pure calcule l'âge et l'expiration à partir d'une heure fournie par
l'appelant. Elle distingue les cinq états sans jamais transformer une donnée
absente ou incohérente en valeur courante. Le modèle reste dans Core : aucune
dépendance HTTP, MongoDB, fournisseur ou interface n'a été introduite. Aucun appel
externe, stockage, endpoint ou affichage live n'est encore actif.

### Implémentation `LIVE-03` — 28 septembre 2026

La version `5.4.2` ajoute une chaîne de correspondance vérifiée entre les
identifiants externes et les entités réelles du produit. Chaque création,
validation, correction, suspension, rejet ou remplacement produit une révision
immuable, avec acteur, motif et contrôle de concurrence. Un candidat ne peut
jamais alimenter le live, même lorsque son nom ressemble exactement à celui d'une
attraction.

MongoDB conserve toutes les révisions dans une nouvelle collection indexée. Les
cas d'usage restent derrière des ports Application et l'administration utilise
des endpoints protégés, audités et limités. L'écran Angular en cartes permet de
comparer les deux côtés puis de décider sur ordinateur ou mobile sans débordement
horizontal. Aucun fournisseur n'est encore appelé et rien n'est public.

### Implémentation `LIVE-04` — 28 septembre 2026

La version `5.4.4` introduit l'adaptateur pilote ThemeParks.wiki REST v1 sans
activer de polling ni d'endpoint public. Le fournisseur reste cantonné à
Infrastructure ; Application ne connaît qu'un port générique et Core porte les
états, types de files et observations normalisées.

L'adaptateur utilise une URL fixe en HTTPS, refuse les redirections, borne le
timeout et le corps de réponse à 2 Mio, transmet les ETag et expose les 429 sans
relance automatique. Il conserve un hash SHA-256 du payload pour la future chaîne
de preuve. La normalisation refuse plus de 10 000 observations, limite les
diagnostics à 1 000 et isole les entrées mal formées sans perdre leurs voisines.
Les valeurs de statut ou de file inconnues sont diagnostiquées et reviennent dans
un état sûr. Core interdit les champs étrangers à un type de file et signale la
contradiction fermeture/attente sans effacer le fait reçu. Les fixtures couvrent
les six files, les quatre statuts connus, `0` contre `null`, les valeurs futures,
les champs invalides, les réponses vides et les principaux incidents HTTP.

### Implémentation `LIVE-05` — 29 septembre 2026

La version `5.4.5` ajoute un ordonnanceur interne désactivé par défaut, limité à
un fournisseur et un parc pilotes. Un lease Mongo atomique empêche deux instances
de collecter simultanément la même cible. La boucle dédiée est séquentielle et ne
consomme aucun slot des jobs métier généraux.

Le domaine impose au moins cinq minutes entre deux appels, une fenêtre active
dans le fuseau local du parc, un jitter positif, un backoff exponentiel borné et
un circuit breaker persistant. Les réponses `429` honorent `Retry-After`, les
ETag sont réutilisés et les crashes libèrent implicitement la cible à l'expiration
du lease. Des métriques comptent les issues, durées et ouvertures de circuit sans
journaliser les payloads. Aucun temps d'attente n'est encore stocké : cette
responsabilité est réservée à `LIVE-06`.

### Implémentation `LIVE-06` — 29 septembre 2026

La version `5.4.6` conserve un seul état normalisé par source et cible interne,
uniquement après résolution en lot d'un mapping humain encore vérifié. Chaque
photographie garde la provenance complète, la version du mapping, la politique
d'usage, le hash du payload, les files typées et la politique de fraîcheur qui
permettra de calculer son âge sans ambiguïté.

MongoDB applique la règle d'ancienneté dans l'écriture atomique elle-même :
l'heure observée par la source prime, puis l'heure de réception départage deux
versions du même instant. La concurrence ne peut donc pas faire régresser le
dernier état. Un échec de stockage transforme la collecte en échec et ne mémorise
pas son ETag, afin que le payload puisse être rejoué au passage suivant. La
collection n'est ni historisée, ni exposée par une API ou une interface ; le
polling reste désactivé. Les cibles inconnues et anomalies sont désormais
isolées par `LIVE-07` avant toute exploitation.

### Implémentation `LIVE-07` — 29 septembre 2026

La version `5.4.7` introduit une quarantaine courte et idempotente avant toute
écriture latest. Les observations non reliées, les mappings inéligibles, les
heures impossibles, les contradictions fermeture/attente et les diagnostics de
l'adaptateur conservent une preuve minimisée pendant sept jours, sans payload
brut. Une défaillance de ce stockage invalide tout le poll afin de ne perdre
aucun signal qualité.

Une commande administrateur protégée, auditée et limitée rejoue de 1 à 100
incidents après correction humaine. Elle recharge les mappings actuels, applique
de nouveau les règles du domaine, déduplique chaque couple source/cible et laisse
bloquée toute donnée encore douteuse. L'écriture latest monotone empêche toujours une ancienne
photographie de régresser l'état. Le polling demeure désactivé et aucune API ou
interface publique n'est ajoutée.

### Implémentation `LIVE-08` — 29 septembre 2026

La version `5.4.8` ajoute les lectures publiques latest limitées à une fiche parc
ou élément. Une réponse explicite sépare observation courante, expirée,
indisponible et absente. Le temps zéro n'est jamais confondu avec l'absence ; les
faits opérationnels d'une observation expirée sont retirés, tandis que sa source,
son âge et son expiration restent disponibles pour expliquer l'indisponibilité.

La sélection de source est déterministe dans Core et ne fusionne jamais deux
provenances. Application vérifie les entités publiques, Infrastructure fournit le
catalogue juridique et MongoDB, et WebAPI borne le canal avec ETag, cache de 30
secondes, rate limiting et CORS existants. Une migration idempotente unifie les
anciens timestamps Mongo. L'attribution ThemeParks.wiki accompagne chaque donnée,
mais collecte et lecture publique restent désactivées par défaut jusqu'à
`LIVE-09` et à l'ouverture explicite des interrupteurs d'exploitation.

### Implémentation `LIVE-09` — 29 septembre 2026

La version `5.4.9` ajoute un bloc « En direct » compact aux fiches parc et
attraction. L’interface distingue explicitement zéro minute, attente inconnue,
fermeture, information expirée et absence d’observation. Une fiche parc peut
filtrer les attractions couvertes, tandis qu’une fiche attraction détaille les
files disponibles sans mélanger ces faits opérationnels avec les notes, avis ou
passages du passeport.

Le navigateur conserve l’ETag, adapte son prochain contrôle à l’expiration la
plus proche et arrête toute actualisation lorsque l’onglet est masqué ou hors
ligne. La source, l’âge et une explication publique restent visibles. Le bloc ne
s’affiche pas tant que la lecture publique du pilote est désactivée ; son
ouverture opérationnelle reste volontairement réservée à `LIVE-10`.

### Implémentation `LIVE-11` — 29 septembre 2026

La version `5.4.12` permet à un membre connecté de suivre temporairement la
réouverture, une attente sous ou au-dessus d’un seuil ou un fonctionnement
dégradé depuis la fiche d’une attraction. Les alertes expirent automatiquement
après 1, 3 ou 6 heures, ou au plus tard à la fin de la journée locale du parc.
Le centre WATCH regroupe les alertes actives et les notifications reçues avec
le nom, l’image, la source et l’âge exact de la donnée.

Le domaine impose une hystérésis de cinq minutes et un cooldown de trente
minutes pour éviter les oscillations. Seules les observations publiques,
fraîches, plus récentes et encore autorisées par les contrôles opérationnels
peuvent déclencher. MongoDB applique l’unicité et les TTL ; la suppression de
compte purge ces données. Le canal reste volontairement interne au site et
aucun identifiant technique ne remplace un libellé public manquant.

### Implémentation `LIVE-12` — 29 septembre 2026

La version `5.4.15` ajoute une mémoire historique strictement conditionnée par
la politique courante de chaque source. ThemeParks.wiki a été revu avant cette
activation : le produit peut conserver son propre historique et en dériver des
agrégats, mais ne peut republier ni miroir ni export massif du flux brut.

Chaque observation réellement retenue par le latest est écrite de façon
idempotente dans un brut normalisé conservé sept jours et dans un bucket horaire
minimal conservé 400 jours. Les TTL sont indépendants, les lacunes ne sont pas
comblées et aucune route publique n'expose encore ces données. La version
contractuelle accompagne les buckets ; une suspension, un retrait du droit de
stockage ou une version incohérente bloque automatiquement l'historisation.

### Implémentation `LIVE-13` — 29 septembre 2026

La version `5.4.16` publie sur chaque fiche d'attraction couverte une analyse
horaire des attentes passées. La médiane, les quartiles et une plage robuste
10–90 % sont accompagnés du nombre réel d'attentes, de jours comparables, de la
couverture et des exclusions. Une heure trop peu documentée reste vide au lieu
d'être reliée à ses voisines ; l'ensemble est présenté comme une observation et
jamais comme une prédiction.

La lecture est limitée à 90 jours et ne quitte jamais le serveur sous forme
d'échantillons bruts. Source, version juridique, politique de rétention,
mapping humain et kill switches sont revérifiés avant chaque réponse. Le calcul
pur appartient à Core, MongoDB utilise un index dédié et Angular charge le bloc
hors SSR via un port et une façade. La grille se replie jusqu'à 320 px sans
déborder du viewport et conserve une description accessible de chaque tranche.

### Implémentation `LIVE-14` — 29 septembre 2026

La version `5.4.18` ajoute au pilotage live une étude reproductible par
attraction. Chaque attente évaluée est une médiane horaire et n'est comparée
qu'à des données strictement antérieures : une référence « même heure » affronte
une candidate « même jour de semaine et même heure ». Le rapport montre les MAE,
la couverture et la largeur de l'intervalle 10–90 %, ainsi que la dérive récente.

Avant 100 points sur 14 jours, la conclusion reste « données insuffisantes ».
Après ce seuil, une candidate qui ne réduit pas le MAE d'au moins 5 %, produit
un intervalle inutilisable ou dérive conduit explicitement à l'abandon. Seul le
troisième verdict, « éligible à un pilote », pourra ouvrir LIVE-15. La route et
l'interface restent réservées à l'administration ; aucune prévision publique
n'est créée par ce jalon.

### Implémentation `LIVE-15` — 29 septembre 2026

La version `5.4.19` termine la phase LIVE avec une estimation publique pour la
prochaine heure couverte de la journée. Elle reprend exactement la méthode
candidate de LIVE-14 : médiane du même jour de semaine et de la même heure,
accompagnée de l'intervalle 10–90 %. La carte affiche aussi l'erreur moyenne et
la couverture réellement observées pendant les 90 jours du backtest courant,
la méthode expliquée, la date de calcul, le volume de preuves et la source.

La publication n'est jamais un simple interrupteur visuel. Source, licence,
fiche publique, mapping humain et contrôles opérationnels sont revérifiés avant
de charger au plus 174 jours. Un verdict insuffisant ou défavorable, moins de
huit jours comparables, un créneau hors journée ou une erreur laisse la page
sans prévision. Le calcul partagé appartient à Core, le contrat ne contient pas
d'identifiant technique de cible et le cache public de quinze minutes borne la
charge. Angular charge la carte hors SSR et la replie sur une colonne jusqu'à
320 px. Aucun changement de schéma MongoDB n'est requis.

## 24. Gate finale `LIVE-G`

- la source et ses droits sont documentés ;
- chaque observation conserve sa provenance ;
- les mappings sont vérifiés et versionnés ;
- `0`, `fermé`, `inconnu`, `stale` et `expired` sont distincts ;
- l’âge est toujours visible ;
- une donnée expirée ne reste pas présentée comme live ;
- les sources divergentes ne sont pas fusionnées secrètement ;
- la charge et le stockage sont bornés ;
- le kill switch fonctionne ;
- les alertes sont opt-in, temporaires et anti-oscillation ;
- l’historique respecte la licence ;
- aucune prévision n’existe avant couverture et backtest ;
- toute prévision affiche intervalle, méthode, date et erreur ;
- l’ordre des suggestions n’est pas sponsorisé ;
- le projet est prêt à renoncer à la fonctionnalité si elle apporte plus d’incertitude que de valeur.
