# PASS 18 — Brouillons anonymes du passeport

## Enjeu métier

Un visiteur peut commencer son passeport depuis une fiche de parc sans créer de compte. La visite et ses passages restent privés sur son appareil. Après connexion, il décide explicitement s’il veut les comparer à son passeport puis les importer.

Cette étape réduit la friction à la première utilisation sans affaiblir la confidentialité ni créer silencieusement des doublons.

## Parcours livré

1. La création rapide valide la visite comme pour un utilisateur connecté.
2. Sans session, elle enregistre un brouillon versionné dans IndexedDB ; aucun détail de la visite n’est envoyé au serveur.
3. L’utilisateur peut rouvrir le brouillon, rechercher les attractions publiques, ordonner ses passages, exporter une copie JSON ou purger manuellement les données locales. La fin de la création rapide mène explicitement à la liste du Passeport local afin que ce chemin reste toujours visible.
4. L’action d’import ouvre la connexion puis restaure automatiquement la destination Passeport, y compris son ancre, après authentification.
5. Après connexion, le passeport détecte uniquement en local le nombre de visites et de passages disponibles.
6. Une case et une action distinctes autorisent la comparaison. C’est seulement à ce moment que le parc et l’année servent à rechercher les visites similaires du compte.
7. Pour chaque visite, l’utilisateur choisit de la garder séparée, de la fusionner avec un brouillon serveur après aperçu, ou de l’ignorer.
8. L’import utilise les identifiants d’opération stables du brouillon. Les passages importés portent la source métier `Import`.
9. Le brouillon local n’est supprimé qu’après vérification des accusés de réception de la visite et du contenu exact de tous ses passages. Un échec ou une réponse ambiguë conserve le brouillon pour une reprise idempotente.
10. Dès la première mutation serveur, le brouillon mémorise la stratégie et la cible choisies. Une reprise reste verrouillée sur cette cible et réutilise les mêmes opérations de lot.
11. Cette réservation est atomique dans IndexedDB : si deux onglets tentent le même import, un seul peut choisir la stratégie et l’autre s’arrête avant toute mutation serveur.

## Frontières d’architecture

- Les composants Angular se limitent à l’affichage et aux intentions utilisateur.
- Les façades orchestrent le stockage local, la comparaison, les choix et le rapport final.
- Le stockage IndexedDB implémente un port dédié ; les façades ne dépendent pas directement de l’API navigateur.
- Les appels HTTP passent par les services d’accès aux données et leurs ports.
- Le backend fixe lui-même la source `Import` sur l’endpoint d’import : le client ne peut pas choisir arbitrairement une provenance métier.
- Le handler Application conserve les validations, l’idempotence, le verrou de mutation et la publication d’audit existants.

## Confidentialité et cohérence

- IndexedDB est utilisé à la place de `localStorage`.
- Le schéma local est versionné et validé récursivement avant lecture ou écriture.
- Un brouillon est borné à 2 000 passages développés afin de limiter la mémoire, les temps de traitement et le nombre d’appels.
- Les routes locales sont rendues côté client et marquées `noindex`; aucun contenu personnel n’entre dans le HTML SSR.
- Les métadonnées navigateur de ces routes réutilisent la politique privée du compte et ne présentent jamais une fausse page introuvable.
- La simple détection locale ne déclenche aucun appel contenant un parc, une date, une note ou un passage.
- Dès la première tentative de comparaison, le message de divulgation reste visible même si une comparaison ultérieure échoue ; les erreurs ne prétendent jamais qu’aucune donnée n’a été envoyée.
- Une même date et un même parc ne provoquent jamais une fusion automatique.
- Seule une visite serveur en statut `Draft` peut recevoir une fusion.
- La note privée complète et les passages du brouillon cible doivent être chargés avant que la fusion soit activée.
- Une heure locale n’est conservée que pour une visite au jour exact disposant d’un fuseau horaire ; elle est normalisée au format API avant import.
- La confirmation d’une incohérence historique reste disponible pour chaque attraction : une attraction aujourd’hui ouverte peut ne pas l’avoir été à la date ancienne de la visite.
- Les lots de passages sont bornés à 100 et possèdent chacun une clé idempotente stable.
- Dès qu’un import est réservé, les modifications, suppressions unitaires et purge globale sont bloquées jusque dans la transaction IndexedDB ; le brouillon reste exportable et récupérable.
- Une validation HTTP définitive avant toute mutation libère cette réservation afin que le brouillon puisse être corrigé ou supprimé ; une panne réseau ambiguë conserve au contraire le verrou et les clés de reprise.

## Responsive

Les trois nouvelles surfaces — liste locale, éditeur local et comparaison d’import — bornent leur largeur au viewport, autorisent la réduction de chaque enfant de grille et passent en une colonne sur écran étroit. Le contrat automatisé vérifie `max-width`, `min-width`, l’absence de débordement horizontal et les media queries mobiles.

Les largeurs de validation visuelle sont 320, 360, 390 et 768 pixels.

## Preuves automatisées

- aucune requête serveur avant consentement de comparaison ;
- détection d’une visite de même parc et même date sans mutation ;
- création séparée et suppression locale seulement après accusés vérifiés ;
- reprise d’une mise à jour ambiguë avant import des passages ;
- conservation locale si un accusé serveur est incohérent ;
- conservation locale si le parc, l’attraction, l’heure, le statut, la note privée ou la source d’un passage accusé diffère du lot envoyé ;
- retour sûr vers le Passeport après connexion, sans accepter de destination externe ;
- libération du brouillon après rejet de validation sans mutation, mais maintien du verrou après panne réseau ambiguë ;
- navigation visible vers la liste locale à la fin de la création anonyme ;
- verrouillage d’une reprise partielle sur la visite et les clés de lot originales ;
- exclusion atomique d’un second import concurrent avant toute mutation serveur ;
- gel des modifications et suppressions d’un brouillon dont l’import doit être repris ;
- refus d’une divergence sur le caractère approximatif de la date ;
- chargement de la note privée complète avant fusion ;
- source `Import` imposée par le contrôleur et propagée jusqu’au domaine ;
- validation du schéma local et de sa borne de volume ;
- dernière recherche d’attractions prioritaire en cas de réponses désordonnées ;
- routes locales CSR/noindex et contrat responsive.
