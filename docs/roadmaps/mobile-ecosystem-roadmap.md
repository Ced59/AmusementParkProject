# Amusement Parks Fun — Roadmap de l’écosystème Web et Mobile

> Statut : proposition révisée, prête à être arbitrée — aucune phase mobile n’est engagée par ce document.
>
> Base technique auditée : `origin/master` au commit `36d69ab9`, le 10 août 2026.
>
> Responsable produit : à désigner avant le démarrage de la phase A.
>
> Responsable technique : à désigner avant le démarrage de la phase A.
>
> Prochaine revue : à planifier au plus tard trois mois après validation, puis à chaque fin de phase.

## 1. Objet du document

Cette roadmap décrit comment faire évoluer Amusement Parks Fun d’un site Web éditorial vers un écosystème cohérent comprenant une application mobile native Android et iOS.

Elle sert à :

- fixer la complémentarité entre le Web et le Mobile ;
- rendre visibles les décisions qui restent à prendre ;
- ordonner les travaux selon leurs dépendances réelles ;
- borner un premier pilote utile et mesurable ;
- protéger l’architecture, les données, la vie privée, la batterie et le VPS ;
- conserver les idées à long terme sans les confondre avec des engagements.

Elle ne constitue ni une spécification détaillée, ni un calendrier, ni une autorisation d’implémenter l’ensemble des fonctions listées. Chaque phase devra être découpée en petites PR indépendantes, testables et réversibles.

## 2. Vision produit

### 2.1 Rôle du Web

Le Web reste le point d’entrée public, partageable et indexable :

- découvrir les parcs et attractions ;
- consulter des contenus éditoriaux riches et localisés ;
- comparer, rechercher et préparer une visite ;
- partager des pages stables ;
- administrer et enrichir les données ;
- convertir, lorsque cela apporte une vraie valeur, vers l’application.

Ses exigences SEO, SSR, accessibilité, performance et internationalisation restent indépendantes du projet mobile.

### 2.2 Rôle du Mobile

L’application doit être un compagnon personnel avant, pendant et après la visite :

- retrouver hors ligne les informations utiles d’un parc ;
- se repérer sur place avec une carte et une localisation maîtrisée ;
- démarrer une session de visite et noter ce qui a été fait ;
- recevoir des suggestions explicables et facultatives ;
- synchroniser plus tard les favoris, visites et souvenirs avec un compte ;
- ouvrir directement un contenu depuis un lien Web.

Le Mobile ne doit pas être une copie du site dans une WebView. Sa valeur vient du contexte terrain, du fonctionnement hors ligne, des fonctions natives et d’une interaction rapide à une main.

### 2.3 Proposition de valeur prioritaire

La proposition de valeur à valider en premier est :

> « Dans un parc, je retrouve rapidement où je suis, ce qui m’entoure et ce que j’ai déjà fait, même avec un réseau médiocre. »

Les fonctions sociales, prédictives, partenaires et immersives restent secondaires tant que cette promesse n’est pas démontrée.

### 2.4 Publics et besoins

| Public | Besoin principal | Réponse envisagée | Priorité initiale |
|---|---|---|---|
| Visiteur occasionnel | Trouver une information fiable sur place | Fiche native, carte hors ligne, position en premier plan | P0 |
| Passionné récurrent | Conserver son historique | Session de visite puis carnet synchronisé | P1 |
| Famille ou groupe | Tenir compte de contraintes communes | Profils, accessibilité et groupe après validation du socle | P2 |
| Contributeur autorisé | Mettre à jour une donnée sur le terrain | Extension du Mode Terrain existant | P2 |
| Partenaire parc | Partager des données officielles | Contrats et portail partenaires ultérieurs | P3 |

### 2.5 Non-objectifs du premier pilote

Le premier pilote n’inclut pas :

- l’authentification et la synchronisation multi-appareils ;
- les temps d’attente communautaires ou estimés ;
- la navigation piétonne virage par virage ;
- la localisation en arrière-plan ;
- les notifications push ;
- les groupes, la gamification, les widgets ou les wearables ;
- l’AR, les beacons, le NFC ou un programme partenaire ;
- une couverture immédiate de tous les parcs.

## 3. Indicateurs de succès

Les valeurs cibles seront arrêtées pendant la phase A à partir d’une mesure de référence. Les indicateurs minimums sont :

- taux d’installation réussie du pack hors ligne ;
- taux d’ouverture d’une fiche ou de la carte sans réseau ;
- temps jusqu’à la première information utile pendant une visite ;
- stabilité des sessions, ANR Android et crashs iOS ;
- consommation batterie et données sur un appareil de référence ;
- taux de réussite et délai de synchronisation lorsqu’elle sera introduite ;
- usage répété pendant une même visite puis lors d’une visite suivante ;
- taux de refus ou de révocation des permissions de localisation ;
- signalements de données erronées et délai de correction ;
- satisfaction qualitative des bêta-testeurs.

Des métriques de vanité comme le nombre brut de téléchargements ne suffisent pas à valider la proposition de valeur.

## 4. État actuel du dépôt

Cette base évite de planifier comme si tout était à créer.

| Capacité | État au 9 août 2026 | Conséquence pour la roadmap |
|---|---|---|
| Backend .NET 10 en Clean Architecture | Existant | Étendre les couches actuelles, sans domaine mobile parallèle côté serveur |
| API par contrôleurs | Existant | Conserver les contrôleurs et routes publiques ; aucun basculement global vers des minimal APIs |
| OpenAPI 3.1, document `v1` | Existant | Industrialiser le contrat et sa compatibilité, sans présenter OpenAPI comme une création nette |
| Problem Details, compression, output cache | Existant | Réutiliser et mesurer avant d’ajouter de nouveaux mécanismes |
| Parcs, zones et éléments géolocalisés | Existant | Auditer la précision, la complétude et les droits avant usage mobile |
| Plusieurs types d’entrées d’attraction | Existant | Bonne base pour la carte ; ne garantit pas un graphe piéton exploitable |
| Recherche géographique, proximité, météo et horaires | Partiel à existant | Réutiliser les services d’application au lieu de créer une API mobile concurrente |
| JWT, rotation de refresh token et connexion Google Web | Partiel | Ne constitue pas à lui seul un flux OAuth/OIDC natif ; décision d’identité obligatoire |
| Mode Terrain administrateur avec GPS et photos | Existant | ECO-E04 devra l’étendre, pas le dupliquer |
| Favoris, visites, plans, carnet et sessions d’appareil | Absent | Nouvelles capacités métier à introduire progressivement |
| Push, temps d’attente, statut live et contributions | Absent | Dépendent de la provenance, de la modération et du consentement |
| Packs hors ligne et protocole de synchronisation | Absent | Contrat dédié requis avant l’implémentation mobile |
| CI mobile, signature Android/iOS et distribution bêta | Absent | Pré requis de faisabilité, pas finition de fin de projet |

Tout changement majeur de cette base doit déclencher une révision de la roadmap.

## 5. Principes directeurs

1. Le backend reste la source de vérité des règles métier et des données partagées.
2. Le Mobile consomme des contrats HTTP ; il ne référence aucun assembly serveur.
3. Les entités de persistance et les modèles Web ne sont pas partagés avec l’application.
4. L’expérience essentielle fonctionne avec un réseau absent ou instable.
5. La localisation est déclenchée par une fonction visible, proportionnée et révocable.
6. Une recommandation indique les facteurs qui l’expliquent et laisse toujours le choix.
7. Une donnée live affiche sa source, son âge et son niveau de confiance.
8. Une idée n’entre dans une phase que si ses données, droits, risques et critères de succès sont connus.
9. Le monolithe modulaire est privilégié ; les microservices ne sont pas un objectif.
10. La performance se mesure sur des appareils et un réseau représentatifs, pas seulement sur simulateur.
11. Les contraintes des stores, de confidentialité et de CI sont traitées dès le cadrage.
12. Chaque phase peut être arrêtée si sa gate n’est pas franchie.

## 6. Décision technologique mobile

### 6.1 Position proposée

.NET MAUI est le candidat privilégié pour une application native Android et iOS, compte tenu du socle .NET existant et de la possibilité de mutualiser le langage, les pratiques de test et certains modèles contractuels.

Ce choix n’est définitif qu’après le spike ECO-A02. La formulation « version LTS de MAUI » est à éviter : le cycle de support de .NET MAUI ne suit pas toute la durée LTS de .NET. Le projet devra viser la dernière version stable encore supportée et prévoir une montée de version majeure annuelle.

MAUI Blazor Hybrid ou une WebView Angular ne sont pas retenus comme moyen de recycler le frontend. Une WebView ponctuelle peut rester justifiée pour un contenu externe isolé, après examen UX, sécurité et accessibilité.

### 6.2 Spike de confirmation ECO-A02

Le spike, limité à un ou deux sprints, doit produire un prototype jetable ou clairement isolé et tester sur au moins un appareil Android moyen de gamme et un iPhone réel :

- compilation Android et iOS, dont la chaîne Mac obligatoire pour iOS ;
- carte avec marqueurs, zones et superpositions ;
- téléchargement puis lecture d’un petit pack hors ligne SQLite ;
- position au premier plan, précision et changement de permission ;
- un geofence de démonstration, sans en faire une fonction MVP ;
- deep link vérifié depuis une URL réelle ;
- réception d’une notification de test ;
- stockage sécurisé et comportement après restauration/sauvegarde ;
- démarrage à froid, mémoire, fluidité, batterie et volume réseau ;
- navigation clavier/lecteur d’écran, taille de texte et contraste ;
- intégration d’un SDK natif minimal via une abstraction ;
- pipeline signé de distribution interne sur les deux plateformes.

La gate n’est franchie que si les limites observées, les dépendances et le coût annuel de maintenance sont acceptés. Sinon, un ADR compare MAUI avec les alternatives pertinentes avant tout socle durable.

## 7. Architecture cible

### 7.1 Vue d’ensemble

```text
Web Angular SSR ───────┐
                      ├── HTTP/OpenAPI ── WebAPI ── Application ── Core
Mobile Android/iOS ───┘                         └── Infrastructure

Administration Web ── services d'application existants
Mode Terrain mobile ─ contrats autorisés vers ces mêmes services
```

Le backend continue à appliquer validation, autorisation, règles métier, audit et persistance. Le Mobile peut posséder des règles locales d’expérience — par exemple l’état d’une session non synchronisée — mais ne réimplémente pas les invariants serveur.

### 7.2 Backend

Les responsabilités restent :

- `Core` : entités, valeurs et règles métier pures ;
- `Application` : cas d’usage, ports et orchestration ;
- `Infrastructure` : persistance et services externes ;
- `WebAPI` : transport HTTP, authentification, validation de requête et composition.

L’arrivée du Mobile ne justifie pas :

- une réorganisation générale de `Controllers` vers `Endpoints` ;
- une duplication `WebService`/`MobileService` pour le même cas d’usage ;
- un accès direct du contrôleur à l’infrastructure ;
- un second modèle de parc ou d’attraction ;
- un découpage immédiat en microservices.

Les modules fonctionnels pourront se structurer progressivement dans le monolithe — identité, visites, synchronisation, live, contribution — avec des événements internes après commit lorsque le couplage le justifie.

### 7.3 Mobile : frontières logiques avant nombre de projets

Le démarrage recommandé est volontairement simple :

```text
MOBILE/
  AmusementPark.Mobile.Domain/
  AmusementPark.Mobile.Application/
  AmusementPark.Mobile.Infrastructure/
  AmusementPark.Mobile.App/
```

Les frontières sont obligatoires ; leur séparation en davantage de projets ne l’est pas. `Platform` ou `Presentation` ne seront extraits que si la taille, les tests ou la réutilisation le justifient.

| Couche | Contenu admis | Contenu exclu |
|---|---|---|
| Domain | `VisitSession`, `RideLog`, valeurs et règles locales pures | SQLite, HTTP, GPS, état de viewport, DTO OpenAPI |
| Application | Cas d’usage, commandes, résultats et ports orientés capacité | API MAUI, SQL, contrôles UI |
| Infrastructure | Client HTTP généré, SQLite, cache, sérialisation, implémentations de stockage | Règles métier serveur dupliquées |
| App | UI, navigation, état de présentation, composition et adaptateurs de plateforme | Accès SQL direct et orchestration métier dans les pages |

Exemples de ports préférables : `IParkPackStore`, `IVisitOutbox`, `IGeolocationReader`, `IDeviceNotificationScheduler` et `IAuthenticationSessionStore`. Une interface générique comme `ILocalDatabase` ou `IBackgroundTaskService` expose trop de détails et affaiblit les cas d’usage.

`MapViewportState` appartient à la présentation. Un manifeste de pack, une mutation en attente et une migration SQLite appartiennent aux contrats applicatifs ou à l’infrastructure, pas au domaine.

### 7.4 Contrats API

OpenAPI devient la frontière officielle, sans rendre le backend dépendant du client généré.

Les règles sont :

- conserver les routes Web existantes et leurs contrats ;
- privilégier les ajouts rétrocompatibles ;
- introduire une nouvelle version uniquement pour une rupture justifiée ;
- générer séparément le client C# mobile ;
- verrouiller la génération et le formatage dans la CI ;
- comparer le contrat publié à la branche de base pour détecter les ruptures ;
- documenter durée de support, dépréciation et retrait ;
- inclure une corrélation de requête et une identification non sensible du type de client ;
- ne jamais utiliser un header client comme autorisation.

Un groupe OpenAPI mobile peut faciliter la génération sans déplacer les contrôleurs. Les endpoints de la section 15 sont des pistes et non des contrats validés.

### 7.5 Compatibilité et capacités

Avant la première bêta, le serveur doit pouvoir exposer de façon peu coûteuse :

- version minimale et version recommandée de l’application ;
- fonctions disponibles par plateforme et environnement ;
- versions de contrat et de schéma de pack acceptées ;
- état de maintenance et mécanisme de mise à jour obligatoire ;
- paramètres distants non sensibles avec valeurs par défaut embarquées.

Les feature flags ont un propriétaire, une date d’expiration et un comportement de repli. Ils ne remplacent pas le versionnement des contrats.

## 8. Identité et sessions mobiles

### 8.1 Décision préalable

Le backend actuel émet des JWT et gère la rotation de refresh tokens, mais il ne doit pas être considéré automatiquement comme un serveur d’autorisation OAuth/OIDC adapté à une application native.

ECO-A03 doit choisir et documenter :

- fournisseur d’identité géré ou serveur d’autorisation maîtrisé ;
- flux Authorization Code avec PKCE dans le navigateur système, conformément aux bonnes pratiques des applications natives ;
- enregistrements clients Android/iOS, redirect URIs et universal/app links ;
- liaison entre comptes existants et identités externes ;
- stratégie Google et exigence éventuelle d’une option équivalente Sign in with Apple ;
- durée des tokens, rotation, détection de réutilisation et révocation ;
- modèle de session par appareil, nom de l’appareil et déconnexion distante ;
- récupération de compte, suppression et export ;
- comportement hors ligne et après expiration.

### 8.2 Règles de stockage et de cycle de vie

- Le token d’accès reste en mémoire autant que possible.
- Le secret de renouvellement utilise le stockage sécurisé natif.
- Aucune donnée sensible n’est inscrite dans les logs, analytics, sauvegardes ou notifications.
- Une déconnexion efface les données privées, l’outbox et les clés de l’utilisateur sans supprimer un pack public choisi comme conservable.
- Un changement de compte isole ou purge toute donnée locale associée au compte précédent.
- Les migrations et restaurations sont testées : le Keychain iOS peut survivre à une désinstallation et les sauvegardes Android peuvent restaurer des valeurs devenues illisibles.
- Le certificate pinning n’est pas imposé par défaut ; il ne sera retenu qu’avec un modèle de menace et un plan de rotation opérationnel.

L’authentification n’entre qu’en phase D, après validation d’un pilote anonyme. Cela évite de faire dépendre la valeur terrain initiale d’un chantier d’identité.

## 9. Offline-first et synchronisation

### 9.1 Principe

Le mode hors ligne n’est pas un cache opportuniste. Il repose sur un format versionné, installable atomiquement et testable indépendamment de l’interface.

Le Mobile lit d’abord l’état local. Le réseau rafraîchit cet état sans bloquer l’écran essentiel.

### 9.2 Pack de parc

Un manifeste de pack doit au minimum décrire :

- `contractVersion` et `schemaVersion` ;
- identifiant et révision du parc ;
- date de génération, fraîcheur, expiration et urgence d’invalidation ;
- langues incluses ;
- chunks, URLs, tailles et sommes de contrôle ;
- niveau de médias et ressources cartographiques ;
- droits, attribution et licences nécessaires ;
- versions minimale et maximale d’application compatibles.

Installation proposée :

1. récupérer le manifeste avec ETag ;
2. vérifier espace disque, compatibilité et réseau choisi par l’utilisateur ;
3. télécharger dans une zone temporaire avec reprise ;
4. vérifier tailles et sommes de contrôle ;
5. migrer ou importer dans une transaction ;
6. basculer atomiquement vers la nouvelle révision ;
7. conserver une révision de repli pendant une durée bornée ;
8. nettoyer sans supprimer une version encore utilisée.

Les packs sont idéalement immuables et préconstruits, puis servis depuis du stockage objet ou un cache. Ils ne doivent pas être recomposés à chaque téléchargement sur le VPS de production.

Avec huit langues, l’utilisateur choisit une langue principale et éventuellement une langue secondaire. Les médias ont plusieurs niveaux — essentiel, standard, complet — avec taille affichée. Les tuiles de carte ne sont intégrées que si leur licence autorise explicitement l’usage hors ligne.

### 9.3 Fraîcheur et urgence

Chaque type de donnée définit sa durée de fraîcheur. Une donnée durable peut rester consultable en indiquant sa date ; une fermeture de sécurité ou une information live périmée doit être invalidée ou masquée.

Le client conserve une configuration sûre embarquée si le serveur de capacités est indisponible.

### 9.4 Outbox de mutations

À partir de la phase D, une mutation hors ligne contient au minimum :

- identifiant local stable ;
- `clientMutationId` idempotent ;
- type d’opération et payload versionné ;
- utilisateur et appareil propriétaires ;
- date locale informative et date serveur à réception ;
- nombre d’essais, prochaine tentative et dernière erreur non sensible ;
- version ou ETag de l’entité connue au moment de la modification.

Le protocole précise :

- ordre des opérations dépendantes ;
- lots bornés et curseurs opaques ;
- tombstones pour les suppressions ;
- règles de conflit par type d’entité ;
- retries exponentiels avec jitter ;
- erreurs définitives et file de rejet visible ;
- récupération après timeout ambigu sans double écriture ;
- reprise après migration, fermeture forcée ou changement de compte ;
- limites de volume et durée de rétention.

Les favoris peuvent tolérer une fusion ensembliste. Un journal édité sur deux appareils demande une stratégie explicite. « Last write wins » n’est pas une règle universelle.

## 10. Carte, localisation et geofencing

### 10.1 Données cartographiques

Avant une carte phare, ECO-A01 et ECO-A04 auditent :

- précision des coordonnées des parcs, zones, attractions et services ;
- entrées réellement utilisables et accessibilité ;
- géométries de zones, obstacles, chemins, escaliers et surfaces non traversables ;
- source du fond de carte, attribution, coûts et droit au téléchargement ;
- droit d’utiliser les plans et données fournis par les parcs ;
- procédure éditoriale de correction et date de dernière vérification.

Les données existantes permettent des marqueurs et des zones. Elles ne suffisent pas à promettre un itinéraire piéton fiable.

### 10.2 Progression cartographique

- V1 : marqueurs, zones, filtres, orientation et distance approximative.
- V2 : chemins connus et graphe piéton validé pour le parc pilote.
- V3 : coûts d’accessibilité, fermetures temporaires et itinéraires alternatifs.

Une ligne droite ne doit jamais être nommée « navigation » : elle peut traverser un lac, un bâtiment ou une zone inaccessible. Tant qu’aucun graphe piéton n’est validé, l’interface parle de direction et de distance approximatives.

### 10.3 Localisation

Le pilote utilise la localisation au premier plan uniquement. Il doit fonctionner si l’utilisateur refuse la permission, sélectionne manuellement le parc ou désactive le GPS.

La fréquence dépend de l’écran, du mouvement et de la précision nécessaire. La carte cesse les mises à jour actives lorsqu’elle n’est plus visible. La position exacte n’est pas envoyée au serveur sans finalité déclarée.

### 10.4 Définition du geofencing

Le geofencing est une clôture géographique virtuelle. L’application demande au système d’exploitation de surveiller une zone — par exemple un cercle autour d’un parc — afin de recevoir un événement lors de l’entrée ou de la sortie, sans maintenir le GPS actif en permanence.

Usages possibles : proposer de démarrer une visite à l’arrivée, adapter l’accueil ou rappeler une action explicitement choisie. Ce n’est ni une localisation continue ni une preuve certaine de présence.

Contraintes à intégrer :

- iOS limite une application à 20 régions surveillées simultanément ;
- Android limite généralement une application à 100 geofences par utilisateur et appareil ;
- les événements peuvent être retardés, agrégés ou absents selon batterie, réseau et réglages ;
- la localisation en arrière-plan exige une justification forte et des permissions/store declarations spécifiques ;
- un geofence doit être choisi dynamiquement — visite planifiée, favori ou parc proche — et non créé pour tous les parcs ;
- rayon, durée de vie et précision doivent être testés sur appareils réels ;
- l’utilisateur doit pouvoir refuser ou désactiver la fonction sans perdre le cœur de l’application.

Le geofencing reste hors MVP. Il n’est introduit qu’après preuve que son bénéfice justifie la permission d’arrière-plan et le risque de rejet en store.

## 11. Données live et recommandations

### 11.1 Provenance obligatoire

Avant d’afficher un temps d’attente, un statut ou une alerte, le modèle doit conserver :

- source officielle, partenaire, communautaire ou estimation ;
- instant de collecte et dernière confirmation ;
- niveau de confiance et méthode de calcul ;
- durée de validité ;
- parc et fuseau horaire ;
- conditions d’usage et droit de redistribution.

Une valeur périmée est masquée ou présentée comme historique, jamais comme live.

### 11.2 Ordre d’introduction

1. données officielles ou partenaires autorisées ;
2. statut live avec âge et provenance ;
3. administration et mécanisme de correction ;
4. modération et signalement ;
5. contribution communautaire bornée ;
6. estimation, seulement avec mesure de qualité et indication claire.

### 11.3 « Que faire maintenant ? »

Une recommandation éventuelle peut combiner proximité, ouverture, contraintes explicites, fraîcheur des données, préférences et éléments déjà réalisés.

Elle doit afficher une explication courte — par exemple « proche, ouvert et adapté à tes préférences » — et permettre de modifier les critères. Aucun profilage opaque ou sensible n’est requis pour la première version.

## 12. Vie privée, sécurité et conformité stores

Ces sujets sont des prérequis transverses, pas une phase de finition.

### 12.1 Livrables de phase A

- inventaire des données collectées, finalités, bases légales et destinataires ;
- durées de rétention côté serveur, appareil, logs et sauvegardes ;
- parcours consentement, refus, retrait et permissions progressives ;
- export et suppression du compte et des données associées ;
- analyse spécifique aux mineurs, familles, âges approximatifs et restrictions de mobilité ;
- analyse d’impact si la localisation, les groupes ou le profilage le justifient ;
- privacy labels Apple et Data safety Google préparés à partir des faits ;
- privacy manifest Apple et inventaire des SDK tiers ;
- modèle de menace mobile, API et synchronisation ;
- procédure de signalement, blocage, modération et appel avant tout UGC ;
- politique photos : EXIF, visages, localisation, droit de publication et suppression.

### 12.2 Règles minimales

- permissions au moment du besoin, avec explication préalable ;
- service essentiel disponible sans analytics marketing ;
- pas de position exacte dans les analytics ;
- TLS obligatoire et secrets absents du binaire ;
- autorisation serveur sur chaque mutation ;
- idempotence et limites de débit adaptées aux synchronisations ;
- base locale privée, données sensibles réduites et chiffrement évalué selon le risque ;
- logs structurés sans tokens, coordonnées exactes, photos ou données personnelles inutiles ;
- procédure de révocation rapide d’une version ou d’un SDK compromis ;
- dépendances natives limitées et suivies.

## 13. Performance, autonomie et observabilité

### 13.1 Budgets à fixer pendant le spike

Pour chaque appareil de référence et scénario, mesurer puis approuver :

- démarrage à froid et reprise ;
- mémoire au repos, sur carte et pendant une synchronisation ;
- images et taille installée ;
- taille d’un pack essentiel et d’une mise à jour différentielle ;
- volume réseau d’une visite type ;
- batterie en premier plan et en arrière-plan ;
- fréquence et durée des tâches de fond ;
- latence p50/p95 des endpoints mobiles ;
- CPU, mémoire et bande passante induits sur le VPS.

Une régression au-delà du budget bloque la gate ou exige une dérogation documentée.

### 13.2 Observabilité

Le socle minimal précède le pilote :

- crashs, ANR et erreurs non gérées ;
- corrélation client/API sans identifiant publicitaire ;
- versions application, OS, contrat, schéma et pack ;
- succès/échec/durée de téléchargement et de synchronisation ;
- saturation et coûts backend ;
- tableau de santé et procédure d’incident ;
- kill switch distant avec valeur de repli locale ;
- analytics produit minimaux, consentis et documentés.

Les logs ne remplacent pas les métriques produit, et les analytics ne remplacent pas les journaux techniques.

## 14. CI/CD, repository et versions

Le dépôt utilise `master` et dispose actuellement d’un workflow de production Ubuntu. La chaîne mobile doit être conçue explicitement :

- jobs séparés et filtrés par chemins pour Web, backend, Android et iOS ;
- runner macOS pour compiler, signer et tester iOS ;
- comptes développeur Apple/Google, certificats, profils et clés avec rotation ;
- dépendances restaurées de façon reproductible et outils épinglés ;
- tests unitaires, architecture, contrat, migration et packaging ;
- distribution interne puis bêta fermée avant soumission publique ;
- environnements distincts et APIs clairement identifiées ;
- scans de dépendances et génération de SBOM si retenue par la politique projet ;
- procédure de rollback serveur et de désactivation d’une fonction mobile incompatible.

La version Web dans `FRONT/AmusementPark/release-version.json` reste celle du site. Android et iOS auront leurs propres version marketing et numéros de build, pilotés par tags ou releases mobiles. Une release documentaire du dépôt ne doit pas être confondue avec une version d’application publiée en store.

## 15. Contrats mobiles envisagés

Ces routes illustrent des capacités. Leurs noms, payloads et versions seront arrêtés par ADR et revus contre les routes actuelles avant implémentation.

| Capacité | Contrat possible | Phase |
|---|---|---|
| Capacités/compatibilité | `GET /api/mobile/capabilities` | B |
| Bootstrap borné | `GET /api/mobile/bootstrap` | B |
| Manifeste hors ligne | `GET /api/mobile/parks/{parkId}/pack-manifest` | B |
| Chunks immuables | URLs signées ou publiques issues du manifeste | B |
| Appareil/session | `POST/DELETE /api/mobile/device-sessions` | D |
| Synchronisation | `POST /api/mobile/sync` avec lots et curseurs bornés | D |
| Visites et journal | endpoints métier idempotents | D |
| Live | lecture avec provenance, âge et ETag | E |
| Recommandations | résultat expliqué, facteurs et fraîcheur | E |

Le bootstrap ne doit pas devenir un payload géant. Il agrège uniquement ce qui évite des allers-retours coûteux au premier écran, avec cache, compression, ETag et limites mesurées.

En premier plan, un polling adaptatif suffit tant que la fréquence et le volume restent raisonnables. SignalR n’est introduit que si un besoin mesuré le justifie. En arrière-plan, les notifications push transportent un signal minimal ; l’application récupère ensuite la donnée autorisée.

## 16. Roadmap par phases et gates

Les phases expriment un ordre de dépendance, pas des dates. Aucune phase suivante n’est lancée automatiquement.

### Phase A — Décisions, faisabilité et risques

| ID | Livrable | Critère essentiel |
|---|---|---|
| ECO-A00 | Charte produit, publics, non-objectifs, métriques et responsables | Proposition de valeur et seuils d’arrêt validés |
| ECO-A01 | Audit des données, précision, provenance, licences et parc pilote | Un parc de référence est juridiquement et éditorialement exploitable |
| ECO-A02 | Spike MAUI Android/iOS | Gate technique de la section 6 franchie sur appareils réels |
| ECO-A03 | ADR identité native | Fournisseur, PKCE, sessions, compte et Apple/Google décidés |
| ECO-A04 | ADR cartes et protocole offline | Fournisseur, licence, pack, invalidation et stockage décidés |
| ECO-A05 | Dossier vie privée, sécurité, stores et UGC | Risques, consentements, rétention et déclarations connus |
| ECO-A06 | Stratégie CI, signature, distribution et versionnement | Une build interne signée des deux plateformes est démontrée |
| ECO-A07 | Stratégie tests, feature flags, observabilité et budgets | Appareils, métriques, kill switches et gates définis |

**Gate A :** la technologie, l’identité, les cartes, les droits, le parc pilote, les coûts récurrents et la chaîne iOS sont acceptés. Toute inconnue bloquante maintient la roadmap au stade d’étude.

### Phase B — Fondations contractuelles et offline

| ID | Livrable | Critère essentiel |
|---|---|---|
| ECO-B01 | Gouvernance OpenAPI et test de compatibilité | Ruptures détectées sans modifier les routes Web existantes |
| ECO-B02 | Capacités, compatibilité et bootstrap minimal | Ancienne app, serveur indisponible et kill switch testés |
| ECO-B03 | Format de pack, générateur, stockage et installateur atomique | Pack installé, mis à jour, repris et restauré hors ligne |
| ECO-B04 | Données du parc pilote | Couverture, coordonnées, langues, médias et licences audités |
| ECO-B05 | Skeleton mobile à frontières logiques | Composition, navigation et dépendances respectent la section 7 |
| ECO-B06 | Observabilité et mesures de référence | Crashs, latences, batterie, réseau et VPS sont visibles |

**Gate B :** un appareil neuf peut installer une build, télécharger un pack borné, passer hors ligne, le lire et revenir à la révision précédente après un échec simulé.

### Phase C — Pilote terrain anonyme

| ID | Livrable | Critère essentiel |
|---|---|---|
| ECO-C01 | Découverte et fiche natives du parc pilote | Écrans utiles, accessibles et fonctionnels sans compte |
| ECO-C02 | Gestion utilisateur du pack hors ligne | Taille, langue, Wi-Fi, suppression et fraîcheur compréhensibles |
| ECO-C03 | Carte, filtres et GPS au premier plan | Refus de permission, précision faible et batterie testés |
| ECO-C04 | Session de visite et journal local | Démarrer, marquer « fait », corriger et restaurer localement |
| ECO-C05 | Universal links et app links | Web vers app et repli store/Web validés sur les deux OS |
| ECO-C06 | Bêta terrain instrumentée | Tests réels dans le parc pilote et retours consolidés |

**Gate C :** le pilote démontre une utilité terrain répétée, un hors-ligne fiable et des budgets acceptables. Si l’usage ne dépasse pas la simple consultation Web, le périmètre est réévalué avant d’ajouter un compte.

### Phase D — Compte, préparation et synchronisation

| ID | Livrable | Critère essentiel |
|---|---|---|
| ECO-D01 | Authentification native | Code + PKCE, navigateur système et récupération testés |
| ECO-D02 | Sessions par appareil | Rotation, réutilisation, révocation et déconnexion distante testées |
| ECO-D03 | Favoris et préparation de visite | Web et Mobile convergent sans modifier silencieusement les contrats |
| ECO-D04 | Outbox et protocole de synchronisation | Conflits, tombstones, timeout ambigu et changement de compte testés |
| ECO-D05 | Carnet de parcs et visites synchronisées | Historique cohérent, exportable et corrigeable |
| ECO-D06 | Cycle de vie des données personnelles | Export, suppression, rétention et purge locale vérifiés |

**Gate D :** aucune perte ou fuite entre comptes lors des scénarios offline, multi-appareils, migration, expiration et suppression.

### Phase E — Contexte, live et exploitation

| ID | Livrable | Critère essentiel |
|---|---|---|
| ECO-E01 | Modèle de provenance et fraîcheur | Toute donnée live expose source, âge et confiance |
| ECO-E02 | Statuts et temps d’attente autorisés | Source durable, TTL, repli et correction opérationnels |
| ECO-E03 | « Que faire maintenant ? » explicable | Facteurs visibles, préférences modifiables, qualité mesurée |
| ECO-E04 | Administration, modération et extension du Mode Terrain | Services existants réutilisés, autorisation et audit préservés |
| ECO-E05 | Push opt-in | Consentement par catégorie, fréquence et désabonnement testés |
| ECO-E06 | Détection du parc et geofencing borné | Sélection dynamique, limites OS et justification store validées |
| ECO-E07 | Météo contextuelle | Source, cache, fraîcheur et valeur produit démontrés |
| ECO-E08 | Graphe piéton du parc pilote | Aucun itinéraire sans chemins et obstacles validés |

**Gate E :** les données live sont légales, explicites et exploitables ; les notifications et la localisation de fond apportent plus de valeur que de friction mesurée.

### Phase F — Communauté et personnalisation

| ID | Capacité candidate | Prérequis |
|---|---|---|
| ECO-F01 | Contributions terrain grand public et réputation | Modération, signalement, anti-abus et provenance |
| ECO-F02 | Groupes de visite et localisation partagée | Consentement de chaque membre, expiration et risque mineurs |
| ECO-F03 | Profils famille, contraintes et accessibilité avancée | Données fiables, minimisation et langage non discriminant |
| ECO-F04 | Gamification saine | Pas de pression dangereuse, addictive ou contraire à l’accessibilité |
| ECO-F05 | Photos et souvenirs | EXIF, droits, visages, stockage et suppression maîtrisés |
| ECO-F06 | Accueil contextuel, personnalisation et recherche géographique | Contrôles explicites et explications |
| ECO-F07 | Actualités et contenus contextuels | Ligne éditoriale, fréquence et cache |
| ECO-F08 | Affiliations ou premium éventuel | Transparence, indépendance des recommandations et consentement |

**Gate F :** la communauté peut être opérée humainement et financièrement sans détériorer la fiabilité du produit.

### Phase G — Laboratoire

Ces idées ne sont pas planifiées. Chacune nécessite un mini business case, un prototype, une analyse de permissions et une gate indépendante.

| ID | Exploration | Condition d’entrée minimale |
|---|---|---|
| ECO-G01 | Widgets Android/iOS | Donnée réellement utile, fraîche et peu coûteuse |
| ECO-G02 | Live Activities, Dynamic Island et notification persistante | Session courte, consentie et arrêt fiable |
| ECO-G03 | Wear OS/watchOS | Usage mobile validé et budget de maintenance disponible |
| ECO-G04 | Réalité augmentée | Valeur supérieure à la carte et sécurité sur le terrain |
| ECO-G05 | BLE, beacons et positionnement indoor | Partenaire, matériel, installation et maintenance financés |
| ECO-G06 | QR et NFC | Contenu signé, anti-abus et déploiement physique maîtrisé |
| ECO-G07 | Portail et API partenaires | Contrats, quotas, support, audit et modèle économique |
| ECO-G08 | Vidéo et contenu propriétaire | Droits, hébergement, accessibilité et coût éditorial |

## 17. Parc pilote et stratégie de déploiement

Le pilote cible un seul parc de référence. Son choix ne dépend pas uniquement de sa popularité :

- données complètes et coordonnées fiables ;
- autorisation des médias et cartes ;
- diversité suffisante d’attractions et services ;
- équipe capable de tester réellement sur place ;
- réseau volontairement imparfait pour valider le hors-ligne ;
- mécanisme rapide de correction éditoriale.

Déploiement proposé :

1. distribution interne à l’équipe ;
2. bêta fermée avec scénarios guidés ;
3. bêta terrain limitée au parc pilote ;
4. élargissement progressif avec feature flags ;
5. publication store seulement après conformité et support opérationnel ;
6. ajout d’un parc uniquement après contrôle automatique et éditorial de sa readiness.

La couverture multi-parcs et multi-langues augmente par lots. Elle ne doit pas être promise tant que le coût de génération, téléchargement, vérification et maintenance d’un parc n’est pas connu.

## 18. Fonctions signatures, dans le bon ordre

### 18.1 Park Companion

Socle prioritaire : fiche native, carte hors ligne, GPS en premier plan et session locale. Les couches live, recommandation, push et geofencing se greffent ensuite séparément.

### 18.2 Mon carnet de parcs

Le carnet naît du journal local du pilote, puis devient synchronisé avec le compte. Il peut afficher visites, attractions réalisées, souvenirs choisis et statistiques compréhensibles. L’utilisateur peut corriger, exporter et supprimer son historique.

### 18.3 « Que faire maintenant ? »

Cette fonction arrive après la provenance live et les préférences. Elle reste une aide explicable, pas un planificateur opaque ni une promesse de journée optimale.

### 18.4 Radar parcs

La recherche géographique et l’accueil contextuel réutilisent les capacités Web existantes. Le radar n’exige pas une surveillance permanente de la position et propose une sélection manuelle.

## 19. Ce qu’il ne faut pas faire

- recycler l’application Angular dans une WebView ou choisir MAUI Blazor Hybrid par défaut ;
- copier le backend, ses entités de persistance ou ses règles métier dans le Mobile ;
- partager des références d’assemblies entre serveur et application ;
- réorganiser tous les contrôleurs ou générer tous les clients Web pour préparer le Mobile ;
- multiplier les projets, abstractions ou microservices sans problème mesuré ;
- appeler une longue série d’endpoints pour composer le premier écran ;
- construire un pack lourd à la demande sur le VPS ;
- télécharger les huit langues et tous les médias sans choix utilisateur ;
- présenter une ligne droite comme un itinéraire piéton ;
- activer le GPS permanent ou demander la localisation de fond au démarrage ;
- afficher une donnée live sans source, âge ou droit de redistribution ;
- ouvrir les contributions sans administration, modération et procédure d’abus ;
- stocker des secrets ou positions exactes dans les logs et analytics ;
- promettre une fonction parce qu’un SDK existe ;
- introduire de l’IA sans jeu d’évaluation, explicabilité et valeur supérieure à une règle simple ;
- traiter la CI, les stores, la confidentialité, les tests réels ou l’accessibilité à la fin.

## 20. Definition of Ready d’une capability

Une capability ne peut entrer en développement que si :

- son utilisateur et son problème sont identifiés ;
- son résultat attendu et ses métriques sont définis ;
- ses dépendances de données et leurs droits sont connus ;
- les contrats et règles de compatibilité sont esquissés ;
- les permissions, la rétention et la suppression sont établies ;
- les comportements offline, erreur, refus et repli sont décrits ;
- les impacts batterie, réseau, stockage, VPS et bundle sont évalués ;
- les besoins Android/iOS et d’appareils réels sont listés ;
- le support, la modération ou l’exploitation nécessaires sont financés ;
- le rollout, le kill switch et le rollback sont prévus ;
- le périmètre tient dans une suite de petites PR focalisées.

## 21. Definition of Done d’une capability

Une capability est terminée lorsque :

- les règles d’architecture sont respectées et testées ;
- les contrats sont compatibles ou versionnés ;
- les tests unitaires, d’intégration, de migration et de device pertinents passent ;
- Android et iOS sont vérifiés selon la matrice définie ;
- refus de permission, offline, réseau lent, reprise et changement de compte sont couverts ;
- l’accessibilité et les huit langues concernées sont vérifiées ;
- les budgets performance, batterie, réseau, stockage et VPS sont tenus ;
- logs, métriques, alertes et tableaux de santé sont disponibles ;
- sécurité, confidentialité et déclarations stores correspondent au comportement réel ;
- la documentation utilisateur et opérationnelle est à jour ;
- le déploiement progressif, le support et le rollback sont testés ;
- aucune limitation connue n’est cachée.

## 22. Correspondance avec la roadmap initiale

Cette table garantit que les idées initiales ne disparaissent pas lorsqu’elles sont déplacées derrière leurs prérequis.

| Ancien item | Nouvelle destination |
|---|---|
| ECO-00 Charte | ECO-A00 |
| ECO-01 Contrats API | ECO-B01 et ECO-B02 |
| ECO-02 Skeleton MAUI | ECO-A02 puis ECO-B05 |
| ECO-03 Authentification | ECO-A03 puis ECO-D01/D02 |
| ECO-04 Découverte | ECO-C01 |
| ECO-05 Deep links | ECO-C05 |
| ECO-06 Préparation | ECO-D03 ; son volet offline devient ECO-B03/C02 |
| ECO-07 Visit Session | ECO-C04 puis ECO-D05 |
| ECO-08 Carte GPS | ECO-C03 |
| ECO-09 Détection du parc | Premier plan en ECO-C03, geofencing en ECO-E06 |
| ECO-10 Navigation interne | ECO-E08 après validation du graphe |
| ECO-11 Que faire maintenant | ECO-E03 |
| ECO-12 Journal et collection | ECO-D05 |
| ECO-13 Gamification | ECO-F04 |
| ECO-14 Temps d’attente | ECO-E01/E02 |
| ECO-15 Statut live | ECO-E01/E02 |
| ECO-16 Push | ECO-E05 |
| ECO-17 Background et geofencing | ECO-E06 |
| ECO-18 Contribution publique | ECO-F01 |
| ECO-19 Mode Terrain | ECO-E04, par extension de l’existant |
| ECO-20 Photos et souvenirs | ECO-F05 |
| ECO-21 Groupes | ECO-F02 |
| ECO-22 Profils famille | ECO-F03 |
| ECO-23 Accessibilité avancée | ECO-F03 ; accessibilité de base transverse |
| ECO-24 Météo | ECO-E07 |
| ECO-25 Home contextuelle | ECO-F06 |
| ECO-26 Personnalisation | ECO-E03/F06 |
| ECO-27 Recherche géographique | ECO-F06 |
| ECO-28 Widgets | ECO-G01 |
| ECO-29 Live Activities | ECO-G02 |
| ECO-30 Wearables | ECO-G03 |
| ECO-31 AR | ECO-G04 |
| ECO-32 BLE/beacons | ECO-G05 |
| ECO-33 NFC/QR | ECO-G06 |
| ECO-34 Écosystème partenaire | ECO-G07 |
| ECO-35 Provenance | ECO-A01 puis ECO-E01 |
| ECO-36 Actualités | ECO-F07 |
| ECO-37 Vidéo | ECO-G08 |
| ECO-38 Monétisation | ECO-F08 |
| ECO-39 Administration | ECO-E04 |
| ECO-40 Feature flags | ECO-A07/B02 |
| ECO-41 Observabilité | ECO-A07/B06 |
| ECO-42 Privacy by design | ECO-A05, puis transverse |
| ECO-43 Sécurité | ECO-A05, puis transverse |
| ECO-44 Performance/autonomie | ECO-A02/A07, puis gates continues |
| ECO-45 CI/CD | ECO-A06, puis transverse |
| ECO-46 Tests | ECO-A07, puis Definition of Done de chaque phase |

## 23. Décisions à consigner par ADR

Avant la fin de la phase A :

1. choix du framework mobile après spike ;
2. fournisseur et flux d’identité native ;
3. fournisseur cartographique, licences et politique offline ;
4. format, stockage, versionnement et invalidation des packs ;
5. compatibilité API, génération client et politique de dépréciation ;
6. structure physique de la solution mobile ;
7. analytics, crash reporting et consentement ;
8. CI, signature, distribution et versionnement mobile ;
9. parc pilote et critères de readiness ;
10. stratégie de localisation, geofencing et tâches de fond.

Chaque ADR contient contexte, options, décision, conséquences, coût de sortie et date de réexamen.

## 24. Sources techniques de cadrage

Consultées le 9 août 2026 :

- [.NET MAUI support policy](https://dotnet.microsoft.com/platform/support/policy/maui)
- [What is .NET MAUI?](https://learn.microsoft.com/en-us/dotnet/maui/what-is-maui)
- [OAuth 2.0 for Native Apps — RFC 8252](https://datatracker.ietf.org/doc/html/rfc8252)
- [Apple App Review Guidelines](https://developer.apple.com/app-store/review/guidelines/)
- [Android geofencing documentation](https://developer.android.com/develop/sensors-and-location/location/geofencing)
- [Apple Core Location region monitoring](https://developer.apple.com/documentation/corelocation/monitoring-the-user-s-proximity-to-geographic-regions)
- [Google Play background location policy](https://support.google.com/googleplay/android-developer/answer/9799150)
- [Apple privacy manifests](https://developer.apple.com/documentation/bundleresources/adding-a-privacy-manifest-to-your-app-or-third-party-sdk)
- [.NET MAUI secure storage](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/secure-storage)

Les limites de plateforme, règles de store et versions supportées évoluent. Elles doivent être revérifiées au début de chaque phase concernée, jamais recopiées comme vérités permanentes.
