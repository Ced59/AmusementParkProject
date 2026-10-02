# Choix de langue et affichage initial — 2 octobre 2026

## Diagnostic

Relevés de production effectués sur la version 5.4.44. Aucun travail éditorial ni changement de politique d’indexation.

- Le sélecteur de langue à `/` existe depuis le 20 août 2026 (PR #522, commit `f981bb7db`). Il est postérieur à la rupture de visibilité constatée début juillet.
- Sans cookie, `/` répond en HTTP 200 aux visiteurs et à Googlebot avec huit liens HTML vers les accueils localisés, une canonique propre et un `hreflang` `x-default`.
- Sans cookie, `/fr/home` et la fiche française de Disneyland Park répondent en HTTP 200 à Googlebot avec leur contenu SSR, leur titre propre et `X-AmusementPark-Seo-Ready: true`.
- Un cookie de langue anglaise ne redirige pas `/fr/home`. Le garde de sélection de langue ne s’applique qu’à la racine exacte.
- Le sélecteur racine déclenche actuellement le diagnostic interne `insufficient-body-content` : son texte est inférieur au seuil générique de 500 caractères. Ce marqueur de diagnostic ne provoque ni un HTTP 503, ni un `noindex` sur cette réponse. Il ne prouve pas un défaut d’indexation.
- Google documente explicitement les sélecteurs de langue comme pages `x-default` : <https://developers.google.com/search/docs/specialty/international/localized-versions>.

## Google Search Console

Les données de performance disponibles le 2 octobre s’arrêtent au 29 septembre :

| Mesure | 16–22 septembre | 23–29 septembre |
| --- | ---: | ---: |
| Impressions Web | 17 | 4 |
| Clics Web | 0 | 0 |
| CTR | 0 % | 0 % |
| Position moyenne | 72 | 51,3 |

La diminution des impressions est de 76,5 %, sur un volume très faible. La position moyenne porte sur un ensemble différent de résultats et ne constitue pas une preuve de récupération. Cette fenêtre comprend un seul jour complet après les correctifs déployés le 28 septembre.

Le rapport d’indexation est toujours daté du 21 septembre : 399 pages indexées et 174 exclues. Il ne permet pas de mesurer l’effet des derniers correctifs. Le détail du sitemap `static-it.xml` affiche encore « Impossible de lire le sitemap », dernière lecture le 28 septembre, zéro page découverte.

L’inspection réelle de la fiche de Disneyland Park le 2 octobre à 09 h 19 confirme « Google a accès à cette URL » et « La page peut être indexée ». Le HTML rendu contient le titre français, les cinq paragraphes de présentation et les liens internes du parc. L’index consulté avant ce test indique cependant « Google ne reconnaît pas cette URL » et aucun sitemap référent. La disponibilité technique et la découverte/indexation sont donc deux observations distinctes.

Les logs Nginx du 28 septembre au 2 octobre à 07 h 20 UTC, filtrés par les plages IP officielles de Google et le User-Agent Googlebot, montrent 73 récupérations HTML en HTTP 200 et quatre HTTP 404. Aucune récupération HTML Googlebot de `/` ni aucun HTTP 5xx n’apparaît dans cette fenêtre. Les tests d’inspection et PageSpeed sont comptés séparément. Les seuls téléchargements XML vérifiés sont ceux des tests d’inspection du 28 septembre ; aucune récupération automatique supplémentaire de sitemap n’est observée. Source de vérification des robots : <https://developers.google.com/crawling/docs/crawlers-fetchers/verify-google-requests>.

## PageSpeed Insights avant correction

Mesures de laboratoire, émulation mobile et connexion 4G lente. Aucun jeu de données CrUX disponible : ces valeurs ne représentent pas les Core Web Vitals réels de tous les visiteurs.

| Page | Performance mobile | FCP | LCP | TBT | CLS |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sélecteur `/` | 72 | 2,3 s | 9,1 s | 90 ms | 0,004 |
| Accueil `/fr/home` | 58 | 5,0 s | 12,2 s | 210 ms | 0 |
| Disneyland Park, fiche française | 56 | 6,7 s | 14,9 s | 180 ms | 0,01 |

Accueil français sur ordinateur : performance 94, FCP 0,6 s, LCP 1,1 s, TBT 80 ms, CLS 0,085. Le score SEO Lighthouse est de 100 sur les trois pages ; ce contrôle limité ne garantit pas leur indexation ou leur classement.

Rapports reproductibles :

- <https://pagespeed.web.dev/analysis/https-amusement-parks-fun/75fl9ioacu?form_factor=mobile>
- <https://pagespeed.web.dev/analysis/https-amusement-parks-fun-fr-home/3sua2mfait?form_factor=mobile>
- <https://pagespeed.web.dev/analysis/https-amusement-parks-fun-fr-park-834a7b68-1c6c-42b4-893c-2c5082dbc603-disneyland-paris-disneyland-park/jvu73a5t2s?form_factor=mobile>

## Correction ciblée

### Compression au proxy public

Les GET publics des fichiers JavaScript et CSS ne renvoyaient pas de `Content-Encoding`, malgré une demande explicite `Accept-Encoding: gzip`. La compression NPM était activée globalement mais ses types par défaut ne couvraient pas ces ressources. Une directive `gzip_types` limitée au Proxy Host `amusement-parks.fun` a été persistée dans son Advanced et sa configuration générée le 2 octobre à 07 h 40 UTC, après sauvegarde de la base NPM et du fichier de ce host. `nginx -t` et le rechargement ont réussi. Le niveau 4 limite le coût CPU ; les réponses JSON de l’API ne sont pas ajoutées à cette liste.

| Ressource | Avant | Avec gzip | Réduction |
| --- | ---: | ---: | ---: |
| Chunk JavaScript `5EODF4E3` | 245 804 octets | 83 655 octets | 66,0 % |
| CSS principal `XFBSPPUG` | 217 580 octets | 34 852 octets | 84,0 % |
| Index `sitemap.xml` | 26 561 octets | 1 298 octets | 95,1 % |
| `static-it.xml` | 1 348 octets | 310 octets | 77,0 % |

Les empreintes SHA-256 des réponses non compressées et des réponses gzip décompressées sont identiques pour les quatre ressources. Les deux XML restent valides. `Vary: Accept-Encoding` est présent sur les deux variantes. Les snippets NPM de production et de l’environnement proche production sont documentés dans `deploy/README.md` et `deploy/local/npm-proxy-host-advanced-snippets.md`.

### Rendu initial et connexion facultative

PageSpeed identifie le texte du bandeau de consentement comme élément LCP sur le sélecteur et l’accueil. Le bandeau était absent du HTML SSR et ajouté seulement après l’initialisation JavaScript. Son apparition tardive remplaçait le candidat LCP déjà visible.

Le bandeau requis est maintenant inclus dans le HTML SSR. Le premier rendu navigateur conserve le même DOM ; `afterNextRender` applique ensuite la décision mémorisée. Une première visite conserve son bandeau, une visite avec un choix existant le retire. Le cache public reste anonyme et n’est pas personnalisé selon le consentement. Aucun traceur optionnel n’est autorisé par cette modification ; les gardes existants du service de consentement restent actifs.

Le script Google Identity Services était également chargé depuis `index.html` à chaque visite : environ 98,9 Kio transférés, dont 71,5 Kio signalés inutilisés sur l’accueil. Le contenu du formulaire de connexion est désormais créé uniquement à l’ouverture du dialogue ; son bouton demande alors le SDK Google. Un chargement unique est partagé entre appels concurrents, avec délai maximal de dix secondes et nouvelle tentative possible après une erreur. Le script provient toujours de l’URL officielle ; les vérifications d’authentification et la CSP sont conservées.

Les tests couvrent le rendu initial, le maintien du bandeau sur une première visite, les deux actions de consentement, le retrait après restauration d’un choix et la désactivation du bandeau. Neuf tests supplémentaires couvrent le chargement du SDK, les erreurs, le SSR, l’annulation du rendu et l’association des réponses au bouton d’origine. Un test du header vérifie le dialogue fermé, ouvert, fermé puis rouvert sans doublon. Deux tests du formulaire vérifient qu’une erreur tardive ne produit pas de notification après destruction, tandis qu’un dialogue actif conserve son message d’erreur.

Chaque bouton reçoit un `state` propre, renvoyé par le SDK officiel dans `CredentialResponse`. La fermeture du dialogue retire son callback : une réponse issue d’une ancienne fenêtre Google ne peut pas être transmise au dialogue rouvert. Cette association suit la référence officielle : <https://developers.google.com/identity/gsi/web/reference/js-reference#state>.

## Incident mémoire et nettoyage du VPS

Pendant la sauvegarde précédant le déploiement du 2 octobre, le VPS de 8 Go ne disposait plus que de 22 Mo de RAM disponible, sans swap, avec une charge observée de 73. Les accès SSH et HTTPS expiraient. Un swap de secours de 2 Go, réservé à root et enregistré dans `fstab`, a permis au serveur de retrouver de la marge ; le déploiement a ensuite réussi. Cette mesure ne remplace pas un dimensionnement des caches et de la mémoire de pointe.

À la demande explicite du propriétaire, 157 fichiers de sauvegarde du projet et anciennes copies de configuration ont ensuite été supprimés : 35 596 659 725 octets, soit 33,15 Gio. Le disque est passé de 77 % à 42 % d’occupation, avec environ 57 Gio disponibles. Les copies de configuration du proxy et de `fstab` créées durant cette intervention font partie de ce nettoyage. Les sauvegardes futures restent activées.

## Points restant à traiter séparément

- JavaScript inutilisé avant correction : environ 377–386 Kio sur le sélecteur et l’accueil, 530 Kio sur la fiche de parc. Le SDK Google reporté à la connexion en représente environ 71,5 Kio sur l’accueil ; les autres modules doivent être étudiés selon leur usage réel.
- CSS inutilisé : environ 144–202 Kio suivant la page. Sa réduction demande un découpage des styles réellement partagés, pas une suppression aveugle.
- Plusieurs logos utilisent des variantes plus larges que leur affichage ; PageSpeed estime 199 Kio économisables sur l’accueil mobile.
- La fiche de parc signale des zones tactiles trop rapprochées et des réponses API 400/404 pour des fonctionnalités optionnelles. Ces réponses ne remplacent pas le contenu SSR du parc par un sélecteur.

Un meilleur LCP est une correction technique mesurable. Il ne suffit pas à attribuer la chute Google à une cause unique ou à garantir un retour du trafic.
