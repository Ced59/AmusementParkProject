# Publication automatique sur la Page Facebook

## Périmètre

L’intégration publie sur une **Page Facebook** avec l’identité de cette Page. Elle ne publie pas dans un groupe Facebook ni avec le profil personnel de l’administrateur.

Facebook est le premier fournisseur de l’abstraction de publication sociale. Les publications manuelles et les annonces automatiques de nouveaux parcs sont historisées dans MongoDB et peuvent être relancées depuis l’administration lorsqu’un appel Meta échoue.

## Préparer l’accès Meta

1. Utiliser une application Meta dont le compte administrateur possède un accès complet à la Page.
2. Autoriser au minimum la gestion des publications de Page (`pages_manage_posts`) et les accès nécessaires à la récupération de la Page administrée (`pages_show_list` et `pages_read_engagement`, selon le parcours Meta actif).
3. Obtenir un User Access Token durable pour l’application, puis récupérer le Page Access Token avec :

   ```text
   GET https://graph.facebook.com/v24.0/me/accounts?fields=name,id,access_token,tasks
   Authorization: Bearer USER_ACCESS_TOKEN
   ```

4. Vérifier que la Page attendue possède une tâche de création de contenu, par exemple `PROFILE_PLUS_CREATE_CONTENT` dans la réponse des nouvelles Pages.
5. Copier le `id` et le `access_token` de cette Page. Le Page Access Token agit au nom de la Page lors de l’appel `POST /{page-id}/feed`.

Références officielles : [collection Facebook API de Meta](https://www.postman.com/meta/facebook/documentation/r56bjfd/facebook-api) et [espace Facebook officiel de Meta](https://www.postman.com/meta/facebook/overview).

## Configurer GitHub production

Dans l’environnement GitHub Actions `production` :

- créer le secret `PROD_SOCIAL_PUBLISHING_FACEBOOK_PAGE_ACCESS_TOKEN` ;
- créer la variable `PROD_SOCIAL_PUBLISHING_FACEBOOK_PAGE_ID` ;
- créer la variable `PROD_SOCIAL_PUBLISHING_FACEBOOK_PAGE_URL` avec l’URL publique exacte de la Page ;
- conserver `PROD_SOCIAL_PUBLISHING_FACEBOOK_ENABLED=false` pendant le premier déploiement ;
- après vérification de l’état « configuré » dans `/fr/admin/social-publications`, passer `PROD_SOCIAL_PUBLISHING_FACEBOOK_ENABLED=true` et redéployer.

La version Graph API est configurable par `PROD_SOCIAL_PUBLISHING_FACEBOOK_API_VERSION`. Lors d’une montée de version Meta, la modifier sans changement de code après validation dans l’environnement de test.

## Fonctionnement des liens et de l’image Open Graph

Le backend envoie le texte dans `message` et l’URL du site dans le champ Graph API `link`. Facebook explore alors la page liée et construit son aperçu depuis les balises Open Graph SSR, notamment `og:title`, `og:description` et `og:image`.

Les liens manuels sont volontairement limités à l’origine publique configurée par `Seo:PublicBaseUrl`. Cela empêche l’outil d’administration d’être détourné pour publier des liens externes.

## Exploitation

- Ne jamais enregistrer le Page Access Token dans `appsettings.json`, un fichier `.env` commité, une capture ou une discussion.
- Si Meta révoque le jeton, remplacer uniquement le secret GitHub puis redéployer.
- Une annonce automatique en échec n’annule pas l’upsert du parc. Elle reste visible dans l’historique et peut être relancée.
- Une clé de déduplication par réseau et par parc empêche de republier automatiquement le même lancement.
