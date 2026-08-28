# Roadmap 08 — Données live, temps d’attente et intelligence d’affluence

> Code programme : `LIVE`
>
> Statut : étude et phase tardive. Aucune implémentation n’est autorisée par ce document avant les gates de source, de droits, de mapping, de fraîcheur, de charge et d’exploitation.
>
> Dépendances : `RANK`, `PASS`, `WATCH`, qualité/observabilité transverse et contrats de source validés.
>
> Principe : une donnée live affiche toujours sa source, son âge et son état. Une prévision affiche une fourchette, une méthode et un niveau de confiance. L’absence de donnée n’est jamais transformée en zéro minute.

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
| `LIVE-01` | Inventaire juridique/technique des sources | Source pilote autorisée |
| `LIVE-02` | Modèle provenance/fraîcheur | Sémantique publique |
| `LIVE-03` | Mapping et admin | Aucun mapping heuristique public |
| `LIVE-04` | Adaptateur pilote | Fixtures complètes |
| `LIVE-05` | Scheduler/circuit breaker/budgets | Charge bornée |
| `LIVE-06` | Latest store | Pas d’écrasement ancien |
| `LIVE-07` | Quarantaine/anomalies | Données douteuses isolées |
| `LIVE-08` | API latest/cache | Source et âge obligatoires |
| `LIVE-09` | UI pilote | 0/unknown/closed distincts |
| `LIVE-10` | Kill switches/ops | Arrêt immédiat possible |
| `LIVE-11` | Alertes temporaires | Hystérésis/expiration |
| `LIVE-12` | Historique autorisé | Rétention et buckets |
| `LIVE-13` | Statistiques descriptives | Volumes/lacunes visibles |
| `LIVE-14` | Étude prévision/backtest | Peut conclure à l’abandon |
| `LIVE-15` | Prévision publique conditionnelle | Intervalle et erreur publiés |

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
