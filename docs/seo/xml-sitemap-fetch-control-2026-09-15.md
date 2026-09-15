# Un seul fichier témoin pour la récupération XML

## Hypothèse et portée

Des défauts de livraison XML ont existé avant les corrections documentées en juillet et août. Une récupération actuelle conforme ne dit pas comment Google traite une URL soumise antérieurement. Un nom de fichier jamais soumis pendant cette enquête permet de comparer ce traitement à celui du petit sitemap français existant, en changeant seulement le chemin demandé.

Le témoin fixe est `/sitemap-static-fr.xml`. Il reprend exactement les octets de `static-fr.xml` déjà récupéré et validé par le publisher, sans modifier son contenu, ni ajouter de requête backend. Il n’est ajouté ni à l’index XML ni à robots.txt. La livraison emploie le snapshot statique et la route XML déjà présents, sans changement de proxy, DNS ou nom de l’index principal.

Cette expérience ne démontre pas un cache négatif chez Google et ne constitue pas une correction du statut Search Console. La normalisation éventuelle d’un paramètre d’URL par ce service n’est pas établie. Le rapport officiel décrit aussi des récupérations arrêtées après des échecs répétés, des traitements différés et une faible demande d’exploration : [rapport Sitemaps](https://support.google.com/webmasters/answer/7451001?hl=fr).

## Publication et garde-fous

- Le témoin est maintenu lors des publications suivantes tant que sa source française figure dans l’index. Il compte dans le nombre de documents, la borne de volume total et le digest du snapshot ; le Buffer de la source est réutilisé.
- Un snapshot entièrement identique reste `unchanged`. Un témoin manquant est restauré par le mécanisme existant. Une source modifiée entraîne une nouvelle copie byte-identique, pas une version figée indépendante.
- Le résultat et le journal de publication affichent `fetch control=included` lorsque la copie est présente. Si la source n’est plus annoncée, le snapshot principal valide est publié normalement avec `source-missing`, sans témoin fabriqué. Si le nom entre lui-même dans l’index, son document normal est respecté et le statut `name-conflict` invalide l’expérience.
- Si une source annoncée répond en erreur, est vide ou incomplète, la validation habituelle échoue et conserve le dernier snapshot valide. Le témoin ne contourne aucun contrôle de publication.
- Le repli existant vers le répertoire `previous` peut encore servir une ancienne copie si le témoin n’est plus dans `current`. Un simple HTTP200 ne remplace donc pas la vérification du statut `included` et de l’égalité des deux documents actuels.

## Protocole unique après déploiement

Avant toute soumission, vérifier que le journal indique `included`, puis contrôler le HTTP200 direct, la source statique, le type XML, l’intégrité du corps et l’égalité SHA-256 avec `static-fr.xml`. Les neuf URL françaises connues doivent toujours constituer la source au début de l’expérience. Si elle a changé, documenter ce fait et réévaluer la comparaison avant de continuer.

Conserver le nom exact, la version déployée, les heures UTC, les en-têtes, le XML, l’empreinte et le résultat du test actif Google. Soumettre ce nom une seule fois dans Search Console, puis rapprocher les observations du rapport et les requêtes serveur identifiées par les plages officielles de Google. Les contrôles locaux qui emploient un User-Agent Google doivent être distingués des véritables requêtes Google.

Le code n’effectue aucune soumission, aucun appel Search Console ou IndexNow, aucune génération forcée et aucune rotation de noms. La surveillance ne doit pas devenir une succession de fichiers aléatoires ou de soumissions répétées.

## Interprétation et retrait

Un succès du témoin soutiendrait une différence de traitement entre URL, sans établir sa cause ni valider automatiquement l’index principal. Une récupération Google200 suivie d’un échec de traitement persistant déplacerait l’enquête vers ce traitement. Aucune requête Google observée resterait peu discriminante entre attente, faible demande d’exploration et état du service ou du site. Un échec HTTP observé appellerait d’abord une correction de livraison fondée sur sa trace.

Retirer l’ajout dans une PR ciblée lorsque l’issue et les observations ont été archivées et que l’expérience est explicitement close. Ne pas supprimer la copie pendant une attente encore observée, afin de ne pas fabriquer un nouveau404. La suppression de la ligne dans Search Console ne garantit pas que Google oublie son URL. Le repli vers `previous` peut prolonger la disponibilité après le retrait du code ; le constater sans purger les snapshots principaux pour forcer la disparition.

Les résultats de l’expérience et les métriques privées des moteurs restent dans le dossier de diagnostic local. Aucun résultat Google n’est présumé par ce document.
