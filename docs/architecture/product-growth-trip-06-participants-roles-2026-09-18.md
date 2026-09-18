# TRIP-06 — Participants, rôles et acceptation réparable

Date : 18 septembre 2026

Version : 5.3.66

Roadmap : [`06-collaborative-trip-planning-roadmap.md`](../roadmaps/product-growth/06-collaborative-trip-planning-roadmap.md)

## Résultat métier

Le lien créé par `TRIP-05` devient une vraie entrée dans un voyage privé. L'invité
peut accepter ou refuser après connexion. Une fois l'acceptation établie, il voit le
voyage selon son rôle. Le propriétaire dispose d'une liste lisible des personnes,
peut ajuster leurs rôles et transférer la propriété. Chaque membre non propriétaire
peut quitter le groupe.

Le rôle n'est pas un simple réglage d'interface. Il décide côté serveur qui peut
modifier les informations générales et le programme :

| Capacité | Owner | Editor | Participant | Viewer |
|---|---:|---:|---:|---:|
| Lire le voyage | oui | oui | oui | oui |
| Renommer et modifier les dates | oui | oui | non | non |
| Ajouter, classer et programmer les parcs | oui | oui | non par défaut | non |
| Inviter | oui | non par défaut | non | non |
| Changer les rôles / transférer | oui | non | non | non |
| Voter | oui | oui | oui | non |
| Quitter | après transfert | oui | oui | oui |

Les options futures « un éditeur peut inviter » et « un participant peut ajouter
un candidat » restent explicites dans la politique, mais sont désactivées par
défaut. Aucun écran ne peut les activer implicitement.

## Frontières d'architecture

- **Core** : états d'adhésion, rôle effectif, matrice de permissions, transfert et
  départ, fence d'admission et invariants de l'agrégat ;
- **Application** : validation de l'identité du destinataire, orchestration de la
  saga, contrôle des permissions, résolution en lot des alias et concurrence
  optimiste ;
- **Infrastructure** : écritures MongoDB conditionnelles, heure serveur `$$NOW`,
  index de reprise et réconciliateur borné ;
- **WebAPI** : identité issue du JWT, contrats bornés, jeton dans le corps, erreurs
  HTTP normalisées ;
- **Angular** : ports injectés, façades, capacités calculées par le serveur et rendu
  responsive en lecture ou édition.

Chaque classe, interface, record et enum possède son propre fichier. Le contrôle
d'architecture automatique ne relève aucune violation C# ou TypeScript.

## Modèle de classes

```mermaid
classDiagram
    class TripPlan {
      +TripPlanId Id
      +string OwnerUserId
      +TripMember[] Members
      +TripMemberAdmissionFence? MemberAdmissionFence
      +long Version
      +ResolveRole(userId)
      +PrepareAdmission()
      +EstablishAdmission()
      +ChangeMemberRole()
      +TransferOwnership()
      +BeginMemberDeparture()
    }
    class TripMember {
      +TripMemberId Id
      +string UserId
      +TripDelegatedRole? DelegatedRole
      +TripMembershipState State
      +long MemberDataEpoch
      +string? AdmissionOperationId
    }
    class TripMemberAdmissionFence {
      +TripInvitationId InvitationId
      +string OperationId
      +string CandidateUserId
      +long Generation
      +DateTime LeaseExpiresAtUtc
      +TripMemberAdmissionFenceState State
    }
    class TripInvitation {
      +TripPlanId TripPlanId
      +TripInvitationStatus Status
      +TripDelegatedRole ProposedRole
      +string? AcceptingUserId
      +string? AcceptanceOperationId
      +long? AcceptanceGeneration
      +int UseCount
    }
    class TripAuthorizationPolicy {
      +HasPermission(role, permission) bool
    }
    TripPlan "1" *-- "1..50" TripMember
    TripPlan "1" *-- "0..1" TripMemberAdmissionFence
    TripMemberAdmissionFence --> TripInvitation
    TripPlan ..> TripAuthorizationPolicy
```

`Owner` n'est jamais stocké comme un second rôle. Il est dérivé lorsque
`TripMember.UserId == TripPlan.OwnerUserId`; le membre propriétaire conserve donc
`DelegatedRole = null`. Le transfert modifie le propriétaire et les deux rôles dans
la même écriture versionnée du document racine.

## Schéma MongoDB utile à ce jalon

```text
trip-plans
├── _id: string opaque
├── ownerUserId: string opaque
├── ownerSlot: int
├── ownerScopeHash: HMAC du créateur initial, immuable après transfert
├── members[]
│   ├── memberId: string opaque
│   ├── userId: string opaque
│   ├── delegatedRole?: Editor | Participant | Viewer
│   ├── state: Provisional | Active | Leaving
│   ├── joinedAtUtc: date
│   ├── memberDataEpoch: long
│   └── admissionOperationId?: provenance opaque de l'admission
├── memberAdmissionGeneration: long
├── memberAdmissionFence?
│   ├── invitationId: string opaque
│   ├── operationId: hash opaque
│   ├── candidateUserId: string opaque
│   ├── generation: long
│   ├── leaseExpiresAtUtc: date
│   └── state: Prepared | Active | Applied | Cancelled
├── admissionClosureState
├── childMutationEpoch
├── version
└── updatedAt

trip-invitations
├── _id / tripPlanId / tokenHash
├── proposedRole
├── status: Active | Accepting | Accepted | Declined | Revoked | Expired
├── useCount: 0 | 1
├── acceptingUserId?
├── acceptanceOperationId?
├── acceptanceOperationKeyHash?
├── acceptanceGeneration?
├── acceptanceLeaseExpiresAtUtc?
├── admissionCompletedAtUtc? (sortie de la file de reprise)
├── acceptedAtUtc? / declinedAtUtc?
├── version
└── retentionExpiresAtUtc
```

Indexes concernés :

- `trip-plans`: `(members.userId, members.state, deletionState, updatedAt, _id)` ;
- `trip-plans`: index partiel du fence par échéance et état ;
- `trip-invitations`: `tokenHash` unique ;
- `trip-invitations`: index de reprise par statut, marqueur d'admission terminée,
  échéance de lease et date de mise à jour ;
- `trip-invitations`: TTL de rétention, sans dépendre d'un nettoyage applicatif.

La lecture privée utilise un `ElemMatch` exigeant simultanément le bon `userId` et
`state = Active`. Deux éléments différents du tableau ne peuvent donc pas satisfaire
chacun une moitié du filtre. Un membre `Provisional` présent physiquement dans le
document ne dispose d'aucun accès. La liste conserve son tri stable par dernière
modification sans réutiliser le quota de 50 créations du propriétaire. Une limite
de sécurité dédiée de 100 adhésions récentes borne cependant chaque réponse et la
mémoire consommée sur le VPS ; elle reste distincte du quota de possession.

## Séquence d'acceptation

```mermaid
sequenceDiagram
    actor U as Invité authentifié
    participant W as WebAPI
    participant A as TripAdmissionService
    participant I as trip-invitations
    participant P as trip-plans
    participant R as Réconciliateur

    U->>W: POST /me/trip-invitations/accept\nbody { token } + Idempotency-Key
    W->>A: userId du JWT, token, opération
    A->>I: résoudre le hash du token
    A->>A: compte actif + destinataire HMAC compatible
    A->>P: installer fence Prepared (génération + lease)
    P-->>A: identité exacte du fence
    A->>I: Active -> Accepting avec $$NOW et identité du fence
    A->>P: Prepared -> Active
    A->>P: ajouter Provisional + fence Applied atomiquement
    A->>I: Accepting -> Accepted, useCount = 1
    A->>P: Provisional -> Active + retrait du fence
    A->>I: marquer l'admission terminée
    P-->>A: adhésion établie
    A-->>U: voyage rejoint
    Note over U,P: un retry avec la même opération retrouve le membre Active
    R->>I: balayer Accepting/Accepted incomplets
    R->>P: reprendre les mêmes opération et génération
    R->>P: compenser le fence expiré si l'adhésion n'est pas établie
```

Le point de linéarisation métier est la dernière écriture sur `trip-plans`. Avant
elle, le membre reste `Provisional`; après elle, un retry constate le membre actif et
renvoie le résultat sans rejouer les étapes. Le marqueur `admissionCompletedAtUtc`
retire ensuite l'invitation terminale des lots de reprise sans participer à la
décision métier. Le réconciliateur ne révoque jamais une
invitation `Accepted` dont le membre est déjà actif grâce à cette même opération,
même après l'expiration de la lease technique. Le membre actif conserve uniquement
cette provenance opaque interne, jamais exposée par l'API : une adhésion ultérieure
obtenue avec un autre lien ne peut pas valider l'ancienne invitation. Le fallback
d'établissement exige lui aussi le même `AdmissionOperationId` ; partager seulement
le compte candidat et l'état `Active` ne suffit jamais à revendiquer le succès.

La compensation expirée revalide `$$NOW` dans MongoDB puis ne révoque l'invitation
que si elle a effectivement retiré le fence de même génération. Si un autre nœud a
établi le membre entre la lecture et la compensation, l'invitation reste acceptée.
Une réponse perdue après la préparation retrouve également le même fence au retry ;
un refus rejoué avec la même clé renvoie le résultat acquis. Si le processus tombe
après avoir retiré un fence expiré mais avant d'avoir révoqué l'invitation, le passage
suivant reconnaît l'état déjà compensé et termine la révocation. Toute mutation
ordinaire du voyage exige enfin l'absence de fence et ne réécrit jamais ce champ :
une copie chargée avant l'admission ne peut donc effacer ni le membre provisoire ni
le verrou de reprise.

Le navigateur conserve en `sessionStorage` l'identifiant d'opération associé à une
empreinte locale du token et à une empreinte du compte initiateur, jamais le token
ni l'identifiant de compte eux-mêmes. Un autre compte ouvert dans le même onglet ne
reprend donc pas cette décision. Si l'acceptation
est validée mais que sa réponse HTTP se perd, un rechargement rejoue directement la
même opération avant de demander l'aperçu désormais terminal. Le succès efface le
marqueur ; une erreur ambiguë le conserve pour un nouveau retry et recharge
explicitement l'aperçu afin de ne jamais laisser une page vide. Si la connexion se
termine dans la modale sans recharger la route, la première action relit ce marqueur
lié au compte et reprend la décision déjà engagée avant de générer une autre clé.
Après une réponse ambiguë, même un clic sur l'action opposée rejoue cette décision
initiale : son identifiant ne peut pas être remplacé avant un résultat autoritatif.
L'état conservé en mémoire porte la même frontière de compte que le stockage : une
déconnexion suivie d'une autre connexion dans la page l'invalide sans supprimer la
reprise réservée au premier compte. Une réponse réseau tardive porte aussi le compte
initiateur : elle est ignorée si la session a changé avant son retour, puis la page
quitte son attente afin que le compte courant puisse choisir sa propre action.
La clé physique de session combine les deux empreintes : plusieurs comptes peuvent
donc conserver chacun leur reprise pour le même lien, et le succès de l'un ne nettoie
jamais le marqueur de l'autre.

## Séquence de transfert de propriété

```mermaid
sequenceDiagram
    actor O as Propriétaire
    participant A as TripParticipantService
    participant D as Domaine TripPlan
    participant M as MongoDB

    O->>A: transférer à memberId + rôle futur + version
    A->>M: charger le voyage possédé
    A->>M: vérifier le compte cible activé et non bloqué
    A->>D: TransferOwnership(owner, memberId, rôle)
    D->>D: vérifier membre Active et distinct
    D->>D: rétrograder ancien owner + promouvoir nouveau
    D->>D: avancer l'epoch des écritures enfants
    A->>M: version + ownerSlot + aucune lease antérieure active
    M-->>A: document complet persisté ou conflit
    A-->>O: participants et capacités recalculés
```

Après succès, le panneau participants demande à la vue d'ensemble de recharger le
voyage. Les droits, la version et les actions visibles changent immédiatement, sans
laisser d'anciens contrôles de propriétaire à l'écran. Plusieurs mutations proches
coalescent leurs demandes de lecture et rejouent toujours la dernière : une réponse
plus lente ne peut pas figer une ancienne version ou d'anciens droits. Si la réponse
d'une mutation se perd, la relecture qui découvre une version plus récente déclenche
le même rafraîchissement de la vue d'ensemble. Un départ ambigu est, lui, vérifié par
une lecture d'accès ; le `404` prouve que l'ancien membre peut être redirigé sans
présenter l'échec comme un départ non enregistré.

`ownerScopeHash` reste celui du créateur initial lorsque `ownerUserId` et
`ownerSlot` changent. Ce champ n'autorise rien : il sert uniquement à retrouver la
création d'origine si le premier propriétaire rejoue sa clé après un transfert. Le
transfert ne peut donc ni créer un doublon au retry, ni déplacer la portée
d'idempotence vers le nouveau propriétaire.

L'initialisation MongoDB migre l'index historique fondé sur le propriétaire
courant vers un unique index partiel
`(ownerScopeHash, creationOperationKeyHash)`. Elle supprime l'ancien index de
lecture non unique avant de créer cette contrainte, puis retire l'ancienne
unicité `(ownerUserId, creationOperationKeyHash)` : les deux systèmes ne
coexistent pas après la migration et un transfert ne peut pas entrer en collision
avec une clé de création propre au destinataire.

Le reçu de création conserve également le propriétaire d'origine avec les membres
d'origine. Une migration complète les reçus historiques avant tout transfert. Le
rejeu du créateur reconstruit donc un agrégat cohérent et stable, sans mélanger le
nouveau propriétaire avec l'ancien état des participants.

Un changement de rôle, un transfert ou un départ avance le même epoch racine que
les écritures enfants. MongoDB n'accepte cette transition qu'après la fin des leases
déjà accordées. La réponse de changement de droits ne peut donc pas réussir puis
laisser une ancienne autorisation terminer une modification de candidat ou de jour.
L'exécuteur libère toujours sa lease en `finally`, y compris si l'écriture lève une
exception ou propage une annulation ; la libération utilise son propre token non
annulé et reste en meilleur effort, avec l'expiration comme dernier filet de sécurité.
Un compte supprimé, désactivé ou bloqué ne peut pas devenir propriétaire : cette
vérification précède toute mutation afin de ne jamais rendre le voyage ingérable.
L'annulation d'une admission provisoire avance elle aussi la version du voyage. Une
mutation chargée avant le nettoyage entre ainsi en conflit au lieu de réintroduire
un membre provisoire déjà retiré.

## API et confidentialité

Les routes d'acceptation et de refus sont authentifiées :

```text
POST   /api/me/trip-invitations/accept
POST   /api/me/trip-invitations/decline
GET    /api/me/trips/{tripPlanId}/participants
PATCH  /api/me/trips/{tripPlanId}/participants/{memberId}/role
POST   /api/me/trips/{tripPlanId}/participants/{memberId}/transfer-ownership
DELETE /api/me/trips/{tripPlanId}/participants/me?expectedVersion=...
```

Le token n'apparaît pas dans l'URL authentifiée. Les réponses de participants
contiennent `memberId`, alias public, rôle, indicateur « moi » et date d'arrivée ;
elles n'exposent ni adresse, ni identifiant de compte, ni empreinte technique. Les
utilisateurs sont résolus en une requête par lot afin d'éviter un N+1.

## Compatibilité et évolution MongoDB

Il ne faut ni migration destructive ni deuxième adaptateur. Les champs ajoutés sont
facultatifs ou possèdent une valeur par défaut lors du mapping :

- un ancien membre sans `memberDataEpoch` est relu avec l'epoch initial ;
- un plan sans fence reste un plan normal ;
- une invitation antérieure sans données d'acceptation reste active, expirée ou
  révoquée selon son statut existant ;
- les nouveaux indexes sont créés par l'initialisation MongoDB habituelle.

Ainsi, le déploiement met à niveau le schéma de façon additive et l'écriture suivante
persiste le format courant. Aucun ancien et nouveau système d'adhésion ne coexistent.

## UX responsive

La page affiche le nombre de membres et le rôle courant. Les éditeurs obtiennent les
outils de dates et de programme. Pour `Participant` et `Viewer`, les champs,
poignées de glisser-déposer et boutons de déplacement sont absents, pas simplement
désactivés. Les dates, parcs et journées restent lisibles.

L'aperçu public associe aussi chaque décision à la génération et au token affichés.
Une réponse tardive d'un ancien lien ne peut donc ni remplacer le nouveau voyage,
ni afficher une erreur qui ne concerne plus la page courante.

Le panneau des participants borne toutes ses colonnes avec `minmax(0, 1fr)`, coupe
les mots longs et empile identité, rôle et actions à 42 rem puis l'en-tête à 22,5
rem. Le contrôle navigateur couvre réellement 320, 360, 390, 768 et 1280 px.

## Preuves automatisées

- matrice des permissions et options explicites ;
- membre provisoire invisible avant établissement ;
- transfert de propriété et impossibilité pour l'owner de partir directement ;
- départ d'un membre ;
- saga d'acceptation et retry après résultat accepté ;
- réconciliation conservant une adhésion déjà établie ;
- résolution des alias en un seul batch ;
- refus serveur d'un changement de rôle par un non-propriétaire ;
- capacités Angular, opération d'acceptation stable, navigation concurrente et lecture seule ;
- reprise d'une mutation de rôle, d'un transfert ou d'un départ dont la réponse réseau est perdue ;
- portée de création immuable après transfert de propriété ;
- architecture façades/ports et une classe par fichier ;
- huit langues et viewports réels.

Les préférences individuelles, votes, synthèses de désaccords, audit détaillé et
export ne sont pas simulés ici : ils appartiennent respectivement à `TRIP-07` à
`TRIP-11`.
