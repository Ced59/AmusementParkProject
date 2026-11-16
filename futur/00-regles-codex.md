# 00 — Contrat de travail de Codex

[Retour à la roadmap](README.md)

## 1. Portée et hiérarchie

Ce contrat concerne C01/PARKLINKS, C02/Effet papillon, C03/DoppelPark et C07/Train sans rails, y compris leurs projections, médias, administration et diffusion. Il complète `AGENTS.md` ; il n'autorise aucune dérogation aux règles de sécurité, de publication des données ou de revue du dépôt.

Le propriétaire a demandé une roadmap, pas l'implémentation simultanée des concepts. Un futur « implémente la roadmap » doit être interprété comme « livre le prochain concept autorisé, puis arrête-toi », sauf instruction explicite ultérieure modifiant cette règle. Ne pas ouvrir quatre branches d'implémentation en parallèle.

## 2. Étape obligatoire AVANT le code applicatif

Lire le master distant actuel, les consignes applicables et les dépendances du concept. Puis produire **deux livrables concordants** : un descriptif visible dans la conversation du propriétaire, et son dossier versionné `futur/livraisons/Cxx-cadrage.md` (à créer seulement lors du démarrage effectif).

Le descriptif doit couvrir toutes les rubriques suivantes, avec des exemples concrets et les décisions retenues, et non une simple liste d'intentions :

| Rubrique | Contenu attendu |
| --- | --- |
| Fonctionnalité exacte | Ce que voit le visiteur, ce qu'il peut faire, valeur ajoutée, modes disponibles, hors périmètre |
| Parcours de bout en bout | Arrivée depuis le catalogue ou un réseau, première interaction, résultat, exploration des preuves, partage, retour ultérieur |
| Écrans et états | Mobile/desktop, chargement, vide, données partielles, échec, retrait d'une source, non-connecté, accès interdit, reprise |
| Règles métier | Définitions, unités, calculs, contraintes, seuils, dates, égalités, dénominateurs, absence de données, reproductibilité |
| Données réellement utilisables | Classes/champs/ports inspectés, visibilité, couverture mesurée, normalisation, sources et droits des images |
| Architecture | Répartition Core/Application/Infrastructure/WebAPI/front, dépendances, composants réutilisés et nouveaux fichiers envisagés |
| Persistance et API | Collections et index proposés, contrats de requête/réponse/erreur, pagination, autorisations, concurrence, idempotence |
| Rendu et SEO | Routes localisées, SSR, canonical/hreflang, indexation, Open Graph, caches et invalidation |
| Diffusion | Matrice par réseau : génération, partage utilisateur, publication de marque, prérequis, validation, automatisme, repli manuel |
| Sécurité et vie privée | Périmètre public, sessions anonymes, CSRF/IDOR, rate limiting, données conservées, export/suppression |
| Performances et coût | Volumétrie observée, complexité, budgets, jobs bornés, charge estimée puis mesurée, absence de coût externe |
| Tests et recette | Jeux de données synthétiques, cas nominaux/limites/adverses, vérifications de rendu, calcul et diffusion |
| Livraison | Lots/PR, migrations compatibles, flags, activation, monitoring, retour arrière, preuve de déploiement |
| Écarts et arbitrages | Ce qui diffère de cette roadmap, pourquoi, alternatives écartées et éventuels blocages réels |

Distinguer systématiquement **observé**, **choisi**, **à vérifier**. Ne pas reprendre des volumes de l'ancien brainstorming comme s'ils étaient actuels. Ne pas prétendre qu'un compte social, une permission ou un binaire est disponible sans le vérifier.

Une instruction explicite d'implémentation autorise la poursuite après cette présentation ; ce contrat n'ajoute pas artificiellement une attente de validation à chaque détail. En revanche, un coût, une autorisation de publication, une migration destructive ou un changement majeur de périmètre nécessite un arbitrage explicite. Pendant un blocage, réaliser seulement les tâches sûres du même concept, sans commencer le suivant.

## 3. Verrou de progression

États documentaires : `PLANNED → SCOPED → IMPLEMENTING → IN_REVIEW → MERGED → DEPLOYED → VERIFIED`. `BLOCKED` et `ROLLED_BACK` ne sont jamais équivalents à `VERIFIED`.

Le registre d'avancement du document 08 doit identifier le concept actif, ses PR, les SHA, les exécutions CI/CD et les preuves de recette. Un seul concept peut être entre `SCOPED` et `DEPLOYED`. Les opérations de correction du concept actif restent dans le même périmètre.

Pour débloquer le suivant, il faut :

- toutes les PR nécessaires du concept précédent mergées dans `master`, reviews utiles traitées et checks obligatoires réussis ;
- un déploiement réussi contenant le SHA attendu, pas seulement une image construite ou un tag poussé ;
- fonctionnalité activée pour son public cible, smoke tests et parcours principal réussis sur la production, sans régression bloquante connue ;
- partage visiteur fonctionnel et chaîne de préparation sociale vérifiée ; les API sociales non accessibles doivent avoir un repli manuel utilisable et être déclarées bloquées, jamais fictivement validées ;
- bilan, risques résiduels et preuves consignés dans `futur/livraisons/Cxx-recette.md`, puis état `VERIFIED`.

La publication réelle sur un réseau n'est pas nécessaire pour forcer une autorisation qui manque. Le package prêt à publier et le test isolé du connecteur suffisent lorsque l'activation sociale n'a pas été autorisée ; la limitation reste explicite. Le produit principal ne doit pas dépendre d'un agrément tiers.

Une livraison partielle peut comporter plusieurs PR et déploiements techniques sous flag, mais ne déverrouille pas le concept suivant. Le périmètre de V1 indiqué dans la fiche doit être réellement utilisable. Les évolutions V1.1 ne sont pas automatiquement exigées pour franchir le gate.

## 4. Anticipation autorisée, surarchitecture interdite

Autorisé : `DatasetVersion`, interface d'horloge testable, enveloppe de partage, contrats de rendu sans dépendance métier spécifique, réutilisation du publisher existant, primitives de qualité et test doubles utilisés maintenant.

Interdit : développer le vote pendant C01, créer toutes les collections de C07 pendant C02, ajouter un ordonnanceur général non nécessaire, livrer une page DoppelPark inactive dans une PR Effet papillon, introduire Kafka/Redis/Neo4j ou un nouveau service pour un besoin hypothétique.

Toute anticipation doit mentionner son usage immédiat et un test du concept courant. Une interface spéculative sans consommateur réel doit rester dans la documentation, pas dans le runtime.

## 5. Protocole GitHub

Créer une branche d'intention depuis `origin/master` actualisé. Respecter les règles Git bornées et séquentielles d'`AGENTS.md`. Ne pas toucher aux branches, travaux d'intégration ou PR parallèles sans rapport.

Dans chaque PR en français : concept et lot, expérience livrée, choix techniques, tests réellement exécutés, risques, migrations/flags, données personnelles, effets sociaux, absence de dépense, fichiers supprimés et limites. Appliquer la règle de version du dépôt ; une modification purement documentaire du backlog et de ses règles ne nécessite pas de release applicative.

Avant merge, relire le diff final et les trois surfaces de review : commentaires de conversation, reviews formelles, threads inline. Demander/contrôler la review Codex selon la configuration existante, sans créer une API LLM ou un workflow payant pour la remplacer. Une réaction « eyes » signifie une review en cours, pas une approbation. Un avis sans finding peut arriver comme commentaire ou réaction ; vérifier son contexte et le SHA concerné.

Corriger chaque remarque pertinente, expliquer les désaccords, relancer les checks utiles. Ne pas résoudre artificiellement un thread sans correction ou justification. Après modification significative, demander une nouvelle lecture du diff concerné. Ne pas contourner une review requise, une protection, un check rouge ou une permission manquante. Un Codex indisponible est signalé explicitement, jamais présenté comme une review réussie.

Effectuer le merge avec vérification du SHA de tête. Ensuite contrôler le workflow de déploiement du commit mergé, ses jobs et les vérifications HTTP prévues. Si la CI évite légitimement un déploiement pour une PR documentaire, l'indiquer ; cela ne constitue évidemment pas la livraison d'un concept.

## 6. Autorisations et absence de LLM

La génération de textes d'interface, légendes et supports se fait par gabarits éditorialement relus, règles déterministes et interpolation échappée de faits publics. Les descriptions du catalogue ne sont pas remplacées par ces gabarits. Les traductions sont rédigées/revues lors du développement puis stockées dans les ressources existantes ; aucun service de traduction à l'exécution.

Pas d'appel à OpenAI, Anthropic, Gemini ou autre moteur génératif, pas d'embeddings ni de modèle local, y compris pour choisir les sujets ou générer des vidéos. Pas d'abonnement social de planification. Une API officielle de publication n'est pas une API LLM, mais son activation dépend toujours de l'accès réel, des conditions applicables et de l'absence de coût.

Les tests doivent utiliser des publishers factices ; aucun test CI ne doit publier pour de vrai. Aucune action de publication n'est déclenchée par un GET, une visite de robot, un rendu SSR, un export, un merge documentaire ou un simple clic « préparer ».

## 7. Rapport final obligatoire

Résumer ce qui est réellement livré, avec liens PR, merge SHA, run de déploiement, URL de recette, tests, métriques techniques mesurées, limites et état de chaque canal social. Indiquer expressément que le concept suivant n'a pas été commencé. Ne jamais transformer une absence d'accès à la production en déclaration de succès.
