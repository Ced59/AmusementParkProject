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

- créer le secret `PROD_FACEBOOK_APP_ID` avec l’identifiant numérique public de l’application Meta ;
- créer le secret `PROD_SOCIAL_PUBLISHING_FACEBOOK_PAGE_ACCESS_TOKEN` ;
- créer la variable `PROD_SOCIAL_PUBLISHING_FACEBOOK_PAGE_ID` ;
- créer la variable `PROD_SOCIAL_PUBLISHING_FACEBOOK_PAGE_URL` avec l’URL publique exacte de la Page ;
- conserver `PROD_SOCIAL_PUBLISHING_FACEBOOK_ENABLED=false` pendant le premier déploiement ;
- après vérification de l’état « configuré » dans `/fr/admin/social-publications`, passer `PROD_SOCIAL_PUBLISHING_FACEBOOK_ENABLED=true` et redéployer.

La version Graph API est configurable par `PROD_SOCIAL_PUBLISHING_FACEBOOK_API_VERSION`. Lors d’une montée de version Meta, la modifier sans changement de code après validation dans l’environnement de test.

Quand `PROD_SOCIAL_PUBLISHING_FACEBOOK_ENABLED=true`, `PROD_FACEBOOK_APP_ID` est obligatoire. Le déploiement valide sa présence et le frontend SSR refuse de démarrer si cette configuration est incohérente. `PROD_FACEBOOK_APP_SECRET` reste réservé à Facebook OAuth : il n’est pas requis pour publier sur la Page ni pour émettre `fb:app_id`.

## Synchronisation des modifications et suppressions

L’administration peut modifier, supprimer et synchroniser manuellement les publications créées par le site avec le token de Page actuel.

Pour recevoir automatiquement les modifications ou suppressions faites directement sur Facebook :

1. régénérer le token avec l’autorisation `pages_manage_metadata` en plus des autorisations existantes ;
2. créer les secrets `PROD_SOCIAL_PUBLISHING_FACEBOOK_APP_SECRET` et `PROD_SOCIAL_PUBLISHING_FACEBOOK_WEBHOOK_VERIFY_TOKEN` ;
3. déclarer dans Meta le callback `https://amusement-parks.fun/api/social-publishing/facebook/webhook`, avec le même verify token, et s’abonner au champ Page `feed` ;
4. passer `PROD_SOCIAL_PUBLISHING_FACEBOOK_WEBHOOK_ENABLED=true`, puis redéployer.

Le callback vérifie systématiquement la signature `X-Hub-Signature-256`. Seules les publications déjà suivies par le site sont mises à jour ; les autres contenus de la Page ne sont pas importés.

## Fonctionnement des liens et de l’image Open Graph

Le backend envoie le texte dans `message` et l’URL du site dans le champ Graph API `link`. Facebook explore alors la page liée et construit son aperçu depuis les balises Open Graph SSR, notamment `og:title`, `og:description` et `og:image`.

Les liens manuels sont volontairement limités à l’origine publique configurée par `Seo:PublicBaseUrl`. Cela empêche l’outil d’administration d’être détourné pour publier des liens externes.

Dans l’administration, le collage d’une URL publique reconnue prépare automatiquement un texte bilingue adapté au nom de la fiche parc, du parkItem, de la vidéo ou de la page. Pour un parc, le message annonce explicitement l’ajout de sa fiche sur Amusement-Parks.Fun, invite la communauté à partager son expérience, affiche la version anglaise, puis le lien public canonical et des hashtags francophones et internationaux. Le texte reste modifiable avant l’envoi. Le paramètre technique `facebook-image` reste réservé au lien Graph API utilisé pour l’aperçu et n’est pas ajouté au lien visible dans le message.

Pour les cibles liées à un parc ou un parkItem, les images publiques rattachées à cette entité sont présentées dans un carrousel paginé. Le choix par défaut conserve l’image Open Graph actuelle. Une sélection ajoute au lien publié le paramètre réservé `facebook-image` ; le rendu SSR remplace alors, après l’optimisation robot, l’unique `og:image`, `og:image:secure_url` et `twitter:image` pour cette exploration. Le canonical, la description, le titre et les règles SEO de la page restent inchangés.

Le backend ne fait jamais confiance à l’identifiant envoyé par l’interface : il vérifie à nouveau que l’image est publiée, qu’elle appartient exactement au parc ou au parkItem résolu depuis l’URL et que sa catégorie correspond. Les pages sans propriétaire d’image restent sur leur aperçu automatique.

La variante publique actuelle est `social-preview-v2`. L’ancienne route `social-preview-v1` reste lisible pour les publications existantes. La version d’URL fournit un cache-busting déterministe lors d’une évolution de la variante ; elle ne change pas à chaque requête. Les réponses exigent une revalidation et le stockage vérifie toujours la révision de l’image source avant de réutiliser un JPEG généré.

Le frontend demande au reverse proxy public de ne pas bufferiser les réponses HTML/API dynamiques. Le déploiement télécharge en plus une page publique non compressée et vérifie que le corps reçu correspond à `Content-Length`, afin de détecter une régression de transport telle qu’une coupure à 64 Kio.

Les mêmes garanties sont disponibles au workflow Codex explicite via `ResolveFacebookPublication` puis `PublishFacebook`. Cette surface accepte le texte automatique en omettant `Message`, un texte personnalisé, et seulement une image issue du brouillon paginé de la cible.

## Exploitation

- Ne jamais enregistrer le Page Access Token dans `appsettings.json`, un fichier `.env` commité, une capture ou une discussion.
- Si Meta révoque le jeton, remplacer uniquement le secret GitHub puis redéployer.
- Une annonce automatique en échec n’annule pas l’upsert du parc. Elle reste visible dans l’historique et peut être relancée.
- Une clé de déduplication par réseau et par parc empêche de republier automatiquement le même lancement.
