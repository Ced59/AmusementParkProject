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

La configuration et le contrôle source sont vérifiés avant de prendre un lease,
puis de nouveau dans la frontière partagée juste avant d’admettre l’appel au
fournisseur. Les contrôles parc et attraction s’appliquent une fois le mapping
interne courant connu, avant persistance : un ancien contrôle de parc ne peut
donc pas bloquer un parc remappé. Le rejeu de quarantaine applique
également ce contrôle et laisse l’incident en attente tant que la collecte est
arrêtée. La lecture publique applique la même hiérarchie et conserve la réponse
publique neutre existante lorsque le direct est masqué.

Le contrat distingue une désactivation permanente de configuration d’un arrêt
opérationnel temporaire. Dans les deux cas, le navigateur retire immédiatement
les données live. Pour le second seulement, un onglet déjà ouvert conserve des
tentatives espacées et bornées afin de restaurer le bloc après reprise, sans
rechargement manuel ni exposition de données pendant l’arrêt.

La mutation d’un contrôle, l’admission d’un appel fournisseur et la dernière
vérification précédant une écriture partagent une frontière sérialisée par
source et entité externe. Un appel ou une écriture déjà engagé se termine donc
avant que l’arrêt soit confirmé ; tout travail qui vient ensuite recharge les
contrôles dans cette frontière. Cette règle couvre la collecte normale et le
rejeu de quarantaine. En régime stable, la production n’exécute qu’une instance
canonique de l’API. Pendant la bascule transactionnelle, le candidat qui
coexiste avec l’ancienne instance démarre avec la collecte live désactivée et
refuse, en `503` avec `Retry-After`, les mutations de contrôle et les rejeux. La
lecture publique reste indépendante : le candidat continue à restituer le
dernier état stocké avec sa fraîcheur réelle pendant toute la bascule.
L’ancienne instance peut donc finir ses écritures sous sa frontière locale sans
qu’un arrêt soit confirmé par un autre processus ; seule la nouvelle instance
canonique réactive ensuite collecte et mutations. Un passage futur à plusieurs
instances actives simultanément devra remplacer cette clôture de déploiement et
la frontière en mémoire par une coordination distribuée avant la mise à
l’échelle.
L’annulation de la requête est respectée jusqu’à l’entrée dans cette frontière.
Une fois une mutation critique ou une écriture d’observation engagée, la lecture
du contrôle, l’écriture MongoDB et la reconstruction du résultat vont au bout
sans dépendre de la connexion cliente. La frontière ne se libère donc jamais
pendant qu’une écriture acceptée par MongoDB peut encore aboutir. Pour une
mutation de contrôle, la photographie nécessaire au résultat est chargée avant
l’ajout puis complétée en mémoire avec la révision durable : aucune lecture
MongoDB faillible ne subsiste après le point d’engagement. Les filtres HTTP
peuvent ainsi toujours invalider le cache et journaliser une décision déjà
durable. L’écriture d’audit qui suit une action réussie utilise elle aussi un
token indépendant de la connexion cliente ; une déconnexion après engagement
ne peut donc ni annuler la trace ni empêcher l’invalidation englobante.

Lorsqu’une réponse fournisseur contient des observations supprimées par un
contrôle granulaire, son `ETag` reste conservé : un arrêt long ne transforme pas
chaque cycle en téléchargement complet inutile. La révision de réouverture est
comparée à l’heure de la dernière collecte réussie. Le premier appel qui suit
une réouverture omet alors une seule fois l’`ETag`, puis la réponse complète
réarme le polling conditionnel. Un `304` ne peut donc pas laisser absentes les
données volontairement écartées pendant l’arrêt, sans accroître durablement la
charge fournisseur.

## Exploitation

Le tableau de bord présente la dernière collecte réussie, le prochain passage,
le circuit de protection, les échecs consécutifs, la couverture de mapping, la
quarantaine, les versions d’adaptateur/transformation et la politique de licence.
La couverture charge toutes les révisions courantes appartenant au parc pilote,
sans troncature par une page globale de la source. Un périmètre parc est dérivé
dès qu’au moins une attraction de ce parc possède un mapping éligible : l’arrêt
global reste donc disponible même si le fournisseur ne publie pas de ligne
distincte pour le parc lui-même. Chaque changement exige un
motif, utilise une révision attendue contre les
écrasements concurrents et reste présent dans l’historique. Le rejeu de la
quarantaine reste borné à 100 incidents par action.
Après une mutation réussie, le cache HTTP du direct est évincé par son tag dédié
sans refroidir les autres données publiques ni les pages SSR.
Une génération dédiée entre aussi dans la clé du cache du direct. Si une réponse
publique avait commencé avant l’arrêt, elle reste liée à l’ancienne génération
et la politique interdit son stockage lorsque la génération a changé pendant la
requête. Une réponse tardive ne peut donc pas repeupler le cache évincé.
Le nombre total d’incidents en attente reste visible, tandis qu’un compteur
distinct pilote le bouton de rejeu avec les seuls incidents encore rejouables
dont le mapping courant est éligible. Ce compteur et le lot relu sont limités
à la source et aux cibles éligibles du parc pilote actuellement configuré. Les
incidents d’une ancienne configuration ou d’un mapping encore candidat ne
peuvent donc ni activer le bouton ni retarder le lot courant. Les diagnostics
fournisseur et conflits de statut ne provoquent plus d’action à vide.
Le message de résultat distingue un lot entièrement rejoué, partiellement
rejoué, encore bloqué ou désormais vide : l’administration ne présente jamais
comme un succès une action qui n’a résolu aucun incident.

## Retour arrière

Réactiver le périmètre concerné depuis le même écran crée une nouvelle révision
sans effacer l’historique. En urgence supérieure, les options de configuration
`LiveDataPolling.Enabled` et `PublicReadEnabled` conservent la priorité absolue.
