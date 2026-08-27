# Roadmap 09 — Qualité produit, confidentialité, instrumentation et déploiement

> Code programme : `QUAL`
>
> Statut : transverse. Cette roadmap commence avant `RANK-01` et accompagne toutes les autres.
>
> Principe : une fonctionnalité n’est pas validée parce qu’elle compile ou parce qu’elle augmente un compteur. Elle doit être comprise, utile, accessible, fiable, réversible, respectueuse de la vie privée et supportable avec les moyens réels du projet.

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

- `share_preview_opened` ;
- `share_published` ;
- `share_revoked` ;
- `share_opened` ;
- `share_cta_passport_started`.

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
| `QUAL-01` | ADR analytics et plan d’événements | Finalités/minimisation validées |
| `QUAL-02` | Infrastructure feature flags | Fallback/kill switch |
| `QUAL-03` | Baseline performance/erreurs | État avant produit connu |
| `QUAL-04` | Matrice privacy et export/suppression | Champs catalogués |
| `QUAL-05` | Helpers d’instrumentation typés | Pas d’événements ad hoc |
| `QUAL-06` | Dashboards funnel/fiabilité | Questions utiles uniquement |
| `QUAL-07` | Automatisation accessibilité/i18n | Régressions détectées |
| `QUAL-08` | Tests cross-user et sécurité | Parcours critiques |
| `QUAL-09` | Runbooks et alerting | Incidents opérables |
| `QUAL-10` | Protocole beta/recherche | Tests comparables |
| `QUAL-11+` | Tranche transverse par roadmap | Gate locale documentée |

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
