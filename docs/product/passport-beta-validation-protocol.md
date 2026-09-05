# Protocole de validation de la bêta passeport

## Objectif

Vérifier que le passeport apporte plus de valeur qu'un tableur ou une note libre, que ses différents types de notes sont compris et qu'au moins quelques testeurs reviennent terminer une deuxième visite sans assistance.

Le tableau admin donne des indices quantitatifs. Ce protocole recueille les preuves qualitatives utiles au pilotage du produit après sa livraison.

> Décision produit du 5 septembre 2026 : l'absence d'une cohorte disponible ne bloque
> plus l'implémentation des roadmaps suivantes. Les observations terrain restent à
> conduire et à consigner, mais elles ne doivent ni suspendre les travaux techniques
> et métier, ni être déclarées acquises sans preuves réelles.

## Cohorte minimale

Recruter séparément :

- des passionnés tenant déjà un journal de visites ;
- des passionnés sans outil structuré ;
- des visiteurs occasionnels ;
- au moins une personne utilisant principalement le clavier ou un lecteur d'écran ;
- au moins une personne sur téléphone et connexion modestes.

Un petit groupe sert à trouver les problèmes, pas à représenter tout le public.

## Préparation

Pour chaque session :

1. expliquer les données collectées et obtenir le consentement ;
2. laisser la personne choisir compte connecté ou brouillon local sans compte ;
3. noter l'appareil, la largeur d'écran et le mode d'entrée sans enregistrer d'identifiant technique du passeport ;
4. ne pas guider tant que la personne n'est pas réellement bloquée ;
5. utiliser le même ordre de scénarios pour rendre les observations comparables.

## Scénarios

1. Créer une visite récente au jour exact.
2. Ajouter plusieurs attractions, dont plusieurs tours de la même attraction.
3. Ajouter une note de visite et une note sur un tour, puis expliquer avec ses propres mots laquelle influence la communauté.
4. Retrouver les statistiques globales et expliquer un graphique sans se fier uniquement à sa couleur.
5. Corriger une erreur, réordonner la timeline au toucher ou au clavier, puis terminer la visite.
6. Créer une ancienne visite avec l'année seulement et traiter une attraction fermée.
7. Exporter les données et vérifier qu'aucun identifiant technique n'est présenté comme information utile.
8. Supprimer une donnée de test et expliquer ce qui va disparaître.
9. Sans rappel ni assistance, revenir lors d'une session différée pour enregistrer et terminer une deuxième visite.

## Questions après chaque session

- Qu'est-ce que le passeport t'apporte que ton ancien outil ne faisait pas ?
- Le niveau de détail t'a-t-il semblé choisi ou imposé ?
- Quelle note compte pour le classement communautaire ?
- À quel moment as-tu hésité ou perdu confiance ?
- Les statistiques te semblent-elles crédibles et pourquoi ?
- Quelles informations refuses-tu de renseigner ?
- Reviendrais-tu l'utiliser après une vraie visite ?

## Fiche de résultat

Consigner sans données privées :

- profil de test et contexte d'appareil ;
- scénario réussi, réussi avec aide ou échoué ;
- faits observés et courte citation autorisée ;
- problème, sévérité et fréquence ;
- compréhension note globale / note de visite / note de tour ;
- deuxième visite terminée sans assistance : oui/non/non encore observable ;
- décision : corriger, approfondir, accepter avec justification ou arrêter.

## Critères de validation qualitative `PASS-G` — suivi non bloquant

La validation qualitative peut être proposée seulement lorsque :

- toutes les garanties techniques de la roadmap PASS sont encore vérifiées ;
- plusieurs testeurs ont terminé une seconde visite sans assistance ;
- aucun problème critique de confidentialité, d'ownership, d'export ou de suppression ne reste ouvert ;
- les notes sont correctement distinguées pendant les entretiens ;
- le parcours est utilisable à 320, 360, 390 et 768 pixels, au zoom 200 %, au clavier et avec lecteur d'écran ;
- la requête d'agrégation et les écritures Matomo restent compatibles avec le VPS ;
- les limites et résultats non conclusifs sont consignés.

Le signal `Candidate` du tableau de bord n'autorise jamais à lui seul à déclarer la
validation terrain acquise. Le développement, les migrations et les déploiements des
roadmaps suivantes peuvent toutefois avancer dès lors que leurs propres garanties
techniques, métier, de sécurité et de confidentialité sont vérifiées.

## Conditions d'arrêt ou de réduction

Réduire ou redessiner la bêta si la deuxième utilisation reste absente malgré une première activation réussie, si la distinction des notes n'est pas comprise après corrections répétées, si la charge dépasse le budget du VPS ou si la mesure exige plus de données personnelles que ce protocole ne l'autorise.
