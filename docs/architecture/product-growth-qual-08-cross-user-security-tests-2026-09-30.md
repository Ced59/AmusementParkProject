# QUAL-08 — Tests cross-user et sécurité

## Décision métier

Un identifiant connu, une requête rejouée ou un rendu serveur ne doivent jamais
permettre à un membre d'agir au nom d'un autre ni de retrouver une donnée redevenue
privée. Les dix scénarios critiques de la roadmap sont donc rattachés à des tests
automatiques précis, exécutés par les suites existantes à chaque pull request.

Ce jalon ne crée pas un second framework de sécurité. Il vérifie les contrôles déjà
placés dans le domaine, l'Application, la persistance, l'API et le routage SSR, puis
ferme les deux preuves qui manquaient sur les notes privées et les liens révoqués.

## Matrice des attaques et des preuves

| Attaque simulée | Résultat attendu | Preuves automatiques principales |
|---|---|---|
| Deviner l'identifiant d'une visite | La lecture est bornée au propriétaire connecté et renvoie le même `not-found` qu'une visite absente. | `PassportVisitHandlersTests.GetVisit_WhenOwnerCannotSeeIt_ShouldReturnTheSameNotFoundAsAnAbsentVisit`, `PassportVisitsControllerTests.GetByIdAsync_ShouldForwardOnlyTheAuthenticatedOwner` |
| Réutiliser un partage après révocation | La révocation retire le jeton ; la requête publique exige un état publié et une visibilité publique ou non listée, puis échoue sans lire le propriétaire. | `SharePublicationTests.Revoke_ShouldImmediatelyRemoveTheTokenAndBecomeTerminal`, `SharePublicationRepositoryTests.BuildResolvableTokenFilter_ShouldExcludeRevokedOrPrivatePublications`, `SharePublicationAccessResolverTests.ResolveAsync_WhenRevokedTokenIsNoLongerResolvable_ShouldReturnNotFoundWithoutReadingOwner` |
| Accepter deux fois une invitation | Le second appel rejoue l'admission déjà établie sans créer un second membre. | `TripAdmissionServiceTests.AcceptAsync_WhenTheSameAcceptedOperationIsRetried_ShouldReturnTheEstablishedTrip` |
| Modifier ou supprimer la note d'un autre membre | L'identité n'existe pas dans le contrat entrant : la commande reçoit exclusivement l'identité authentifiée et les réponses privées sont non stockables. | `RatingsControllerTests.UpsertAsync_ShouldUseOnlyTheAuthenticatedUserAsOwner`, `RatingsControllerTests.DeleteMyRatingForTargetAsync_ShouldUseOnlyTheAuthenticatedUserAsOwner`, `RatingsControllerTests.PrivateEndpoints_ShouldRemainAuthenticatedActivatedAndNonCacheable` |
| Multiplier des tours par retry | Une clé idempotente déjà complétée restitue les mêmes occurrences ; un lot partiel ou divergent est récupéré ou rejeté. | `UserRideOccurrenceRepositoryTests.ResolveIdempotentBatchCreation_WithACompleteMatchingBatch_ShouldReplaySnapshots`, `ResolveExistingBatchCreationAsync_WithAPartialBatch_ShouldRecoverFromReservedSnapshots`, `ResolveExistingBatchCreationAsync_WithChangedConfirmation_ShouldConflict` |
| Déclencher plusieurs fois le même e-mail | Le travail possède une identité stable, attend la fermeture du digest et n'est jamais renvoyé après un état terminal ou ambigu. | `NotificationEmailDeliverySchedulerTests.ScheduleAsync_ShouldUseStableIdentityAndWaitUntilTheDigestCloses`, `NotificationEmailDeliveryJobHandlerTests.HandleAsync_ShouldNotReplayATerminalDelivery`, `HandleAsync_ShouldCancelAnAmbiguousStartedAttemptWithoutReplayingTheEmail` |
| Injecter du contenu actif dans une légende ou une note publique | Le texte public refuse balises, schémas exécutables et liens ; la politique de partage n'autorise aucun champ privé. | `PublicShareTextSafetyPolicyTests.IsSafePlainText_WithMarkupOrLinks_ShouldReturnFalse`, `ShareContentPolicyTests.PublicPolicyContract_ShouldNotRepresentForbiddenPrivateCategories` |
| Exfiltrer un profil de groupe | Une mutation d'un autre propriétaire renvoie `not-found` sans écriture ; suppression et mise à jour combinent propriétaire et version. | `ParkFitGroupProfileLifecycleServiceTests.UpdateAsync_ForAnotherOwner_ShouldReturnNotFoundWithoutWriting`, `DeleteAsync_ShouldFenceOwnerAndVersion` |
| Forcer SSR à lire une ressource privée | Les routes de profil et les brouillons locaux restent rendus côté client avant le fallback SSR ; seules les publications explicitement partagées sont rendues serveur. | `app.routes.server.spec.ts` : `keeps every nested private profile route client-rendered before the fallback`, `keeps device-local passport routes client-rendered before the fallback` |
| Polluer le mapping live | Seul un administrateur activé peut décider, la mutation est limitée, auditée et attribuée à l'identité authentifiée. | `AdminLiveTargetMappingsControllerTests.Controller_ShouldBeAdminOnlyActivatedAndNonCacheable`, `ReviewAsync_ShouldUseAuthenticatedAdministratorAsReviewer` |

## Renforcement livré

### Notes personnelles

Les sept routes `ratings/.../me` déclarent désormais explicitement
`Cache-Control: no-store` et `Location=None`. Elles restent protégées par le rôle
membre/modérateur/administrateur et par le contrôle de compte activé et non bloqué.
Les DTO de mutation ne contiennent aucun `UserId` : le contrôleur dérive toujours
le propriétaire du claim authentifié.

### Partages révoqués

La preuve couvre les trois frontières : le domaine retire le jeton et rend la
publication terminale, MongoDB ne résout que les publications `Published` visibles
et non suspendues, puis l'Application répond comme pour un lien inconnu sans lire
le compte propriétaire. Une ancienne URL ne peut donc pas révéler si elle a existé.

## Architecture et coût

- aucun changement de schéma MongoDB ni migration de données ;
- aucune nouvelle dépendance et aucun traitement supplémentaire au runtime hors
  les en-têtes HTTP privés déjà calculés par ASP.NET Core ;
- règles métier conservées dans Core/Application, identité HTTP collectée dans le
  contrôleur et filtre de résolution conservé dans Infrastructure ;
- une classe par fichier respectée ;
- les tests existants restent la source exécutable, la matrice sert d'index stable
  pour les audits et les futures revues de menace.

## Vérification

Les preuves sont incluses dans les suites habituelles :

```bash
dotnet test API/AmusementPark.Core.Tests/AmusementPark.Core.Tests.csproj --configuration Release
dotnet test API/AmusementPark.Application.Tests/AmusementPark.Application.Tests.csproj --configuration Release
dotnet test API/AmusementPark.Infrastructure.Tests/AmusementPark.Infrastructure.Tests.csproj --configuration Release
dotnet test API/AmusementPark.WebAPI.Tests/AmusementPark.WebAPI.Tests.csproj --configuration Release
npm run test:ci
```

## Limites et suivi

Ces tests prouvent les invariants applicatifs et les frontières d'autorisation ;
ils ne remplacent pas un test d'intrusion externe, une analyse de dépendances ou
la surveillance d'incident. Les runbooks, alertes et exercices de réaction sont le
périmètre de `QUAL-09`.

## Rollback

Le rollback du changement HTTP consiste à retirer les attributs `no-store`. Les
tests et cette matrice peuvent rester sans effet sur les données. Aucune migration
MongoDB n'est à inverser.
