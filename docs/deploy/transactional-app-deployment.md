# Déploiement applicatif transactionnel

Le retrait forcé d’un candidat pouvait interrompre une réponse encore traitée par un ancien worker Nginx. Un healthcheck réussi du canonique ne prouvait ni la fin des requêtes de l’autre paire, ni l’absence de trafic vers ses alias partagés. Ce protocole traite cette interruption de livraison. Il ne modifie pas les sitemaps et ne constitue pas une explication de leur traitement par un moteur de recherche.

## Périmètre et prérequis

Le parcours met à jour **front et API**, derrière l’edge existant, avec au plus deux paires. Il conserve MongoDB, MinIO, l’edge, leurs volumes et leur réseau. Le hash de configuration Compose et l’image effective des services partagés doivent correspondre au bundle ; une modification de cette infrastructure bloque avant création d’un candidat. Une première installation, une pile déjà dégradée sans journal exploitable et une maintenance d’infrastructure nécessitent une opération distincte. `DEPLOY_ZERO_DOWNTIME_ENABLED=false` ne déclenche plus un remplacement global implicite.

L’edge doit avoir exactement `worker_processes 1`, vérifié sur la configuration complète par `nginx -T`. Le protocole refuse un autre nombre de workers. Les protections HTTP, les limites de corps, le routage statique XML/robots, les en-têtes et les routes publiques restent inchangés.

## Installation sous verrou

Le workflow conserve la vérification de fraîcheur du commit, utilise un seul groupe de déploiement de production et n’interrompt plus automatiquement un déploiement en cours. Chaque run/attempt possède son archive et son installateur, au lieu d’un chemin partagé entre runs.

L’installateur Python acquiert le verrou avant de lire la configuration active ou de remplacer un fichier. Son FD9 est hérité par `deploy.sh`, puis par le coordinateur ; il n’est pas rouvert. Le script direct acquiert lui aussi le verrou avant `.env` et ses helpers. Les commandes Docker lancées par Python ne conservent pas ce descripteur ; le warmup détaché ferme toujours FD9 explicitement.

L’archive est validée avant le premier remplacement : chemins connus seulement, aucun lien ni entrée vers le répertoire runtime. Un marqueur d’installation incomplète contient l’empreinte et le chemin de l’archive. L’entrypoint qui vérifie ce marqueur est installé en premier ; les fichiers sont remplacés atomiquement et synchronisés sur disque. Le marqueur disparaît après installation complète. Une interruption oblige à réinstaller l’archive identifiée avant de pouvoir lire `.env` ou lancer le déploiement. Les archives d’un échec restent privées pour permettre cette reprise.

Lors de la première transition depuis l’ancien installateur, aucun ancien déploiement ne doit encore s’exécuter : un processus déjà lancé avec l’ancien code ne peut pas recevoir rétroactivement ces garanties.

## Paires et callbacks

Les candidats portent des noms uniques et un label de génération. Ils ne reçoivent aucun alias `api`, `front` ou alias canonique. Toute instance front/API supplémentaire du même projet qui n’appartient pas au journal bloque une nouvelle allocation.

Chaque front reçoit `SSR_API_INTERNAL_URL=http://<son-api>:8080`. Cette valeur sert au proxy Express **et** à Angular : `CommonEngine.render` la fournit au niveau plateforme, `BootstrapContext` conserve cet injecteur parent, et `ServerApiBaseUrlBackend` l’hérite par un token sans factory racine. La réécriture reste après le transfer-cache ; la requête publique et sa clé d’hydratation ne deviennent pas une URL Docker.

Le host canonique et le host propre du candidat sont ajoutés exactement aux `AllowedHosts` de l’API correspondante, sans wildcard. Avant activation, un vrai appel `/health` depuis le front vers cette origine vérifie aussi que ce Host interne est accepté ; le seul healthcheck sur localhost ne suffit pas.

Les nouvelles API utilisent `Ssr__InternalBaseUrl=http://amusementpark-edge:4000`. Les invalidations et les statistiques internes suivent ainsi le front sélectionné, avec le token existant. Lors de la première livraison, les requêtes encore en cours dans l’ancienne API conservent naturellement son ancienne configuration de callback jusqu’à son arrêt ; aucune garantie rétroactive de cohérence instantanée de tous les caches mémoire n’est revendiquée.

Le worker durable du candidat API est désactivé. Le succès exige ensuite la paire canonique saine et son rôle de worker actif ; un candidat qui sert correctement ne suffit jamais à terminer le déploiement.

## Bascule et preuve de drainage

1. Le journal enregistre la paire canonique A et prépare les noms de B avant sa création. B doit être saine, isolée et réellement liée à son API.
2. L’intention d’exposition est persistée **avant** de changer l’include ou de recharger Nginx. L’include sélectionne le nom exact du front B dans le montage de répertoire existant.
3. `/edge-healthz` atteste la nouvelle génération et le PID qui répond. L’inventaire des enfants du même master est lu **après** cette réponse. Le drainage est acquis uniquement lorsque ce PID est le seul enfant vivant. Tout autre enfant interdit le retrait, y compris un worker apparu après le premier relevé et n’ayant pas encore traité son signal d’arrêt. Si le nouveau worker redémarre, une nouvelle attestation est obtenue.
4. Après cette preuve, A est arrêté par IDs : front d’abord, API ensuite. Aucun `rm -f`. Le canonique C est créé et vérifié pendant que B sert les requêtes.
5. La même bascule et la même preuve s’appliquent de B vers C, puis B est retiré. La route attestée est revérifiée avant chaque phase de retrait et avant le succès.

Les identités conteneur/master incluent les PID et dates de naissance des processus. Une identité ambiguë, un worker illisible encore présent, un échec Docker ou un changement du master arrête le processus sans supposer un drainage. Le journal conserve les IDs ; le nom canonique réutilisé pour C ne permet jamais de supprimer C à la place d’A.

Le délai de drainage est de 190 secondes par défaut. Une échéance conserve les deux paires et fait échouer le run. Après retrait du routage, Node ferme son listener et attend ses réponses actives ; son échéance de 180 secondes est une erreur. Docker attend jusqu’à 190 secondes. Un arrêt forcé, OOM ou code de sortie anormal bloque avant suppression du front et retrait de son API. L’ancien Node sans handler peut normalement retourner 143 après SIGTERM lors de la première transition ; cette exception ne s’applique qu’à la génération antérieure au protocole.

## Reprise et abandon

Le journal `deploy/nginx/runtime/deployment.json` est privé et exclu du bundle/Git, comme `active.conf`. Un état invalide ne donne aucune autorisation de nettoyage.

- Après exposition possible, l’installateur termine d’abord le journal avec la configuration déjà installée, avant d’installer le bundle suivant. Une API canonique partiellement créée est reprise par son ID/génération ; aucune troisième paire n’est créée.
- Avant toute exposition, un candidat cassé peut être abandonné si la route et le master d’origine sont inchangés et si A reste saine. Les candidats sont arrêtés front puis API avant toute restauration de données. Un échec de démarrage déjà terminé peut être supprimé dans ce seul parcours, avec son code de sortie journalisé ; un nouvel arrêt forcé ou OOM reste bloquant.
- L’armement du rollback du premier cutover de classement est persisté avant le gel existant. En abandon, les candidats doivent être arrêtés avant le rollback Mongo, puis l’armement est effacé et l’abandon validé. Après intention d’exposition, ce rollback est interdit, même si la réponse au reload a été perdue. Les scripts métier de migration/rollback ne changent pas.
- L’ancien entrypoint de nettoyage par noms est remplacé par un refus explicite, pour écraser aussi sa copie auparavant déployée. Il ne peut plus retirer un secours conservé.

Une reprise ne répare pas automatiquement un journal corrompu, une image manquante, une panne de l’edge, un arrêt forcé/OOM ou une modification de l’infrastructure. Ces situations restent des échecs explicites à examiner ; le script ne promet pas une disponibilité absolue face à une panne du VPS.

## Validation reproductible

Contrôles légers sans serveur :

```bash
python3 -m unittest discover -s deploy/scripts/tests -p 'test_deployment_*.py'
python3 -m unittest deploy/scripts/tests/test_install_deployment.py
bash -n deploy/scripts/deploy.sh deploy/scripts/cleanup-stale-deploy-candidates.sh
node --test FRONT/AmusementPark/tools/server-graceful-shutdown.test.mjs
```

Node 24 est utilisé en CI. Avec Node 22.12, le dernier contrôle nécessite `--experimental-strip-types`. Les tests de verrou/processus Linux sont exécutés en CI, et ignorés sur Windows. Le test Angular d’héritage du token fait partie de `npm run test:ci`.

**Uniquement en CI**, `deployment-transaction-docker.py` démarre les vrais helpers et la configuration Nginx avec des fixtures Node/API isolées. Des rendez-vous prouvent qu’une attente d’en-têtes, un corps déjà reçu et une écriture unique sont en cours avant la bascule. Deux interruptions réelles du coordinateur couvrent B actif et B actif avec C incomplet. Les réponses complètes, les IDs, les callbacks authentifiés, les rôles du worker et les octets/MIME/source du XML statique sont vérifiés. Un contrôle distinct envoie SIGTERM pendant une réponse HTTP directe au front pour exercer le vrai helper d’arrêt Node. Les tests existants conservent également les politiques XML statique activée et désactivée.

Le coût permanent reste celui de la pile existante : aucun nouveau service, ordonnanceur ou dépendance. Le journal et les quelques attestations sont propres à la livraison ; les deux paires restent temporaires, sauf arrêt conservateur après un échec.
