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

PageSpeed identifie le texte du bandeau de consentement comme élément LCP sur le sélecteur et l’accueil. Le bandeau était absent du HTML SSR et ajouté seulement après l’initialisation JavaScript. Son apparition tardive remplaçait le candidat LCP déjà visible.

Le bandeau requis est maintenant inclus dans le HTML SSR. Le premier rendu navigateur conserve le même DOM ; `afterNextRender` applique ensuite la décision mémorisée. Une première visite conserve son bandeau, une visite avec un choix existant le retire. Le cache public reste anonyme et n’est pas personnalisé selon le consentement. Aucun traceur optionnel n’est autorisé par cette modification ; les gardes existants du service de consentement restent actifs.

Les tests couvrent le rendu initial, le maintien du bandeau sur une première visite, les deux actions de consentement, le retrait après restauration d’un choix et la désactivation du bandeau.

## Points restant à traiter séparément

- JavaScript inutilisé : environ 377–386 Kio sur le sélecteur et l’accueil, 530 Kio sur la fiche de parc.
- CSS inutilisé : environ 144–202 Kio suivant la page. Sa réduction demande un découpage des styles réellement partagés, pas une suppression aveugle.
- Plusieurs logos utilisent des variantes plus larges que leur affichage ; PageSpeed estime 199 Kio économisables sur l’accueil mobile.
- La fiche de parc signale des zones tactiles trop rapprochées et des réponses API 400/404 pour des fonctionnalités optionnelles. Ces réponses ne remplacent pas le contenu SSR du parc par un sélecteur.

Un meilleur LCP est une correction technique mesurable. Il ne suffit pas à attribuer la chute Google à une cause unique ou à garantir un retour du trafic.
