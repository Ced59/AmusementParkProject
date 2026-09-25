# Éviter la boucle HTTP du serveur de secours NPM

Le serveur HTTP par défaut de certaines versions de Nginx Proxy Manager inclut
`assets.conf` alors que son upstream est son propre port 80. Une requête d'icône
avec un Host non configuré boucle et peut épuiser les connexions du proxy partagé.
Ce défaut a été observé sur le VPS ; son rôle dans les erreurs Sitemaps Google
n'est pas établi.

`deploy/scripts/repair-npm-fallback.py` remplace seulement cet include par un
commentaire. La location statique existante sert alors aussi les assets, avec une
vraie 404 pour un fichier absent. Les domaines applicatifs, le challenge ACME,
le blocage des exploits et le refus TLS des noms inconnus sont conservés.

Cette maintenance explicite ne fait **pas** partie des déploiements applicatifs :

```sh
python3 repair-npm-fallback.py --compose-file /chemin/npm/docker-compose.yml
python3 repair-npm-fallback.py --compose-file /chemin/npm/docker-compose.yml --apply
```

Le premier appel décrit les empreintes et les changements sans écrire. Le second
refuse une configuration inconnue ou un fichier opérateur différent, sauvegarde
les originaux en accès privé, valide Compose et Nginx, puis recharge Nginx sans
redémarrer le conteneur. Il ajoute un montage en lecture seule à Compose pour la
prochaine recréation. Ne pas exporter le Compose ni sa sauvegarde : ils peuvent
contenir des paramètres privés. Le script n'affiche pas leur contenu.

Après application, vérifier un asset existant ou absent du Host de secours,
l'accueil de secours, une redirection HTTP applicative, un XML HTTPS et l'absence
de nouvelle boucle dans le journal. Ne pas reproduire la saturation avant la
correction. Une réexécution sur la configuration déjà corrigée est sans effet.

En cas d'échec de validation ou de rechargement, le script restaure les originaux.
Pour un retour arrière ultérieur, comparer les empreintes, restaurer les deux
originaux depuis le répertoire de sauvegarde, valider Nginx puis recharger. Si un
autre opérateur a modifié les fichiers, réconcilier les changements manuellement.

Lors d'une mise à jour de l'image NPM, comparer ce fichier de secours persistant
avec celui fourni par la nouvelle image pour intégrer les évolutions du fournisseur.
Le montage fige ce seul fichier, pas les includes TLS/ACME ni les autres hôtes.
