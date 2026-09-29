# LIVE-10 — Pilotage opérationnel et arrêts d’urgence

## Finalité métier

LIVE-10 permet à un administrateur d’arrêter immédiatement la collecte ou
l’affichage public des données en direct sans attendre un redéploiement. Le
périmètre peut être toute une source, un parc couvert ou une attraction/lieu
couvert. La configuration de production reste un plafond : une commande
persistée ne peut jamais réactiver une collecte ou une lecture désactivée côté
serveur.

## Architecture

- `Core` porte le périmètre, le contrôle versionné et la politique hiérarchique.
- `Application` charge une photographie cohérente des contrôles et l’applique à
  la collecte, à la normalisation et aux lectures publiques.
- `Infrastructure` conserve chaque révision dans `live-operational-controls`,
  avec unicité du périmètre et de la révision. La collection et ses index sont
  créés automatiquement au démarrage ; aucune intervention MongoDB manuelle
  n’est requise.
- `WebAPI` expose uniquement une lecture et une mutation réservées aux
  administrateurs activés, limitées en débit et auditées.
- le frontend utilise un port et une façade dédiés ; la page est chargée à la
  demande dans le bundle administrateur.

## Règles effectives

```text
configuration serveur
        │
        ▼
contrôle source ──► contrôle parc ──► contrôle attraction
        │                 │                    │
        └──────── un seul « non » arrête le périmètre ───────┘
```

La collecte est vérifiée avant de prendre un lease ou d’appeler le fournisseur,
puis une seconde fois par cible avant persistance. La lecture publique applique
la même hiérarchie et conserve la réponse publique neutre existante lorsque le
direct est masqué.

## Exploitation

Le tableau de bord présente la dernière collecte réussie, le prochain passage,
le circuit de protection, les échecs consécutifs, la couverture de mapping, la
quarantaine, les versions d’adaptateur/transformation et la politique de licence.
Chaque changement exige un motif, utilise une révision attendue contre les
écrasements concurrents et reste présent dans l’historique. Le rejeu de la
quarantaine reste borné à 100 incidents par action.

## Retour arrière

Réactiver le périmètre concerné depuis le même écran crée une nouvelle révision
sans effacer l’historique. En urgence supérieure, les options de configuration
`LiveDataPolling.Enabled` et `PublicReadEnabled` conservent la priorité absolue.
