# Faisabilité — Conversion des tarifs dans une devise préférée

Date de l’étude : 2026-08-10

## Décision immédiate

La donnée de référence reste toujours le montant publié par le parc dans sa devise source. Cette devise, généralement la devise locale du pays du parc, est stockée avec un code ISO 4217 et reste visible sur la page publique, dans l’administration, dans Park Graph et dans chaque relevé historique.

La conversion dans une devise préférée n’est pas intégrée dans cette livraison. Elle est techniquement faisable comme aide de lecture facultative, mais elle ne doit jamais remplacer le prix source ni être présentée comme le montant réellement débité.

## Sources de taux étudiées

### Banque centrale européenne

Le [service REST du portail de données de la BCE](https://data.ecb.europa.eu/help/api/data) fournit des séries quotidiennes et historiques, dont les taux de référence contre l’euro. La [méthodologie officielle](https://data.ecb.europa.eu/methodology/exchange-rates) précise que les taux sont normalement publiés vers 16 h les jours ouvrés. La BCE couvre environ trente devises et rappelle que ses taux sont informatifs : leur usage pour une transaction est déconseillé.

Avantages : source institutionnelle, données datées, historique, absence de dépendance à une clé commerciale.

Limites : couverture incomplète pour un portefeuille mondial, absence de taux intrajournalier et prix final du moyen de paiement nécessairement différent en raison des marges et frais.

### Frankfurter

[Frankfurter](https://github.com/lineofflight/frankfurter) est un service libre et auto-hébergeable qui agrège des taux de référence institutionnels. Son API publique est utilisable sans clé et le projet permet un déploiement local. La version 2 étend la couverture à plusieurs banques centrales, certaines sources optionnelles demandant une inscription ou une clé gratuite.

Avantages : API simple, historique, couverture supérieure à la seule BCE, option d’auto-hébergement.

Limites : dépendance opérationnelle supplémentaire, qualité et fraîcheur variables selon la banque centrale, maintenance d’un agrégateur et absence de garantie que le taux corresponde au taux réellement appliqué au visiteur.

## Architecture recommandée si la conversion est ajoutée

1. Introduire un port applicatif `IExchangeRateProvider`, sans appeler un fournisseur depuis un composant Angular ou un contrôleur.
2. Utiliser d’abord une source institutionnelle, puis éventuellement un fournisseur secondaire explicitement identifié. Ne jamais mélanger silencieusement des taux de dates différentes.
3. Mettre en cache côté backend le couple `deviseSource/deviseCible/dateObservation`, avec une durée adaptée à une publication quotidienne et un dernier taux connu borné dans le temps.
4. Retourner avec toute conversion le montant source, le montant converti, les deux codes ISO, le taux, sa date d’observation et le fournisseur.
5. Afficher le montant source en premier et en permanence. Le montant converti porte une mention « estimation », la date du taux et un avertissement sur les frais du moyen de paiement.
6. Conserver la préférence de devise dans le profil authentifié. Pour un visiteur anonyme, une préférence locale peut être envisagée, mais elle ne doit pas modifier le HTML canonique SSR ni créer des variantes SEO.
7. Prévoir une dégradation sûre : si le fournisseur est indisponible ou le taux trop ancien, masquer seulement l’estimation et continuer à servir le prix source.

## Historique et changements de devise

Chaque instantané annuel conserve sa devise d’origine. Un graphique peut calculer une évolution nominale seulement lorsque les points comparés utilisent la même devise.

Lorsqu’un pays ou un parc change de devise, les montants restent affichés dans des segments distincts. Les relier avec le taux courant serait historiquement faux. Une future vue comparative pourrait appliquer un taux daté pour chaque année ou utiliser une mesure corrigée de l’inflation, mais elle devrait être présentée comme une analyse distincte, avec sa méthode et ses sources, jamais comme le prix historique original.

## Verdict

La conversion préférée est faisable avec un coût technique raisonnable, surtout pour les devises couvertes par la BCE. Une couverture mondiale robuste exige cependant un fournisseur complémentaire ou auto-hébergé, du cache, de l’observabilité et une politique de fraîcheur explicite.

La bonne séquence est donc :

- conserver maintenant les tarifs et leur historique dans leurs devises sources ;
- mesurer les devises réellement présentes dans le portefeuille ;
- choisir ensuite le fournisseur selon cette couverture ;
- livrer la conversion comme estimation optionnelle, sans jamais masquer la devise du parc.
