# Rétablissement du rendu complet pour Google

Diagnostic du 15 septembre 2026, sur la production 5.3.30 et le code `origin/master`
`e593f4dcf`. Ce correctif vise le HTML livré à Google. Le problème de lecture du
sitemap XML signalé dans Search Console reste une investigation distincte.

## Éléments établis

| Date | Observation |
|---|---|
| 29 juin 2026 | La [PR 239](https://github.com/Ced59/AmusementParkProject/pull/239) retire le JavaScript des réponses aux robots, y compris du repli CSR. Une reproduction du helper historique sur une coquille Angular vide confirme le retrait du script : un HTTP 200 pouvait donc contenir une page vide sans moyen de la rendre côté client. |
| 3 juillet 2026 | La [PR 330](https://github.com/Ced59/AmusementParkProject/pull/330) corrige ce cas : contrôle SEO-ready, conservation du JavaScript lorsque le HTML est incomplet et réponse 503 temporaire aux robots si le SSR est indisponible. |
| 6 juillet 2026 | La [PR 376](https://github.com/Ced59/AmusementParkProject/pull/376) ajoute le retrait des styles de composants, classes et attributs de présentation. Ce compactage reste actif pour Google dans la version 5.3.30. |
| 15 septembre 2026 | Les cinq pages comparées conservent leurs textes, liens, métadonnées et canonical entre navigateur et Googlebot, en HTTP 200. Google reçoit cependant zéro script exécutable et zéro bloc de styles, contre quatre scripts et 14 à 26 blocs de styles pour le navigateur. |

Le [test officiel Google de la fiche Aquashow](https://search.google.com/test/rich-results/result?id=4hJhXzld-VuthDbmtR5cBg),
effectué le 15 septembre à 00 h 06, heure de Paris, récupère la page et identifie
des données structurées valides. Sa capture smartphone montre néanmoins une
présentation déstructurée. Le contenu textuel présent ne suffit donc pas à
garantir un rendu comparable à celui des visiteurs.

La chronologie rend plausible une contribution du défaut initial à la chute des
impressions. Elle ne permet pas d'en mesurer la part ni d'expliquer à elle seule
la persistance de la baisse. Google peut indexer un HTML SSR complet sans exécuter
JavaScript ; le défaut concerne ici le contenu et la présentation effectivement
livrés. [Documentation Google](https://developers.google.com/search/docs/crawling-indexing/javascript/javascript-seo-basics).

## Correction et vérification

Les familles Google reçoivent le HTML SSR complet avec scripts, état Angular,
styles et classes CSS. La politique de rendu froid, les caches, les contrôles
SEO-ready, les réponses d'indisponibilité et le comportement des autres robots
sont conservés ; GoogleOther reste limité au cache lors d'un cache miss.

Les tests vérifient l'identité du HTML avant/après préparation pour Googlebot,
Google-InspectionTool et les autres agents Google reconnus, ainsi que le maintien
du no-JS pour les autres familles. Les contrôles smoke et warmup exigent les
ressources de rendu quand leur User-Agent est Google. Le
[runbook de production](production-seo-audit-runbook.md) est aligné sur cette politique.

Après déploiement, vérifier le HTML et le rendu Google sur des pages publiques
représentatives. Le rétablissement de ces ressources ne prouve ni l'acceptation
du sitemap XML par Search Console ni une amélioration immédiate du classement.
