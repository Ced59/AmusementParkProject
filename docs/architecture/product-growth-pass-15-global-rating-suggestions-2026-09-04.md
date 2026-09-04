# PASS-15 — Suggestions explicables de note globale

Date : 2026-09-04
Version : 5.0.17

## Résultat livré

PASS-15 rapproche les notes globales courantes des observations temporelles privées du passeport. Le système peut inviter une personne à revoir volontairement une note lorsque ses expériences récentes s'en éloignent nettement. Il ne crée, ne modifie et ne supprime aucune note globale.

La suggestion est disponible dans le panneau « Mes notes » du profil. Elle affiche la cible, la note globale actuelle, la dernière observation, la moyenne récente, la médiane historique et une raison lisible. « Revoir ma note » replace l'utilisateur dans l'éditeur de notes existant ; seul cet éditeur conserve le droit d'envoyer ultérieurement une valeur choisie par l'utilisateur.

## Politique de domaine

`GlobalRatingSuggestionPolicy` est pure et déterministe. Elle exige simultanément :

- une note globale valide ;
- au moins deux observations créées ou corrigées après la dernière modification de cette note globale ;
- un écart absolu d'au moins un point entre la note globale et la moyenne des trois observations nouvelles les plus récentes ;
- l'absence de présentation pour cette cible pendant les 30 derniers jours ;
- l'activation de la préférence utilisateur et du kill switch serveur.

Une présentation reste résoluble pendant 24 heures. Une acceptation ou un rejet sans présentation active est refusé ; rejouer exactement une transition déjà résolue réussit sans nouvel événement analytique.

La médiane est calculée sur toutes les observations valides de la cible. La moyenne récente est calculée sur au plus trois observations nouvelles. Ces deux valeurs restent distinctes et sont nommées explicitement dans l'interface.

```text
note globale + timestamp
           │
           ├── observations temporelles privées
           │       ├── au moins 2 postérieures ?
           │       ├── moyenne récente (3 max.)
           │       └── médiane historique
           │
           └── cadence + préférences + kill switch
                   │
                   └── suggestion expliquée ou aucune suggestion
```

La politique n'a aucune dépendance vers un repository de notes et son résultat ne contient aucune commande de mutation. Les tests Core prouvent les seuils, le sens de l'écart, l'opt-out et la cadence.

## Architecture applicative

```text
ProfileRatingsPanelComponent
  └─ GlobalRatingSuggestionsComponent
       └─ GlobalRatingSuggestionsStateFacade
            └─ GLOBAL_RATING_SUGGESTIONS_API_PORT
                 └─ GlobalRatingSuggestionsApiService
                      └─ API privée /me/passport/rating-update-suggestions

WebAPI
  └─ handlers Application
       ├─ IGlobalRatingSuggestionSourceReader
       ├─ IGlobalRatingSuggestionStateRepository
       ├─ IGlobalRatingSuggestionFeatureGate
       ├─ IParkRepository / IParkItemRepository
       └─ GlobalRatingSuggestionPolicy (Core)
```

La WebAPI extrait l'identité authentifiée et mappe les DTO. Les handlers normalisent les identifiants, orchestrent les ports et écartent les cibles qui ne peuvent plus recevoir de note. Infrastructure projette uniquement les champs Mongo utiles. Angular ne calcule ni seuil, ni éligibilité : le mapper frontend ne fait que localiser les nombres et les libellés.

## Contrats privés

- `GET /api/me/passport/rating-update-suggestions` : suggestions éligibles, préférence et paramètres publics de cadence ;
- `POST /api/me/passport/rating-update-suggestions/interactions` : présentation, acceptation ou rejet explicite ;
- `PUT /api/me/passport/rating-update-suggestions/preference` : activation ou désactivation par la personne.

Les trois endpoints exigent un compte actif non bloqué, portent `no-store` et ne sont jamais transférés dans le cache SSR. Une interaction est rejetée si la note globale n'appartient plus à l'utilisateur.

## Persistance et minimisation

Trois collections bornent clairement les responsabilités :

- `global-rating-suggestion-states` : une ligne unique par utilisateur, type de cible et cible, avec seulement les dates de présentation, acceptation et rejet ;
- `global-rating-suggestion-preferences` : une préférence unique par utilisateur ;
- `global-rating-suggestion-interactions` : événements analytiques conservés 400 jours au maximum.

L'événement analytique ne contient ni identifiant de cible ni valeur de note. Il conserve seulement une clé de cohorte utilisateur hachée, le type de cible, l'action et sa date. Les index uniques empêchent les états dupliqués ; un index TTL purge automatiquement les événements analytiques. La transition Mongo compare atomiquement la dernière présentation attendue et l'état `isAwaitingResolution`, ce qui empêche deux requêtes concurrentes ou rejouées de produire deux événements.

Les observations de parc proviennent des assessments embarqués dans les visites. Les observations d'attraction proviennent des assessments de rides non supprimés et ne sont lues que si leur fence de contenu correspond à la visite parente. Elles sont indexées une seule fois par parc et attraction avant l'évaluation des notes, afin de conserver une construction linéaire plutôt qu'un rescan par cible.

## Cadence et choix utilisateur

La réception d'une suggestion visible demande l'enregistrement d'une présentation. Le serveur recalcule alors l'éligibilité et vérifie encore que la cible peut être notée avant d'ouvrir la fenêtre d'interaction. L'acceptation et le rejet sont deux actions distinctes. Une acceptation signifie uniquement « conduire vers l'éditeur existant » ; elle ne transporte aucune nouvelle valeur. Pour une attraction, le filtre recherche son nom exact afin de faire apparaître la note visée dès la première page. Une personne peut désactiver l'ensemble des suggestions et les réactiver depuis le même panneau.

Le flag `Features:Passport:GlobalRatingSuggestions:Enabled`, activé par défaut, appartient au domaine produit Passeport. Son comportement de repli est l'absence de toute suggestion, sans effet sur les notes ni sur le journal. Il sert de kill switch et doit être réévalué à la stabilisation PASS-20.

## Responsive et accessibilité

- toutes les grilles et leurs enfants utilisent `min-width: 0` et `max-width: 100%` ;
- les textes longs peuvent se couper avec `overflow-wrap: anywhere` ;
- les trois suggestions passent sur une colonne sous 1100 px ;
- sous 520 px, en-tête, métriques et actions s'empilent, chaque action occupant toute la largeur ;
- sous 340 px, l'icône d'en-tête passe au-dessus du titre ;
- le composant borne ses descendants avec `overflow-x: clip` ;
- les états de chargement et d'erreur sont annoncés, les boutons sont de vrais contrôles clavier.

## Preuves automatisées

- Core : seuil minimal, écart significatif, médiane, sens de la suggestion, cooldown, fenêtre d'interaction et désactivation ;
- Application : orchestration, kill switch sans lecture des observations, résolution de cible, présentation revalidée, propriété et rejeu idempotent ;
- Infrastructure : séparation parc/ride, indexation linéaire des observations, fence de contenu, rejet des données invalides, transitions atomiques, index uniques, TTL et absence de valeurs exactes dans l'analytics ;
- WebAPI : identité authentifiée, mapping, route privée `no-store` et contrat d'interaction sans valeur de note ;
- Angular : endpoints, port de façade, mapping localisé, présentation, acceptation, rejet, opt-out et contrat responsive.
