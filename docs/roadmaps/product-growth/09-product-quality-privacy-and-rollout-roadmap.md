# Roadmap 09 — Qualité produit, confidentialité, instrumentation et déploiement

> Code programme : `QUAL`
>
> Statut : transverse. Cette roadmap commence avant `RANK-01` et accompagne toutes les autres. `QUAL-01` à `QUAL-06` sont livrés au 29 septembre 2026.
>
> Principe : une fonctionnalité n’est pas validée parce qu’elle compile ou parce qu’elle augmente un compteur. Elle doit être comprise, utile, accessible, fiable, réversible, respectueuse de la vie privée et supportable avec les moyens réels du projet.

## 0. Gate technique FOUNDATION

La définition de terminé inclut désormais les deux roadmaps FOUNDATION. Avant une généralisation :

- compatibilité des IDs démontrée ;
- `RatingValue` testé sur les dix valeurs et les données historiques ;
- dates partielles et fuseaux validés ;
- source de vérité active atomique ;
- idempotence et concurrence testées ;
- ordre des éléments stable ;
- jobs à lease, retry borné et reconciler pour toute réaction critique ;
- scopes canoniques pour les snapshots ;
- calcul à la demande mesuré avant matérialisation ;
- rollback ne supprimant ni visite ni publication privée ;
- dette temporaire et feature flags avec date de retrait.

### Contrôles CI ajoutés au programme

- tests de mapping des identifiants ;
- fixtures indépendantes `RatingValue` ;
- tests Mongo standalone des écritures conditionnelles ;
- vérification des indexes par nom et définition ;
- tests de lease, crash et replay ;
- test qu’un snapshot incomplet ne peut pas devenir courant ;
- test qu’un filtre arbitraire ne crée pas de scope persistant ;
- tests de renormalisation d’ordre ;
- OpenAPI diff confirmant que les IDs restent des chaînes ;
- absence de données privées dans jobs, logs, SSR et caches publics.

### Métriques d’exploitation ajoutées

- backlog et âge du plus vieux job ;
- leases expirés ;
- dead-letters ;
- retard de révision des snapshots ;
- retard des statistiques ;
- conflits optimistes ;
- replays idempotents ;
- renormalisations ;
- anomalies de conversion de notes ;
- CPU des jobs lourds.

## 1. Objectifs

- Définir une mesure produit minimale et cohérente.
- Évaluer la qualité du funnel sans surveillance excessive.
- Créer des gates de beta et des conditions d’arrêt.
- Standardiser feature flags, migrations, rollbacks et diagnostics.
- Garantir accessibilité, internationalisation et performance.
- Intégrer export, suppression, rétention et consentement dès le domaine.
- Prévenir les dark patterns, la gamification addictive et les formulations trompeuses.
- Préparer l’exploitation sur le VPS sans multiplier les services inutiles.
- Organiser les tests qualitatifs avec de vrais utilisateurs ciblés.
- Conserver une documentation de décision et d’évolution.

## 2. Non-objectifs

- collecter tous les clics ;
- installer un outil marketing avant d’avoir une question à mesurer ;
- profiler individuellement les passionnés ;
- enregistrer les notes exactes dans un analytics tiers ;
- faire dépendre une gate du nombre brut de comptes ;
- lancer un A/B test sans volume suffisant ;
- utiliser des métriques pour justifier un dark pattern ;
- adopter microservices, streaming ou data warehouse par anticipation ;
- promettre une conformité automatique uniquement grâce à un outil.

## 3. Modèle de qualité produit

Chaque capacité est évaluée sur huit dimensions :

| Dimension | Question |
|---|---|
| Utilité | Résout-elle un problème réel pour une personne ciblée ? |
| Compréhension | La personne comprend-elle la promesse, les états et les limites ? |
| Fiabilité | Les données et opérations sont-elles exactes, récupérables et sourcées ? |
| Probité | Le produit évite-t-il d’exagérer, manipuler ou masquer l’incertitude ? |
| Accessibilité | Le parcours fonctionne-t-il sans modalité unique et avec aides techniques ? |
| Confidentialité | Le minimum est-il collecté, privé par défaut, exportable et supprimable ? |
| Performance | Le parcours reste-t-il utilisable sur mobile et compatible avec le VPS ? |
| Exploitabilité | Les erreurs, abus, corrections et demandes peuvent-ils être traités ? |

Aucune dimension ne compense entièrement une autre. Une fonctionnalité attractive mais trompeuse échoue ; une fonction rigoureuse mais inutilisable échoue également.

## 4. North star et métriques

### 4.1 North star initiale

Pour le programme Passeport :

> **Nombre d’utilisateurs ayant enregistré au moins une deuxième visite réelle ou rétrospective avec une donnée utile, dans une période définie.**

Cette mesure indique davantage une valeur récurrente que :

- comptes créés ;
- pages vues ;
- followers ;
- notes saisies ;
- partages générés.

### 4.2 Activation

Étapes proposées :

1. ouvre le Passeport ;
2. crée une visite ;
3. ajoute au moins cinq éléments ou termine une petite visite ;
4. ajoute une note temporelle facultative ;
5. consulte une statistique ;
6. sauvegarde/termine ;
7. revient pour une autre visite.

Chaque étape doit pouvoir être mesurée sans stocker la liste exacte des parcs et notes dans l’analytics tiers.

### 4.3 Confiance des classements

- classements avec méthodologie visible ;
- entrées inéligibles correctement non classées ;
- ouvertures de l’explication ;
- signalements de classement trompeur ;
- erreurs de cache ;
- distribution des niveaux de preuve côté métriques internes agrégées.

### 4.4 Partage

- aperçu demandé ;
- publication confirmée ;
- révocation ;
- ouverture ;
- conversion vers création de Passeport ;
- partages dont la politique est modifiée après aperçu ;
- aucune optimisation du taux de publication au détriment de la confidentialité.

### 4.5 Recommandation

- recherche démarrée/terminée ;
- zéro résultat ;
- données inconnues ;
- explications ouvertes ;
- comparaison ;
- ajout à wishlist/projet ;
- correction de données ;
- résultat abandonné.

### 4.6 Mesures à ne pas utiliser seules

- nombre brut d’inscriptions ;
- nombre brut de partages ;
- temps passé ;
- nombre de notifications ;
- taux de clic e-mail ;
- nombre de rides ;
- nombre de badges ;
- volume de données collectées.

## 5. Plan d’événements

## 5.1 Convention

- noms en anglais technique stables ;
- propriétés documentées ;
- version de schéma ;
- propriétaire ;
- finalité ;
- durée de conservation ;
- classification de sensibilité ;
- tests ;
- date de retrait.

Exemple :

```json
{
  "event": "visit_completed",
  "schemaVersion": 1,
  "properties": {
    "entryCountBucket": "5-9",
    "hasTemporalRating": true,
    "datePrecision": "Day",
    "source": "park-page"
  }
}
```

Ne pas transmettre : `VisitId`, `ParkId`, liste des éléments, commentaire, valeur de note, date exacte, alias de profil, share id.

## 5.2 Catalogue initial

### RANK

- `ranking_methodology_opened` ;
- `ranking_evidence_details_opened` ;
- `provisional_rating_state_seen` ;
- `ranking_data_issue_reported`.

### PASS

- `passport_opened` ;
- `visit_creation_started` ;
- `visit_created` ;
- `visit_completed` ;
- `visit_reopened` ;
- `ride_occurrence_added` ;
- `temporal_rating_added` ;
- `target_timeline_opened` ;
- `second_visit_recorded` ;
- `passport_export_requested` ;
- `passport_deletion_started/completed`.

### SHARE

- `share_activation_started` ;
- `share_preview_created` ;
- `share_published` ;
- `share_revoked` ;
- `share_rotated` ;
- `share_opened` ;
- `share_cta_passport_started` ;
- `share_render_failed`.

Pour SHARE, le démarrage d'activation correspond à l'entrée dans la préparation
d'un partage. Sa réussite correspond à `share_published`, émis seulement après la
confirmation de l'API. Le label autorisé est limité à la famille de récit ; aucun
identifiant ni contenu du partage n'est une propriété analytics.

### FIT/WATCH/TRIP/HIST/LIVE

Événements listés dans chaque roadmap, harmonisés dans ce catalogue avant code.

## 5.3 Collecte

Options à arbitrer :

- logs applicatifs agrégés ;
- solution analytics respectueuse ;
- auto-hébergement proportionné ;
- consentement selon cookies et finalité ;
- métriques serveur non personnelles pour fiabilité.

Le choix fait l’objet d’un ADR et d’une mise à jour des pages de confidentialité/cookies.

## 6. Entrepôt minimal et rapports

Première version :

- événements produits bornés ;
- agrégations journalières ;
- cohortes pseudonymisées si nécessaire ;
- rapports simples ;
- aucune duplication brute longue sans besoin ;
- accès admin restreint ;
- export interne ;
- purge testée.

Rapports :

- funnel activation ;
- cohorte deuxième visite ;
- erreurs ;
- temps de réponse ;
- données manquantes ;
- partages ;
- correction/signalement ;
- feature flags ;
- comparaison avant/après déploiement.

## 7. Recherche utilisateur

## 7.1 Recrutement

Cohortes distinctes :

- passionnés tenant déjà un journal ;
- passionnés sans outil ;
- visiteurs occasionnels ;
- familles préparant une sortie ;
- utilisateurs de lecteurs d’écran/clavier ;
- utilisateurs sur connexion/appareil modeste ;
- contributeurs historiques.

Ne pas prétendre qu’un groupe de cinq passionnés représente tout le public.

## 7.2 Protocoles

- tâches concrètes ;
- observation sans guider immédiatement ;
- compréhension de la distinction des notes ;
- relecture des libellés ;
- test de suppression/export ;
- test de données inconnues ;
- retour différé après visite ;
- consentement et traitement des enregistrements de test.

## 7.3 Synthèse

Pour chaque test :

- objectif ;
- profil ;
- scénario ;
- faits observés ;
- citations courtes autorisées ;
- problèmes ;
- sévérité ;
- hypothèses ;
- décisions ;
- éléments non conclusifs.

Ne pas transformer un avis isolé en vérité produit.

## 8. Gates de beta

### 8.1 Alpha interne

- données de test ;
- aucune publication publique ;
- instrumentation ;
- erreurs visibles ;
- export/suppression ;
- feature flag admin.

### 8.2 Beta fermée

- invitation manuelle ;
- données réelles ;
- consentement clair ;
- support direct ;
- migrations réversibles ;
- limites connues ;
- mesure qualitative.

### 8.3 Beta ouverte limitée

- capacité VPS ;
- support ;
- modération ;
- monitoring ;
- documentation ;
- incident response ;
- pas de promesse de disponibilité excessive.

### 8.4 Généralisation

- valeur répétée observée ;
- erreurs sous seuil ;
- confidentialité validée ;
- accessibilité ;
- huit langues ;
- performance ;
- coûts ;
- retrait des flags temporaires ;
- runbook.

## 9. Conditions d’arrêt

Une phase est arrêtée, réduite ou redessinée si :

- les utilisateurs ciblés ne comprennent pas la valeur après tests répétés ;
- la deuxième utilisation reste inexistante malgré activation réussie ;
- les données requises ne peuvent pas être obtenues honnêtement ;
- la modération dépasse les moyens ;
- les coûts ou la charge sont disproportionnés ;
- la confidentialité exige une complexité non justifiée ;
- l’accessibilité fondamentale ne peut pas être assurée ;
- la fonction produit principalement des erreurs ou de la défiance ;
- une source live devient juridiquement ou techniquement indisponible ;
- un modèle prédictif ne bat pas une baseline simple.

Arrêter n’est pas un échec technique : c’est une gate prévue.

## 10. Feature flags

## 10.1 Modèle

Chaque flag possède :

- clé ;
- description ;
- owner ;
- date de création ;
- date cible de retrait ;
- défaut ;
- environnements ;
- cohortes ;
- dépendances ;
- métriques ;
- kill switch ;
- fallback ;
- procédure de nettoyage.

### 10.2 Types

- release flag ;
- operational kill switch ;
- permission/capability ;
- expérimentation — seulement si volume/méthode ;
- data gate par parc/source.

Ne pas utiliser un flag permanent à la place d’une règle métier ou d’un contrat versionné.

### 10.3 Évaluation

- côté serveur pour autorité ;
- front reçoit capacités ;
- pas de sécurité uniquement côté UI ;
- cache court ;
- valeurs par défaut embarquées ;
- état de panne sûr ;
- audit des changements.

## 11. Migrations et compatibilité

Chaque nouvelle persistance inclut :

- schéma/version ;
- index ;
- backfill ;
- reprise ;
- idempotence ;
- mesure de durée ;
- charge ;
- rollback ;
- compatibilité ancien code/nouveau schéma ;
- ordre déploiement API/front ;
- validation post-déploiement.

### 11.1 Expand/contract

1. ajouter champs/collections ;
2. déployer écriture compatible ;
3. backfill ;
4. lire nouveau avec fallback ;
5. mesurer ;
6. couper ancien chemin ;
7. attendre ;
8. supprimer seulement dans une PR dédiée.

### 11.2 Données utilisateur

- pas de migration irréversible sans export/backup ;
- pas de visite synthétique ;
- pas de nouvelle visibilité par migration ;
- pas de consentement présumé ;
- journal et contrôle d’intégrité.

## 12. Rollback

Chaque tranche documente :

- flag à couper ;
- endpoints à désactiver ;
- lectures compatibles ;
- données créées ;
- purge ou conservation ;
- cache ;
- jobs ;
- rollback de schéma ;
- communication utilisateur ;
- critères de réactivation.

Le rollback ne doit pas :

- réexposer un classement faible ;
- rendre publiques des données ;
- supprimer des visites ;
- répéter des notifications ;
- réutiliser une source live expirée.

## 13. Accessibilité

### 13.1 Standard minimum

- WCAG cible à préciser, au moins conformité raisonnable AA ;
- clavier complet ;
- focus ;
- labels ;
- messages d’erreur ;
- contraste ;
- zoom ;
- texte redimensionnable ;
- lecteur d’écran ;
- reduced motion ;
- aucune information par couleur seule ;
- ordre DOM ;
- tableaux alternatifs aux graphiques.

### 13.2 Tests

- unitaires lorsque possible ;
- axe automatisé ;
- navigation manuelle clavier ;
- lecteur d’écran sur parcours clés ;
- mobile ;
- formulaires longs ;
- modales ;
- drag-and-drop avec alternative ;
- huit langues et textes longs.

### 13.3 Définition de terminé

Une fonctionnalité n’est pas terminée si l’action principale nécessite souris, couleur, hover ou graphique seul.

## 14. Internationalisation

- huit langues ;
- codes métier traduits côté front/contenu ;
- aucun texte public persisté dans une seule langue sans stratégie ;
- dates partielles localisées ;
- pluriels ;
- nombres ;
- fuseaux ;
- unités ;
- contenus de méthode ;
- e-mails ;
- Open Graph ;
- fallback indiqué ;
- tests de clés manquantes et de longueurs.

Une méthode statistique n’est pas copiée huit fois avec des nombres divergents : les paramètres viennent d’un contrat versionné, les explications sont traduites.

## 15. Performance

### 15.1 Budgets Web

Pour chaque nouvelle page :

- poids JS initial ;
- lazy loading ;
- images ;
- LCP/CLS/INP ;
- SSR ;
- nombre d’appels ;
- cache ;
- appareil/réseau de référence ;
- listes volumineuses ;
- mémoire.

### 15.2 API/VPS

- latence p50/p95 ;
- CPU ;
- mémoire ;
- requêtes ;
- fan-out ;
- indexes ;
- scans ;
- jobs ;
- cache ;
- quotas ;
- limite par utilisateur ;
- tests volumétriques.

### 15.3 Protection

- pagination cursorisée ;
- batch borné ;
- output cache public ;
- cache privé prudent ;
- projections ;
- aucun N+1 ;
- backpressure ;
- circuit breaker ;
- kill switch ;
- alerting.

## 16. Sécurité

- threat model par capacité ;
- auth/ownership Application ;
- rate limits ;
- idempotence ;
- validation ;
- taille payload ;
- XSS ;
- CSRF selon auth ;
- secrets ;
- SSRF sources ;
- export ;
- liens opaques ;
- logs ;
- audit ;
- dependency scans ;
- tests d’autorisation cross-user ;
- suppression et purge.

### 16.1 Scénarios critiques

- deviner une visite ;
- accéder au partage après révocation ;
- accepter deux fois une invitation ;
- modifier une note d’un autre ;
- multiplier les rides par retry ;
- déclencher massivement des e-mails ;
- injecter contenu dans caption/note ;
- exfiltrer profil de groupe ;
- forcer SSR à lire une ressource privée ;
- polluer mapping live.

## 17. Confidentialité par conception

Pour chaque champ :

- finalité ;
- nécessité ;
- visibilité ;
- base/consentement selon cas ;
- rétention ;
- export ;
- suppression ;
- sous-traitant ;
- analytics ;
- chiffrement ;
- accès support ;
- journal.

### 17.1 Paramètres par défaut

- visite privée ;
- note temporelle privée ;
- profil de groupe privé ;
- voyage privé ;
- partage désactivé ;
- indexation désactivée ;
- e-mail désactivé ;
- localisation non demandée ;
- commentaire privé jamais recopié.

### 17.2 Suppression

Créer des tests automatisés de graphe de suppression :

- compte ;
- visites ;
- occurrences ;
- assessments ;
- stats ;
- partages ;
- images ;
- invitations ;
- voyages ;
- notifications ;
- exports ;
- caches ;
- audit minimisé.

## 18. Probité produit et anti-dark-pattern

Interdictions :

- compte obligatoire avant un premier résultat lorsqu’il n’est pas techniquement nécessaire ;
- case marketing précochée ;
- bouton de refus caché ;
- faux compte à rebours ;
- notification alarmiste ;
- publication automatique ;
- classement sponsorisé non identifié ;
- compatibilité présentée comme garantie ;
- prédiction sans intervalle ;
- badge culpabilisant ;
- perte de série ;
- faux nombre d’utilisateurs ;
- avis synthétique présenté comme réel ;
- données manquantes imputées positivement.

Exigences :

- raison de la demande de compte ;
- bénéfice concret ;
- choix réversible ;
- source ;
- volume ;
- limite ;
- confirmation ;
- alternative manuelle.

## 19. Administration et support

Un panneau opérationnel par module, pas un gigantesque dashboard :

- état ;
- flags ;
- dernière erreur ;
- volumes ;
- jobs ;
- incohérences ;
- signalements ;
- sources ;
- recomputation ;
- export diagnostic ;
- actions auditables ;
- accès limité.

Runbooks :

- classement incohérent ;
- visite orpheline ;
- partage révoqué encore caché ;
- notification dupliquée ;
- source live en panne ;
- export bloqué ;
- purge ;
- incident de confidentialité ;
- rollback.

## 20. Stratégie de test globale

### 20.1 Pyramide

- Core : invariants purs ;
- Application : cas d’usage et autorisation ;
- Infrastructure : Mongo, cache, jobs ;
- WebAPI : contrats, Problem Details, auth ;
- Angular : façades, composants, i18n, accessibilité ;
- E2E : parcours critiques ;
- performance : scénarios volumétriques ;
- sécurité : cross-user et abus ;
- tests qualitatifs.

### 20.2 Données de test

Fixtures :

- parc riche/petit ;
- élément ouvert/fermé/renommé ;
- dates partielles ;
- 0/1/2/3/9/10/30/100 contributeurs ;
- 1/3/100 rides ;
- visite ancienne ;
- données inconnues ;
- source contradictoire ;
- utilisateur supprimé ;
- langues ;
- contenus longs ;
- timezone/DST.

### 20.3 Tests de référence statistiques

Calculer indépendamment des résultats attendus, stocker fixtures et documenter :

- moyenne ;
- médiane ;
- dispersion ;
- bayésien ;
- seuil ;
- rang ;
- égalité ;
- tendance ;
- couverture.

## 21. CI/CD

Pour chaque PR :

- format/lint ;
- build API/front ;
- tests ;
- OpenAPI diff ;
- indexes/migrations vérifiés ;
- i18n ;
- accessibilité automatisée ;
- scans dépendances ;
- taille bundle ;
- docs/liens ;
- version release ;
- preview environnement si disponible.

Avant merge d’une phase :

- checks verts ;
- gate documentée ;
- rollout/rollback ;
- feature flag ;
- monitoring ;
- support.

## 22. Documentation

À maintenir :

- roadmap ;
- ADR ;
- méthode publique ;
- contrats ;
- modèles ;
- événements ;
- confidentialité ;
- runbooks ;
- migrations ;
- changelog ;
- limites ;
- décisions abandonnées avec raison.

Une roadmap n’est pas automatiquement mise à jour par le code : chaque phase prévoit une PR de clôture documentaire.

#### État de généralisation de SHARE au 14 septembre 2026

SHARE a franchi son gate technique en version 5.3.13. Son flag de préversion a été
retiré sur toute la chaîne de configuration, sans créer d'adaptateur ni conserver
de second mode. Les preuves de confidentialité, sécurité, responsive, export,
suppression, observabilité et rollback sont regroupées dans
[`product-growth-share-15-general-availability-2026-09-14.md`](../../architecture/product-growth-share-15-general-availability-2026-09-14.md).
La cohorte communautaire n'est pas exigée par la décision produit et aucune preuve
d'usage réel n'est donc revendiquée.

## 23. Cadence de revue

- après chaque gate ;
- après incident ;
- après changement de méthode ;
- après évolution majeure de source ;
- au moins trimestrielle pendant développement actif ;
- annuelle pour rétention/confidentialité ;
- retrait des flags et champs obsolètes.

## 24. Découpage recommandé en PR

| PR | Contenu | Critère |
|---|---|---|
| [`QUAL-01`](../../architecture/product-growth-qual-01-analytics-event-plan-2026-09-29.md) | ADR analytics et plan d’événements — livré le 29 septembre 2026 | Finalités/minimisation validées |
| [`QUAL-02`](../../architecture/product-growth-qual-02-feature-flags-2026-09-29.md) | Infrastructure feature flags — livré le 29 septembre 2026 | Fallback/kill switch |
| [`QUAL-03`](../../architecture/product-growth-qual-03-performance-error-baseline-2026-09-29.md) | Baseline performance/erreurs — livré le 29 septembre 2026 | État avant produit connu |
| [`QUAL-04`](../../architecture/product-growth-qual-04-privacy-export-deletion-matrix-2026-09-29.md) | Matrice privacy et export/suppression — livré le 29 septembre 2026 | Champs catalogués |
| [`QUAL-05`](../../architecture/product-growth-qual-05-typed-product-analytics-2026-09-29.md) | Helpers d’instrumentation typés — livré le 29 septembre 2026 | Pas d’événements ad hoc |
| [`QUAL-06`](../../architecture/product-growth-qual-06-product-decision-dashboards-2026-09-29.md) | Dashboards funnel/fiabilité — livré le 29 septembre 2026 | Questions utiles uniquement |
| `QUAL-07` | Automatisation accessibilité/i18n | Régressions détectées |
| `QUAL-08` | Tests cross-user et sécurité | Parcours critiques |
| `QUAL-09` | Runbooks et alerting | Incidents opérables |
| `QUAL-10` | Protocole beta/recherche | Tests comparables |
| `QUAL-11+` | Tranche transverse par roadmap | Gate locale documentée |

### Implémentation `QUAL-01` — 29 septembre 2026

La mesure produit est désormais organisée en trois canaux qui ne peuvent pas être
confondus : événements Matomo consentis, agrégats métier first-party et métriques
techniques d’exploitation. Le catalogue canonique couvre les huit familles
`RANK`, `PASS`, `SHARE`, `FIT`, `WATCH`, `TRIP`, `HIST` et `LIVE`, fixe les
instants d’émission, les propriétés fermées et les données interdites.

Le jalon conserve les ports Matomo et agrégats existants sans créer de second
moteur. Il prépare leur migration directe vers les helpers communs de `QUAL-05`,
borne la conservation à 180 jours pour les événements tiers bruts et réserve les
cohortes de valeur récurrente aux calculs internes minimisés. Aucun schéma MongoDB
ni comportement public n’est modifié par cet ADR.

### Implémentation `QUAL-02` — 29 septembre 2026

Le moteur commun de feature flags porte désormais une définition versionnée,
une date de retrait, un propriétaire, un défaut, un repli sûr, des dépendances et
des critères de nettoyage. Les overrides opérationnels sont des révisions MongoDB
immuables protégées par concurrence optimiste, cache court et audit administrateur.

Le premier branchement `live:public-experience` permet de couper attentes,
historique et prévision sans supprimer les observations et sans casser les fiches
parc ou attraction. La panne du stockage ferme cette capacité par sécurité. Le
client public ne reçoit que la clé exposable et son état booléen ; les raisons,
auteurs et détails d'administration restent privés.

### Implémentation `QUAL-03` — 29 septembre 2026

La production `5.4.21` fournit le point zéro de six cibles publiques avec cinq
échantillons mesurés après échauffement : trente réponses réussies, p95 de 16 à
38 ms et modes SSR explicitement relevés. Park Fit apparaît honnêtement en repli
CSR, contrairement aux trois autres pages servies depuis le cache SSR.

La CI bloque désormais les hausses non décidées du chargement initial, du plus gros
chunk différé, du JavaScript total et du CSS total. Les logs API possèdent des
familles de résultat et des EventId stables, emploient le modèle de route et ne
recopient plus query string ni user-agent. Aucune persistance ni dépendance n'est
ajoutée ; les futurs dashboards réutiliseront ces signaux au lieu de créer une
seconde observabilité.

### Implémentation `QUAL-04` — 29 septembre 2026

Le registre versionné de confidentialité applique les douze dimensions de cette
roadmap à huit surfaces métier, 116 documents et 1 105 champs persistés. Il décrit
les visibilités privées ou explicitement publiées, les exports réellement couverts,
les rétentions, les sous-traitants et les accès support sans présenter les
capacités partielles comme terminées.

La CI relit les documents MongoDB et refuse une forme persistée modifiée sans revue
du registre. Elle détecte également les documents portant des marqueurs probables
de données personnelles et exige leur classement ou une exclusion justifiée. Le
constat principal est volontairement explicite : Partage et Alertes possèdent déjà
des participants de purge solides, mais l'export de compte fédéré et le coordinateur
global de suppression restent à construire avant d'exposer une promesse complète
au membre.

### Implémentation `QUAL-05` — 29 septembre 2026

Les événements produit PASS et SHARE utilisent désormais un port et un adaptateur
Matomo communs. Le contrat v1 couvre 54 événements fermés des huit familles et
rejette toute propriété manquante, supplémentaire ou invalide avant émission. Le
consentement, l'exclusion SSR et l'absence d'identifiants métier sont testés. Une
garde CI interdit le retour des anciens ports, la création d'un second adaptateur
ou la construction d'une requête Matomo depuis une feature. Les familles non
activées restent un vocabulaire réservé : ce jalon ne crée pas de nouvelle collecte
ni de stockage MongoDB.

### Implémentation `QUAL-06` — 29 septembre 2026

L'administration propose désormais une entrée décisionnelle commune aux huit
programmes. Chaque carte expose la question métier à trancher, les preuves
disponibles, leur canal, leur maturité et la limite qui interdit de surinterpréter
un volume brut. Elle ouvre ensuite le panneau opérationnel spécialisé déjà en
place, sans recopier ses chiffres ni créer un dashboard monolithique.

L'observatoire ne collecte rien et n'interroge aucune API : son manifeste typé ne
contient que les routes et les classifications de preuve. Les sources consenties,
les agrégats first-party et les métriques techniques restent séparés. L'écran est
protégé par les gardes admin existants, localisé en huit langues et conçu pour
rester contenu jusqu'aux mobiles de 360 pixels et au paysage de faible hauteur.

## 25. Checklist de gate pour toute fonctionnalité

### Produit

- [ ] Problème utilisateur identifié.
- [ ] Non-objectifs écrits.
- [ ] Premier succès défini.
- [ ] Condition d’arrêt définie.
- [ ] Test qualitatif effectué.

### Données et probité

- [ ] Sources et fraîcheur visibles.
- [ ] Inconnues distinctes.
- [ ] Volumes/dénominateurs affichés.
- [ ] Aucun score présenté comme probabilité sans fondement.
- [ ] Aucun partenariat dans le calcul.

### Technique

- [ ] Invariants Core testés.
- [ ] Ownership Application testée.
- [ ] Indexes et volumétrie.
- [ ] Idempotence/concurrence.
- [ ] Cache/invalidation.
- [ ] Observabilité.
- [ ] Rollback.

### Confidentialité

- [ ] Privé par défaut.
- [ ] Export.
- [ ] Suppression.
- [ ] Rétention.
- [ ] Consentement si nécessaire.
- [ ] Analytics minimisés.

### Expérience

- [ ] Responsive.
- [ ] Clavier/lecteur d’écran.
- [ ] Huit langues.
- [ ] Erreur/reprise.
- [ ] Aucun dark pattern.

### Exploitation

- [ ] Feature flag.
- [ ] Kill switch si externe.
- [ ] Admin/diagnostic minimal.
- [ ] Runbook.
- [ ] Charge et coût acceptés.

## 26. Gate finale `QUAL-G`

Le programme global ne peut être considéré comme réussi que si :

- les classements faibles sont présentés honnêtement ;
- le Passeport produit une seconde utilisation réelle chez une cohorte ciblée ;
- les observations temporelles ne gonflent jamais le vote communautaire ;
- les utilisateurs comprennent les différences entre note globale, note de visite et note de ride ;
- les données privées restent privées par défaut ;
- les partages sont contrôlables et révocables ;
- les recommandations expliquent leurs inconnues ;
- les alertes restent factuelles ;
- les voyages respectent les participants ;
- les historiques affichent leur couverture ;
- le live peut être arrêté sans casser le produit principal ;
- export, suppression, accessibilité, i18n, performance et support sont effectifs ;
- les métriques servent à apprendre, pas à justifier une manipulation ;
- le projet accepte qu’une fonction très travaillée puisse être abandonnée si sa valeur n’est pas démontrée.
