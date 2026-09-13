# SHARE-14A — Export privé du cycle de vie des partages

## Résultat métier

L'export du passeport restitue désormais, en plus des visites et notes privées :

- chaque publication du membre, sa visibilité, son état et sa politique de contenu ;
- les versions de source, de publication et de persistance ;
- les dates de création, mise à jour, publication et révocation ;
- les invitations de comparaison créées ou acceptées par le membre ;
- les comparaisons matérialisées, actives ou révoquées, avec leurs résultats complets.

Le fichier reste privé, authentifié et temporaire. Cette évolution ne rend aucune
donnée publique et ne modifie pas le contenu d'un partage existant.

## Frontières d'architecture

```mermaid
classDiagram
    class PassportExportJobHandler {
      +HandleAsync(context, cancellationToken)
    }
    class IPassportShareLifecycleExportSource {
      <<port Application>>
      +LoadAsync(userId, budget, cancellationToken)
    }
    class MongoPassportShareLifecycleExportSource {
      <<Infrastructure>>
    }
    class PassportShareLifecycleExportData {
      +Publications
      +Invitations
      +Comparisons
    }
    class CanonicalVisitExportWriter {
      +Write(request)
    }
    class PassportShareLifecycleExportWriter {
      +WriteJson(writer, request, references)
      +WriteCsvEntries(archive, request, references)
    }
    class PassportShareLifecycleComparisonCsvWriter {
      +WriteCsvEntries(archive, request, references)
    }
    class PassportExportReferenceMap {
      +Publication(id)
      +Invitation(id)
      +Comparison(id)
    }

    PassportExportJobHandler --> IPassportShareLifecycleExportSource
    IPassportShareLifecycleExportSource <|.. MongoPassportShareLifecycleExportSource
    IPassportShareLifecycleExportSource --> PassportShareLifecycleExportData
    PassportExportJobHandler --> CanonicalVisitExportWriter
    CanonicalVisitExportWriter --> PassportShareLifecycleExportWriter
    PassportShareLifecycleExportWriter --> PassportShareLifecycleComparisonCsvWriter
    CanonicalVisitExportWriter --> PassportExportReferenceMap
```

- Core conserve les entités et règles de cycle de vie existantes.
- Application définit le port de lecture, orchestre le job et possède le format
  canonique de l'export.
- Infrastructure lit les documents MongoDB et les convertit par les mappers
  existants ; elle ne décide pas ce qui doit être exposé.
- WebAPI ne reçoit aucun nouveau raccourci vers MongoDB et le contrat HTTP de
  téléchargement ne change pas.

## Séquence de génération

```mermaid
sequenceDiagram
    participant Q as Worker durable
    participant H as PassportExportJobHandler
    participant V as Dépôts visites/passages
    participant S as IPassportShareLifecycleExportSource
    participant M as MongoDB
    participant W as CanonicalVisitExportWriter
    participant R as Dépôt d'exports

    Q->>H: exécuter le job authentifié
    H->>R: verrouiller l'export en traitement
    H->>V: charger visites et passages avec le budget commun
    H->>S: LoadAsync(userId, même budget)
    S->>M: publications du propriétaire
    S->>M: invitations créateur ou accepteur
    S->>M: comparaisons créateur ou accepteur
    S-->>H: entités de domaine restaurées
    H->>W: écrire le schéma v3 JSON ou CSV ZIP
    W->>W: remplacer les identifiants par des références d'export
    W-->>H: artefact borné + empreinte SHA-256
    H->>R: persister les fragments et l'expiration
```

Les trois lectures sont séquentielles. Chaque document BSON consomme le même
`PassportExportSourceBudget` que les visites et passages. Le worker conserve sa
concurrence maximale à un et l'artefact final reste borné à 64 Mio.

## Schéma MongoDB lu

```mermaid
erDiagram
    SHARE_PUBLICATIONS {
      string _id "jamais exporté"
      string ownerUserId "filtre seulement"
      string sourceScopeKey "interprété, jamais recopié"
      string shareToken "jamais exporté"
      string status
      string visibility
      object contentPolicy
      long sourceVersion
      long publicationVersion
      long version
      date publishedAtUtc
      date revokedAtUtc
    }
    PROFILE_COMPARISON_INVITATIONS {
      string _id "jamais exporté"
      string token "jamais exporté"
      string creatorUserId "rôle seulement"
      string acceptorUserId "rôle seulement"
      string comparisonId "référence d'export"
      string status
      array categories
      date expiresAtUtc
      date acceptedAtUtc
    }
    PROFILE_COMPARISONS {
      string _id "jamais exporté"
      string invitationId "référence d'export"
      string shareToken "jamais exporté"
      string creatorUserId "orientation seulement"
      string acceptorUserId "orientation seulement"
      object calculation "résultats publics consentis"
      string status
      date revokedAtUtc
    }

    PROFILE_COMPARISON_INVITATIONS ||--o| PROFILE_COMPARISONS : matérialise
```

MongoDB crée automatiquement l'index partiel
`idx_profile_comparison_invitation_acceptor_created` sur
`acceptorUserId + createdAt`. Les index propriétaire des publications et participants
des comparaisons existaient déjà. Aucune migration de document ni intervention
manuelle n'est requise.

## Contrat d'export v3

### JSON

Les nouvelles racines sont :

- `sharePublications` ;
- `comparisonInvitations` ;
- `comparisons`, avec `results.parks`, `results.ratings`, `results.years` et
  `results.missedItems`.

### CSV ZIP

Les six tables historiques sont conservées et sept tables sont ajoutées :

| Fichier | Contenu |
| --- | --- |
| `share-publications.csv` | politique, versions, état et dates |
| `comparison-invitations.csv` | rôle, catégories, consentement et expiration |
| `comparisons.csv` | état, versions consenties et synthèse du calcul |
| `comparison-parks.csv` | compteurs de visites comparés |
| `comparison-ratings.csv` | notes consenties, écart et affinité |
| `comparison-years.csv` | activité comparée par année |
| `comparison-missed-items.csv` | expériences manquées comparées |

Chaque table enfant porte uniquement une `comparisonReference` propre à l'archive.
La neutralisation des formules CSV reste appliquée aux cellules commençant par
`=`, `+`, `-` ou `@`.

## Matrice de confidentialité

| Donnée source | Exportée | Forme exportée |
| --- | --- | --- |
| identifiant MongoDB | non | référence séquentielle éphémère |
| identifiant de membre | non | rôle `Creator` ou `Acceptor` |
| jeton de publication/invitation/comparaison | non | aucune valeur de remplacement |
| clé de scope source | non | type lisible, année ou référence de visite |
| empreinte de contenu | non | jamais nécessaire à l'utilisateur |
| identifiants de signalements | non | booléen `isModerationSuspended` seulement |
| politique de partage | oui | précision de date et champs inclus |
| versions consenties | oui | nombres de versions métier |
| résultats de comparaison | oui | orientation `your...` / `otherMember...` |
| dates de cycle de vie | oui | horodatages UTC ISO 8601 |

Un identifiant de visite contenu dans un scope n'est converti que s'il appartient
au même propriétaire et correspond à une visite du fichier. Sinon, la référence
reste nulle : la clé technique n'est jamais utilisée comme repli.

## Preuves automatisées

- les sorties JSON et CSV contiennent les nouvelles sections et références ;
- quatre types de jetons et tous les identifiants techniques injectés dans les
  fixtures sont absents du contenu final ;
- l'orientation des résultats reste celle du membre exporteur ;
- le job partage une seule instance de budget entre visites, passages et partages ;
- les filtres MongoDB couvrent propriétaire, créateur et accepteur ;
- l'index accepteur est partiel et ne surcharge pas les invitations en attente ;
- l'enregistrement DI résout le port Application vers son implémentation Infrastructure.

## Suite du jalon SHARE-14

- `SHARE-14B` : révoquer tous les liens avant la purge d'un compte, expirer les
  invitations, rendre les comparaisons inaccessibles et invalider les caches ;
- `SHARE-14C` : mesurer uniquement les événements de cycle de vie autorisés, sans
  notes exactes, dates exactes, identités comparées ni contenu public complet.
