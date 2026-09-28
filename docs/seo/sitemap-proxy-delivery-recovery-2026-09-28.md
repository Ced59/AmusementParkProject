# Livraison des sitemaps à travers le proxy public

## Défaut reproduit

Les archives du proxy public contiennent 430 erreurs critiques le 5 juillet 2026
et 269 le 6 juillet. Nginx ne pouvait plus créer ses fichiers dans `proxy_temp`.
Une réponse dont les en-têtes avaient déjà été envoyés pouvait alors rester
marquée HTTP 200 tout en arrivant tronquée. Des requêtes XML sont présentes parmi
ces erreurs. Ces traces prouvent un incident de transport pendant ces deux jours,
mais ne prouvent pas la cause unique des variations de classement Google.

Les réponses dynamiques utilisent depuis les corrections de transport
`X-Accel-Buffering: no`. Le chemin statique ajouté en août n’avait pas repris cette
protection : les fichiers XML pouvaient à nouveau dépendre du répertoire
temporaire du proxy externe. Un proxy actuellement sain ne révèle pas ce défaut.

Le test de régression utilise le véritable routage statique, un XML supérieur à
256 Kio et un second Nginx dont le répertoire temporaire a été retiré. Un débit de
sortie limité rend le besoin de mise en tampon reproductible. Avant correction,
le téléchargement échoue avec une connexion fermée prématurément. Après
correction, le document reçu est identique octet par octet à la source.

Le témoin négatif force la mise en tampon en ignorant l’en-tête. Il doit toujours
produire la troncature et l’erreur de répertoire temporaire ; il empêche le test
de réussir simplement parce que le petit document tient dans les tampons réseau.
Ces opérations concernent uniquement des conteneurs et répertoires de test.

## Correction

L’index `/sitemap.xml` et les sous-sitemaps statiques émettent désormais
`X-Accel-Buffering: no`, reconnu par Nginx Proxy Manager. Le proxy transmet le
document sans recourir à son stockage temporaire. Le contenu XML, les règles de
cache, les contrôles de sécurité et les limites de charge restent ceux du chemin
statique existant. Le proxy peut consommer cet en-tête sans le montrer au client
final : sa présence se vérifie au niveau de l’edge interne et son effet par un
téléchargement externe complet.

## Chronologie utile au diagnostic

| Date | Changement ou incident établi |
| --- | --- |
| 29 juin | La PR #239 supprime les scripts des réponses aux robots, y compris du repli CSR. Une coquille sans contenu SSR pouvait donc devenir impossible à rendre. |
| 3 juillet | La PR #330 conserve les scripts des rendus incomplets et renvoie une indisponibilité temporaire aux robots lorsque le SSR manque. |
| 5–6 juillet | Les journaux du proxy attestent la disparition du répertoire temporaire et les échecs de transport associés. |
| 6 juillet | Plusieurs corrections traitent l’écriture HTML, les longueurs et le transport XML. Le compactage de présentation pour Google subsiste. |
| 20 août | Le nombre de fragments XML est réduit et l’invalidation des caches est corrigée. |
| 24–25 août | Les sitemaps passent par un snapshot statique ; le type XML et le rendu du lecteur XML sont corrigés. La directive de désactivation du buffering manque sur cette nouvelle voie. |
| 15 septembre | La PR #739 rétablit scripts, styles et état Angular pour Google. Les PR #740, #742 et #747 rétablissent également la découverte par le plan HTML et la pagination SSR. |
| 28 septembre | La présente correction protège aussi la voie statique contre la panne historique de buffering. |

## Validation et limites

Le diagnostic précédant la correction a déjà confirmé l’intégrité du snapshot
et de son téléchargement public : 192 sous-sitemaps, 106 728 URL uniques,
aucun enfant absent, aucun XML mal formé et aucun corps reçu plus court que son
`Content-Length`. Le test actif Search Console peut récupérer l’index. Le correctif
renforce donc une faiblesse démontrée du transport ; il ne transforme pas cette
observation en preuve d’un rejet actuel dû à Nginx.

Après déploiement, vérifier le XML depuis le domaine public et les en-têtes de
l’edge, puis suivre séparément le traitement des sitemaps dans les moteurs. Une
soumission acceptée ou un test actif réussi ne prouve ni le traitement du sitemap
par le moteur ni l’indexation des URL. Les métriques détaillées et les données
privées des consoles sont conservées dans le diagnostic local.

Références : [rapport Sitemaps de Google](https://support.google.com/webmasters/answer/7451001?hl=fr),
[directive `proxy_buffering` de Nginx](https://nginx.org/en/docs/http/ngx_http_proxy_module.html#proxy_buffering),
[rétablissement du rendu Google](google-rendering-recovery-2026-09-15.md).
