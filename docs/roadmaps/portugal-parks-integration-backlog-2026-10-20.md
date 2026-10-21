# Backlog d’intégration des parcs et attractions autonomes portugais — 2026-10-20

## Objectif et autorisation

Ce backlog fixe le périmètre national demandé le 20 octobre 2026 : Portugal continental, Madère et Açores ; parcs actuels, disparus, projets documentés et attractions fixes autonomes pertinentes. Il comprend les établissements absents du site et ne se limite ni aux montagnes russes ni aux onze fiches existantes à examiner.

L’utilisateur demande d’abord ce backlog, puis l’intégration et la complétude éditoriale avec les mêmes exigences que les autres pays. Il autorise explicitement la publication des fiches et contenus audités ainsi que leur annonce Facebook une fois le score acceptable. La condition normale est un score individuel **strictement supérieur à 95**, un audit complet sans bloqueur et une publication anonyme vérifiée. Aucune exception de score n’a été acceptée pour ce lot.

Chaque parc suit les étapes 0 à 9 de [l’orchestrateur](../codex-guidelines/park-data-integration-orchestrator.md) et le [workflow API Codex](../codex-guidelines/codex-park-data-editor-api-workflow.md). Les attractions réellement isolées suivent le [workflow StandaloneAttraction](../codex-guidelines/standalone-attraction-data-integration.md). Les appels restent séquentiels, avec Status avant toute opération coûteuse et respect des délais globaux.

## Photographie initiale et limites explicites

- Recherche officielle `SearchParks -Query Portugal -Page 1 -PageSize 50` du 20 octobre 2026 : **21 fiches**, sur une page, dont **1 visible**, **10 privées à examiner** et **10 déjà classées NotRelevant**.
- Les onze scores ci-dessous ont été recalculés individuellement avec Completeness. Leur présence ne valide ni le statut Operating hérité ni leur qualité éditoriale.
- La liste publique des attractions autonomes filtrée sur PT renvoie **0 entrée**. Cette lecture anonyme ne prouve pas l’absence de brouillons cachés ; leur résolution par identité et Preview reste obligatoire avant toute création.
- Les cinq pages de l’annuaire national Turismo de Portugal ont été parcourues : **44 entrées touristiques**, avant rapprochement des aliases et décision de pertinence. Les rubriques touristiques ne constituent pas une preuve suffisante qu’un musée, une patinoire ou un jardin soit un parc.
- Le registre DGAV porte un chemin de téléchargement de 2025 mais son document est daté du **28 avril 2021**. Il sert à découvrir les établissements animaliers, jamais à certifier leur ouverture en 2026.
- « Absent de la photographie PT » signifie absent de la recherche nationale ci-dessus. Avant création, rechercher aussi le nom, ses variantes et l’adresse : une fiche mal classée ou non indexée reste possible.
- Ce fichier constitue le recensement national initial sourcé, pas un certificat d’exhaustivité définitive ni un relevé d’intégrations achevées. Toute nouvelle identité fiable découverte pendant les archives, plans et recherches locales rejoint la section appropriée avec mise à jour des compteurs. Les dossiers non tranchés restent visibles ci-dessous.

## Conditions de traitement et de sortie

Chaque ligne reste définitivement dans cet inventaire, y compris après intégration, conformément à l’instruction explicite du propriétaire du 20 octobre 2026. Sa case est cochée seulement après le succès cumulé de l’audit final, de la publication des données et de Facebook. Les dates, scores et liens de publication sont alors ajoutés à la ligne et au journal ci-dessous. Un score élevé, une Preview acceptée, la seule création de la fiche ou un classement de pertinence ne suffisent pas. Une identité non pertinente ou fusionnée reste également documentée avec sa décision et ses sources, sans être présentée comme publiée.

1. Résoudre identité, doublons, pertinence et cycle de vie ; garder les sources, IDs et lacunes dans l’état consolidé. Une attraction située dans un vrai parc reste un parkItem. Aucun nettoyage destructif ou masquage d’une donnée publique n’est implicite.
2. Rechercher offre actuelle, annonces confirmées et inventaire fermé ou déplacé ; inclure zones, attractions, restaurants, boutiques, services, hébergements et autres lieux visiteurs nommables et sourçables. Géolocaliser chaque lieu fixe identifiable sans point inventé.
3. Rédiger et relire personnellement les huit langues, avec des textes naturels et spécifiques pour toutes les entités, médias, timelines et articles. Aucun paragraphe mécanique, traduction brute, conseil d’itinéraire ou fiche technique déguisée n’est accepté.
4. Auditer logo officiel, vues générales, images par attraction, cartes officielles annuelles actuelles et archivées, références, histoire, articles durables, calendrier officiel complet et tarifs applicables. Les lacunes ne sont acceptables qu’après recherche documentée ; ne pas remplacer un sujet manquant par un visuel trompeur.
5. Effectuer l’export complet frais avant l’audit final ; contrôler encodage UTF-8, profondeur, corpus transversal, sources et couverture chiffrée. Exiger un score projeté supérieur à 95 et zéro bloqueur, puis publier de façon ciblée les contenus et médias, les articles et enfin la fiche nouvelle. Recalculer le score réel après publication et vérifier les pages anonymes, historiques et contenus attendus.
6. Résoudre le brouillon Facebook de l’URL canonique. Conserver une annonce existante Published ; sinon employer l’annonce par défaut et son chemin idempotent. Une annonce Failed se reprend seulement avec son ID exact et la commande officielle de reprise, après réconciliation ; jamais par un nouveau post manuel de secours. Pour une attraction autonome, vérifier l’historique exact de sa cible et ne pas supposer la présence d’une clé d’annonce de parc.
7. Marquer la ligne intégrée, sans jamais l’effacer, après publication et score réel supérieur à 95 sans bloqueur, puis annonce Facebook confirmée Published. Conserver les preuves, dates, scores, URLs et lacunes factuelles dans le bilan de lot. Regrouper normalement dix intégrations confirmées dans une PR documentaire cumulative avec un seul incrément de version.

## Journal des intégrations

| Entité | Avancement vérifié | Publication du site après audit | Facebook | Preuve et travail restant |
| --- | --- | --- | --- | --- |
| Aquashow Family Park | Intégration en cours : 56 lots appliqués, 76 parkItems dont onze historiques ; descriptions du parc, des trois zones et des 76 éléments rédigées dans les huit langues | Déjà visible avant ce chantier ; export complet de contrôle reçu ; audit final en cours | Aucune annonce envoyée dans ce chantier | 35 nouveaux éléments et trois nouvelles références de constructeurs conservés privés ; 640 descriptions localisées du parc, des zones et des éléments. Première ouverture indéterminée après découverte d’une archive RTP de 1994 contredisant 2001. Calendrier Indoor jusqu’au 30 avril 2027, soit 212 jours ; 42 offres d’entrée et parking gratuit ; 57 positions vérifiées. 122 images enregistrées, dont 84 nouveaux fichiers privés ; deux doublons privés exclus de la future publication. Trois rattachements de photos corrigés sans suppression. Exploitant Celoli et références utiles enrichis ; Technical Park et Fabbri existants reliés aux attractions. Quatorze événements historiques conservés, dont trois doublons masqués et quatre nouveaux brouillons ; deux articles dans les huit langues, celui de Flash Coaster encore privé. Trois photos historiques de Swim With Sea Lions ajoutées depuis le site de son ancien exploitant. Score projeté de 95 avec blocage éditorial à corriger ; biographies communes Fabbri et MACK préparées, en attente d’autorisation ciblée. Relecture complète des huit langues terminée, corrections ciblées enregistrées et rejouées sans modification restante ; 249 opérations de médias vérifiées, dont une réponse perdue rapprochée d’un export ciblé ; textes alternatifs de 121 images corrigés dans les huit langues. Aucun doublon de champs ni défaut d’encodage détecté après ces corrections. Correction de l’affichage des pays livrée en production 6.0.6 (PR #921) après résolution du blocage Docker par la PR #924 ; pages publiques du parc et de Watercoaster vérifiées sur mobile et ordinateur, sans débordement ni code pays brut. Contrôle final des sources, du score et de la publication encore requis avant Facebook. |
| Magikland | Intégration en cours : 23 lots appliqués, six zones et 41 parkItems privés, dont 23 attractions actuelles et quatre anciennes | Parc, six zones, vingt-trois attractions actuelles, quatre historiques et quatorze services, restaurants ou boutique rédigés et relus dans les huit langues ; audit final à poursuivre | Aucune annonce envoyée | Ouverture du site de Penafiel confirmée au 13 mars 2010 ; réouverture sous le nom Magikland le 16 juin 2012. 40 nouvelles fiches privées, coaster existant conservé et fausse précision au 1er janvier supprimée. 14 emplacements précis vérifiés ; 104 conditions d’accès issues des 23 pages officielles, avec nuances entre recommandations et interdictions. Plans officiels 2017, 2019 et 2026 stockés ; huit photos privées créditées sous licence CC BY 2.0. Anciennes attractions Zambeze, Enterprise, Western Train et Carrossel Pirata intégrées. Exploitant Luso Parques créé et rattaché, quatre constructeurs existants réutilisés et leurs 32 biographies relues sans modification ; six mesures ou capacités vérifiées. Calendrier officiel complet du 1er mars au 30 septembre 2026 : 214 jours, sept dates complètes distinctes des fermetures ; aucun calendrier futur publié trouvé. Seize offres conservées uniquement dans le relevé historique 2026, billetterie officielle en maintenance. Six jalons historiques privés : quatre pour le parc, de l’ouverture en 2010 à la relance de juin 2012, et deux pour Montanha Mágica et Zambeze. Article sur la relance de 2012 intégré dans les huit langues. Logo officiel identifié ; utilisation du logo et des photos officielles restreinte par les conditions du détenteur, autorisation antérieure à vérifier. Score projeté 89/100 ; blocage API : logo manquant. Export complet rapproché des textes rédigés ; 1 672 champs localisés, aucun défaut d’encodage ni doublon de paragraphes détecté. Relecture transversale finale et couverture photographique encore à compléter avant publication. |
| Pena Aventura Park | **Intégration terminée le 21 octobre 2026** ; ligne conservée dans l’inventaire | [Fiche publique](https://amusement-parks.fun/fr/park/55afb230-332d-40a4-8290-a17e8d52566c/pena-aventura-park), score réel **100/100**, aucun bloqueur | [Annonce confirmée Published](https://www.facebook.com/1285475681307050/posts/122123973321424431) | Quatre zones et 41 parkItems, dont quinze attractions actuelles et six historiques au statut actuel indéterminé ; 376 descriptions rédigées et relues dans huit langues. 73 images publiques liées, dont 72 imports audités, logo officiel et cinq vues générales ; dix-huit attractions et huit services illustrés, treize emplacements précis. Plans officiels 2014 et 2026, téléchargements publics vérifiés. 43 conditions d’accès ; 365 jours du 21 octobre 2026 au 20 octobre 2027 sans trou ni chevauchement ; 42 offres actuelles. Cinq jalons canoniques publiés et un article en huit langues, pages anonymes vérifiées. Export de publication rapproché sur 11 241 valeurs sans écart ; encodage et contrôle transversal sans anomalie après relecture. Frise réparée par la PR #925 ; traductions des caractéristiques corrigées par la PR #926, version 6.0.8 déployée et contrôlée dans huit langues et quatre largeurs d’écran. Rejouer la publication du parc et la réparation historique ne produit aucune modification. Images de l’hôtel exclues selon ses conditions de réutilisation ; lacunes photographiques, géographiques et descriptions courtes documentées individuellement après recherche. |
| Porta do Mezio | **Intégration terminée le 21 octobre 2026** ; ligne conservée dans l’inventaire | [Fiche publique](https://amusement-parks.fun/fr/park/ebeea550-cb5a-4855-a67b-9b803faa81da/porta-do-mezio), score réel **100/100**, aucun bloqueur | [Annonce confirmée Published](https://www.facebook.com/1285475681307050/posts/122123994243424431) | Deux zones et 35 parkItems, dont 21 attractions actuelles et quatre historiques au statut actuel indéterminé ; 304 descriptions et 16 biographies rédigées et relues dans huit langues. 40 images publiques, logo officiel et cinq vues générales distinctes ; 18 attractions et trois autres lieux illustrés. Six positions vérifiées sur 29 lieux fixes ; six ensembles ou activités mobiles sans point unique. Huit conditions d’accès, 365 jours de calendrier sans trou ni chevauchement, deux offres actuelles et 29 offres historiques sur huit années (2016, 2018 à 2024). Neuf jalons canoniques publiés, tous sourcés et huit illustrés ; article en huit langues sur le parc biologique de 2022. Export de publication rapproché sur 5 084 valeurs sans écart ; 32 pages publiques contrôlées dans huit langues, sans erreur ni débordement. Encodage et répétitions contrôlés après corrections ; réparation de la frise rejouée sans changement. Plans 2016/2024 introuvables en téléchargement, PDF courant sans année vérifiable ; lacunes photographiques et géographiques nommées dans le rapport. Deux doublons de citations retirés des données de l’article et vérifiés dans son API publique ; ancienne liste encore visible dans le cache HTML, sans perte des six sources uniques, suivi de propagation conservé. |

**Bilan vérifié : 2 intégrations terminées sur 112 entrées ; 110 entrées restent à traiter ou qualifier ; 2 nouvelles publications de parcs et 2 annonces Facebook confirmées publiées.** Les 110 entrées initiales, Lusitanix et le parc d’aventure de Carvalhelhos restent présents dans les rubriques ci-dessous.

Les horaires et tarifs courants ne sont applicables qu’aux entités Operating. Une fermeture de saison n’est pas une fermeture définitive. La météo est alimentée par batch : une prévision momentanément vide ne bloque pas la publication si l’affichage se comporte correctement.

## Compteurs actifs

| Catégorie | Initial | Ajouts | Restant |
| --- | ---: | ---: | ---: |
| Fiche déjà publique à auditer | 1 | 0 | 1 |
| Fiches privées existantes de parcs en activité | 4 | 0 | 2 |
| Fiches existantes à reprendre historiquement | 6 | 0 | 6 |
| Parcs thématiques absents de la photographie PT | 8 | 0 | 8 |
| Parcs aquatiques absents de la photographie PT | 22 | 0 | 22 |
| Parcs animaliers et établissements aquatiques permanents à qualifier | 23 | 0 | 23 |
| Parcs d’aventure à installations fixes | 22 | 1 | 23 |
| Parcs familiaux indoor et de plein air | 11 | 0 | 11 |
| Parcs historiques absents de la photographie PT | 6 | 0 | 6 |
| Projets annoncés, en construction ou historiques | 4 | 1 | 5 |
| Attractions autonomes à intégrer ou à qualifier | 3 | 0 | 3 |
| **Total des lignes actives à traiter ou qualifier** | **110** | **2** | **110** |

Les lignes d’établissements à qualifier ne sont pas comptées comme fiches déjà créées. Les pistes supplémentaires et exclusions de découverte restent hors de ce total.

## 01. Fiche déjà publique à auditer

- [ ] Aquashow Family Park — Quarteira — `5b920ecc-397d-4402-b4a5-0952ec8058d8` — score 97 — [TP](#sources-de-découverte). Audit complet et vérification Facebook requis malgré le score déjà supérieur à 95.

## 02. Fiches privées existantes de parcs en activité

- [ ] Magikland — Penafiel — `f4b7fb8d-d2e0-4337-bd25-8f22cc652383` — score 49 — [RC](#sources-de-découverte). Inventaire et complétude éditoriale à reprendre intégralement.
- [x] Pena Aventura Park — Ribeira de Pena — `55afb230-332d-40a4-8290-a17e8d52566c` — **intégré le 21 octobre 2026, score réel 100/100** — [Site](https://amusement-parks.fun/fr/park/55afb230-332d-40a4-8290-a17e8d52566c/pena-aventura-park) · [Facebook Published](https://www.facebook.com/1285475681307050/posts/122123973321424431). Audit, frise, article, traductions et plans publics vérifiés ; lacunes factuelles documentées dans le journal. Ligne conservée.
- [x] Porta do Mezio — Arcos de Valdevez — `ebeea550-cb5a-4855-a67b-9b803faa81da` — **intégré le 21 octobre 2026, score réel 100/100** — [Site](https://amusement-parks.fun/fr/park/ebeea550-cb5a-4855-a67b-9b803faa81da/porta-do-mezio) · [Facebook Published](https://www.facebook.com/1285475681307050/posts/122123994243424431). Inventaire actuel et historique, huit langues, frise et article publics vérifiés ; petite montagne russe distincte d’un alpine coaster. Lacunes documentées dans le journal. Ligne conservée.
- [ ] Zoomarine Albufeira — À préciser — `360d3e08-ccdf-49b5-a2d0-cec0d746367b` — score 50 — [RC](#sources-de-découverte). Inventaire et complétude éditoriale à reprendre intégralement.

## 03. Fiches existantes à reprendre historiquement

- [ ] Bracalândia — À préciser — `fa39635a-d689-43af-a243-ee43c537fa3b` — score 47 — [RC](#sources-de-découverte). Le statut initial Operating est à corriger après vérification historique. Braga et Penafiel sont deux implantations ; la seconde appartient à l’histoire de Magikland.
- [ ] Feira Popular de Lisboa — À préciser — `1d9eeb5e-39e5-4aa6-81fd-9bced242295e` — score 46 — [RC](#sources-de-découverte). Le statut initial Operating est à corriger après vérification historique.
- [ ] Funcenter Lisbon — À préciser — `4b2af208-18ef-4fe6-b2e7-d494a50a6bfa` — score 46 — [RC](#sources-de-découverte). Le statut initial Operating est à corriger après vérification historique.
- [ ] Funcenter Matosinhos — À préciser — `13abce7c-9d1a-4a45-9d9b-de18390eaad3` — score 46 — [RC](#sources-de-découverte). Le statut initial Operating est à corriger après vérification historique.
- [ ] Luna Parque — À préciser — `b6b835a4-4d90-4c27-a4bb-00315544e51f` — score 40 — [RC](#sources-de-découverte). Le statut initial Operating est à corriger après vérification historique. Parc Eduardo VII, années 1930 ; ne pas confondre avec une fête foraine actuelle.
- [ ] RAF Park — À préciser — `06a6610d-bcf7-4497-9476-6500f25a4dd8` — score 40 — [RC](#sources-de-découverte). Le statut initial Operating est à corriger après vérification historique.

## 04. Parcs thématiques absents de la photographie PT

- [ ] Portugal dos Pequenitos — Coimbra — identité applicative à résoudre — [TP](#sources-de-découverte). Parc miniature et patrimonial ; ne pas limiter l’inventaire aux répliques les plus connues.
- [ ] Dino Parque Lourinhã — Lourinhã — identité applicative à résoudre — [TP](#sources-de-découverte). Parcours thématiques, modèles nommés, activités et services ; annonces durables à rechercher.
- [ ] Parque Temático da Madeira — Santana, Madère — identité applicative à résoudre — [MA](#sources-de-découverte). Pavillons, attractions et espaces ethnographiques ; rechercher également les équipements disparus.
- [ ] KidZania Lisboa / Portugal — Amadora — identité applicative à résoudre — [TP](#sources-de-découverte). Ville miniature à activités nommées ; réutiliser une seule identité.
- [ ] Parque dos Monges — Alcobaça — identité applicative à résoudre — [TP](#sources-de-découverte). Parc thématique, animalier et d’aventure.
- [ ] Parque da Mina — Monchique — identité applicative à résoudre — [TP](#sources-de-découverte). Parc rural et ethnographique avec contenus visiteurs nommés.
- [ ] A Quinta — Parque Temático Rural — Localité à confirmer — identité applicative à résoudre — [TP](#sources-de-découverte). Vérifier l’identité et l’adresse officielles avant toute création.
- [ ] Family Golf Park — Vilamoura — identité applicative à résoudre — [TP](#sources-de-découverte). Deux parcours romains nommés ; qualifier le parc de loisirs et son inventaire, sans le confondre avec un golf sportif.

## 05. Parcs aquatiques absents de la photographie PT

- [ ] Slide & Splash — Lagoa — identité applicative à résoudre — [TP](#sources-de-découverte). Aquaparque Slide and Splash est un alias ; une seule fiche canonique.
- [ ] Aqualand Algarve — Alcantarilha, Silves — identité applicative à résoudre — [TP](#sources-de-découverte). Big One et Aqualand Big One sont des anciens noms, pas des parcs supplémentaires.
- [ ] Naturwaterpark — Vila Real — identité applicative à résoudre — [TP](#sources-de-découverte). Parc aquatique et aventure avec hébergement ; inventorier les deux offres.
- [ ] Parque Aquático de Amarante — Amarante — identité applicative à résoudre — [TP](#sources-de-découverte). Complexe Tâmega ; confirmer les nouveautés actuelles et les anciennes installations.
- [ ] Parque Aquático de Fafe — Fafe — identité applicative à résoudre — [FA](#sources-de-découverte). Ne pas confondre avec les parcs aquatiques gonflables de la commune.
- [ ] Norpark — Nazaré — identité applicative à résoudre — [TP](#sources-de-découverte). Aquaparque permanent ; confirmer l’exploitation actuelle.
- [ ] Panorâmico Aquaparque — Pombal — identité applicative à résoudre — [TP](#sources-de-découverte). Identité et exploitation à recouper avec les pages officielles actuelles.
- [ ] Sportágua — Peniche — identité applicative à résoudre — [TP](#sources-de-découverte). Inventaire complet des installations, services et histoire.
- [ ] Mariparque — Praia da Vieira — identité applicative à résoudre — [TP](#sources-de-découverte). Parc aquatique associé aux hôtels Cristal ; éviter une fiche par hôtel.
- [ ] Aqua Gruta — Mira de Aire — identité applicative à résoudre — [TP](#sources-de-découverte). Isoler les installations du parc aquatique des grottes touristiques voisines.
- [ ] El-Xadai — Elvas — identité applicative à résoudre — [TP](#sources-de-découverte). Qualifier le périmètre aquatique et hôtelier ainsi que son statut actuel.
- [ ] Vaga Splash — Vagos — identité applicative à résoudre — [VA](#sources-de-découverte). Parc fixe repéré hors de l’inventaire initial du site.
- [ ] Aqualife / Douro Aqua Fun / Aquafixe — Torre de Moncorvo — identité applicative à résoudre — [PU](#sources-de-découverte), [PN](#sources-de-découverte). Une identité à réconcilier ; ne pas créer trois parcs à la même adresse.
- [ ] Oásis — Parque Aquático das Galveias — Galveias, Ponte de Sor — identité applicative à résoudre — [PU](#sources-de-découverte). Parc aquatique ; ne pas reprendre les tarifs d’un revendeur.
- [ ] Live Beach — Mangualde — identité applicative à résoudre — [AQ](#sources-de-découverte). Plage artificielle structurée ; contrôler l’inventaire fixe et l’exploitation actuelle.
- [ ] Praia das Rocas — Castanheira de Pera — identité applicative à résoudre — [AQ](#sources-de-découverte). Parc aquatique distinct du projet São João da Mata.
- [ ] Aquaparque da Madeira — Santa Cruz, Madère — identité applicative à résoudre — [MQ](#sources-de-découverte). Aquaparque de Santa Cruz est un alias ; une seule fiche.
- [ ] Aquaparque dos Açores / Atlântico Splash — Vila Franca do Campo, São Miguel — identité applicative à résoudre — [AZ](#sources-de-découverte). Réouverture en 2022 ; une seule histoire pour les différents noms.
- [ ] Parque Aquático de Santarém — Santarém — identité applicative à résoudre — [SA](#sources-de-découverte). Différencier les équipements de loisirs des bassins de natation du complexe.
- [ ] Scorpio — Piscinas ao Ar Livre — Guimarães — identité applicative à résoudre — [SC](#sources-de-découverte). Parc aquatique de loisirs ; vérifier le contenu actuel après requalification.
- [ ] Valença Aqua Park — Valença — identité applicative à résoudre — [PN](#sources-de-découverte). Vérifier site fixe, équipements, saison et identité avant intégration.
- [ ] Aquaplay — Regadas, Fafe — identité applicative à résoudre — [PN](#sources-de-découverte). Vérifier les installations fixes et distinguer les établissements homonymes.

## 06. Parcs animaliers et établissements aquatiques permanents à qualifier

- [ ] Zoo Santo Inácio — Avintes, Vila Nova de Gaia — identité applicative à résoudre — [DG](#sources-de-découverte). Registre historique de licence ; statut courant à vérifier sur le site du zoo.
- [ ] Zoo da Maia — Maia — identité applicative à résoudre — [DG](#sources-de-découverte). Même exigence de descriptions, d’enclos nommés, de médias et d’histoire.
- [ ] Parque Biológico de Gaia — Avintes — identité applicative à résoudre — [DG](#sources-de-découverte). Parc animalier et interprétation ; conserver son identité distincte du Zoo Santo Inácio.
- [ ] Zoo de Lourosa — Lourosa — identité applicative à résoudre — [DG](#sources-de-découverte). Parque Ornitológico de Lourosa est un alias.
- [ ] Estação Litoral da Aguda — Arcozelo — identité applicative à résoudre — [DG](#sources-de-découverte). Qualifier l’offre d’aquarium et les contenus nommables avant de choisir le modèle.
- [ ] Aquamuseu do Rio Minho — Vila Nova de Cerveira — identité applicative à résoudre — [DG](#sources-de-découverte). Établissement aquatique permanent ; vérifier le modèle et les contenus visiteurs.
- [ ] Parque Ambiental de Ribeira de Pena — Ribeira de Pena — identité applicative à résoudre — [DG](#sources-de-découverte). Distinguer le parc animalier du Pena Aventura Park.
- [ ] Centro de Educação Ambiental de Vinha de Abade — Cabeceiras de Basto — identité applicative à résoudre — [DG](#sources-de-découverte). Confirmer le nom complet, l’accueil public et le périmètre animalier.
- [ ] SEA LIFE Porto — Porto — identité applicative à résoudre — [DG](#sources-de-découverte). Aquarium permanent avec espaces nommés ; vérifier les huit langues et l’historique.
- [ ] Parque Biológico da Serra das Meadas — Lamego — identité applicative à résoudre — [DG](#sources-de-découverte). Ne pas confondre avec une réserve naturelle non aménagée.
- [ ] Parque Biológico de Vinhais — Vinhais — identité applicative à résoudre — [DG](#sources-de-découverte). Espaces animaliers et de loisirs nommés ; statut courant à confirmer.
- [ ] Europaradise — Montemor-o-Velho — identité applicative à résoudre — [DG](#sources-de-découverte). Résoudre l’exploitant et son adresse ; ne pas déduire la ville du siège social.
- [ ] Parque Biológico da Serra da Lousã — Miranda do Corvo — identité applicative à résoudre — [DG](#sources-de-découverte). Parc animalier structuré ; ne pas englober artificiellement toute la destination.
- [ ] Parque Ecológico de Gouveia — Gouveia — identité applicative à résoudre — [DG](#sources-de-découverte). Parque Zoológico de Gouveia est à rapprocher de cette identité.
- [ ] Jardim Zoológico de Lisboa — Lisboa — identité applicative à résoudre — [DG](#sources-de-découverte). Parc majeur ; inventaire actuel, anciens sites et anciennes attractions à rechercher.
- [ ] Oceanário de Lisboa — Lisboa — identité applicative à résoudre — [DG](#sources-de-découverte). Aquarium permanent ; inventaire des espaces nommés et récit de l’Expo 98 à sourcer.
- [ ] Aquário Vasco da Gama — Cruz Quebrada, Oeiras — identité applicative à résoudre — [DG](#sources-de-découverte). Établissement historique distinct de l’Oceanário.
- [ ] Badoca Safari Park — Santiago do Cacém — identité applicative à résoudre — [DG](#sources-de-découverte). Parc animalier hybride ; intégrer les attractions fixes également.
- [ ] Monte Selvagem — Lavre, Montemor-o-Novo — identité applicative à résoudre — [DG](#sources-de-découverte). Parc animalier et loisirs ; vérifier les équipements nommés.
- [ ] Fluviário de Mora — Mora — identité applicative à résoudre — [DG](#sources-de-découverte). Aquarium et espaces permanents ; périmètre public à établir.
- [ ] Krazy World — Algoz, Silves — identité applicative à résoudre — [DG](#sources-de-découverte). Parc animalier et loisirs aquatiques ; vérifier l’offre complète.
- [ ] Zoo de Lagos — Lagos — identité applicative à résoudre — [DG](#sources-de-découverte). Zoolagos et Pelicanzoo sont à réconcilier avec une seule identité de parc.
- [ ] Parque Zoológico da Povoação — Povoação, São Miguel — identité applicative à résoudre — [PZ](#sources-de-découverte). Ne pas déduire l’inventaire d’une seule mention municipale ; visite publique et enclos à vérifier.

## 07. Parcs d’aventure à installations fixes

- [ ] Parque de Animação Turística e Aventura de Carvalhelhos — Carvalhelhos, Boticas — identité applicative à résoudre — [BC](#sources-de-découverte). Parc fixe documenté dès 2009 et utilisé pour un séjour jeunesse en 2022 ; distinct du Boticas Parque – Natureza e Biodiversidade. Concession Pena Aventura signée le 15 juillet 2011, résiliation décidée par la municipalité le 3 juin 2015. Vérifier l’accès public et le cycle de vie actuels ; ne pas déduire une fermeture de mentions isolées.
- [ ] Adrenaline Adventures Madeira — Porto Moniz, Madère — identité applicative à résoudre — [AA](#sources-de-découverte). Site fixe avec tyroliennes et balançoire géante ; une identité de parc à résoudre, sans doublons standalone pour ses installations internes.
- [ ] Campo Aventura — Óbidos — identité applicative à résoudre — [TP](#sources-de-découverte). Installations fixes et offre de loisirs nommée à vérifier.
- [ ] Radical Park / Complexo Desportivo Príncipe Perfeito — Viseu — identité applicative à résoudre — [TP](#sources-de-découverte). Vérifier le périmètre de loisirs et le cycle de vie ; distinguer les seules activités sportives.
- [ ] Vivapark — Localité à confirmer — identité applicative à résoudre — [TP](#sources-de-découverte). Identité et adresse à résoudre avant tout Apply.
- [ ] Fun Parque São João — São João da Caparica, Almada — identité applicative à résoudre — [TP](#sources-de-découverte). Parc multi-activités permanent ; conserver un seul site.
- [ ] Adventure Park Jamor — Cruz Quebrada, Oeiras — identité applicative à résoudre — [TP](#sources-de-découverte). Site distinct du Pinhal da Paiã, bien qu’ils partagent un exploitant.
- [ ] Adventure Park Pinhal da Paiã — Odivelas — identité applicative à résoudre — [TP](#sources-de-découverte). Vérifier exploitation actuelle et adresse de cette implantation.
- [ ] Parque Aventura Timeout — Ponte de Lima — identité applicative à résoudre — [TP](#sources-de-découverte). Quinta do Casal do Condado ; confirmer le site actuel.
- [ ] Cova da Baleia — Mafra — identité applicative à résoudre — [TP](#sources-de-découverte). Parc multi-activités ; distinguer les équipements installés et les activités externes.
- [ ] Parque Aventura São Jorge — São Jorge, Porto de Mós — identité applicative à résoudre — [TP](#sources-de-découverte). Parcours thématisés de la bataille d’Aljubarrota.
- [ ] Parque Aventura Vila Real de Santo António — Vila Real de Santo António — identité applicative à résoudre — [TP](#sources-de-découverte). Implantation précise du réseau ; ne pas créer une fiche générique pour l’exploitant.
- [ ] Parque Aventura Lagos — Lagos — identité applicative à résoudre — [TP](#sources-de-découverte). Implantation précise du réseau ; statut actuel à confirmer.
- [ ] Parque Aventura Albufeira — Albufeira — identité applicative à résoudre — [TP](#sources-de-découverte). Implantation précise du réseau ; statut actuel à confirmer.
- [ ] Parque Aventura Figueira da Foz — Figueira da Foz — identité applicative à résoudre — [TP](#sources-de-découverte). Implantation précise du réseau ; statut actuel à confirmer.
- [ ] Parque Aventura da Fóia — Monchique — identité applicative à résoudre — [TP](#sources-de-découverte). Vérifier le site fixe et son exploitation actuelle ; ne pas inventer une fermeture.
- [ ] Picoto Park — Braga — identité applicative à résoudre — [PI](#sources-de-découverte). Installations d’aventure, loisirs et services propres au parc.
- [ ] Rates Park — Póvoa de Varzim — identité applicative à résoudre — [RA](#sources-de-découverte). Distinguer les équipements permanents des activités délocalisées.
- [ ] Azurara Parque Aventura — Vila do Conde — identité applicative à résoudre — [ZU](#sources-de-découverte). Inventaire officiel comprenant parcours, jeux et activités mécaniques.
- [ ] DiverLanhoso — Póvoa de Lanhoso — identité applicative à résoudre — [DI](#sources-de-découverte). Grand parc structuré ; ne pas réduire l’inventaire au slide.
- [ ] DiverMinho — Caminha — identité applicative à résoudre — [DI](#sources-de-découverte). Confirmer le périmètre fixe réellement exploité ; ne pas importer les sorties nautiques comme attractions installées.
- [ ] DiverEstrela — Serra da Estrela — identité applicative à résoudre — [DI](#sources-de-découverte). Annonce et mention par l’exploitant ; implantation et ouverture effective à confirmer.
- [ ] Azores Treetop Park — Furnas, São Miguel — identité applicative à résoudre — [AT](#sources-de-découverte). Six parcours installés en 2023 ; vérifier le nom actuel et le calendrier auprès de l’exploitant.

## 08. Parcs familiaux indoor et de plein air

- [ ] Neofun — Faro — identité applicative à résoudre — [TP](#sources-de-découverte). Parc indoor ; qualifier les installations permanentes et leur intérêt éditorial.
- [ ] Polar & Brincar — Viseu — identité applicative à résoudre — [TP](#sources-de-découverte). Parc familial indoor distinct de la patinoire sportive voisine.
- [ ] Feijão Verde Natura Alfragide — Alfragide — identité applicative à résoudre — [FV](#sources-de-découverte). Une fiche par implantation ; laser tag et autres activités du même site restent des parkItems.
- [ ] Feijão Verde Fun Park Alfragide — Alfragide — identité applicative à résoudre — [FV](#sources-de-découverte). Distinguer l’implantation indoor de Natura ; vérifier les adresses et la réouverture annoncée avant création.
- [ ] Feijão Verde Fun Park Coimbra — Coimbra — identité applicative à résoudre — [FV](#sources-de-découverte). Ne pas dupliquer Arena Xtreme si elle appartient au même lieu.
- [ ] Feijão Verde Fun Park Gaia — Vila Nova de Gaia — identité applicative à résoudre — [FV](#sources-de-découverte). Identité précise et inventaire actuel à établir.
- [ ] Feijão Verde Natura Gaia — Vila Nova de Gaia — identité applicative à résoudre — [FV](#sources-de-découverte). Vérifier la distinction physique avec le Fun Park indoor.
- [ ] Feijão Verde Fun Park Montijo — Montijo — identité applicative à résoudre — [FV](#sources-de-découverte). Ne pas créer un second parc pour Arena Xtreme sans site distinct.
- [ ] Feijão Verde Fun Park Sintra — Sintra — identité applicative à résoudre — [FV](#sources-de-découverte). Laser Tag Sintra reste dans le parc si l’adresse est identique.
- [ ] Feijão Verde Natura Sintra — Sintra — identité applicative à résoudre — [FV](#sources-de-découverte). Parc extérieur distinct du Fun Park indoor ; adresse à confirmer.
- [ ] Feijão Verde Spot Setúbal — Setúbal — identité applicative à résoudre — [FV](#sources-de-découverte). Installation permanente à qualifier, sans confondre avec une animation temporaire.

## 09. Parcs historiques absents de la photographie PT

- [ ] Aqualine — Altura, Castro Marim — identité applicative à résoudre — [AL](#sources-de-découverte). Ancien aquaparque confirmé ; dates et nom de l’exploitant à rechercher.
- [ ] Aquaparque do Restelo — Lisboa — identité applicative à résoudre — [RE](#sources-de-découverte). Parc fermé en 1993 ; histoire et incidents à documenter sobrement.
- [ ] Ondaparque — Trafaria, Almada — identité applicative à résoudre — [ON](#sources-de-découverte). Parc aquatique disparu ; croiser le site mémoriel avec presse et archives officielles.
- [ ] Beja Aquática — Nossa Senhora das Neves, Beja — identité applicative à résoudre — [BE](#sources-de-découverte). Ancien parc reconnu par le PDM ; ne pas publier l’année 1992 sans preuve dédiée.
- [ ] Aquaparque Teimoso — Figueira da Foz — identité applicative à résoudre — [TE](#sources-de-découverte), [DF](#sources-de-découverte). Distinguer la fermeture du parc aquatique de celle du restaurant et de l’hôtel.
- [ ] Atlântico Park — Loulé — identité applicative à résoudre — [DF](#sources-de-découverte). Piste historique à documenter : ne pas copier une date d’une liste secondaire dans le graphe.

## 10. Projets annoncés, en construction ou historiques

- [ ] Viva Mundo — World of Football — Santarém — identité applicative à résoudre — [VM](#sources-de-découverte). Projet annoncé en 2026 ; ouverture cible 2030, ne pas présenter le chantier comme commencé sans preuve.
- [ ] Parque Temático de São João da Mata — Castanheira de Pera — identité applicative à résoudre — [SJ](#sources-de-découverte). Projet annoncé en juin 2026 avec alpine coaster ; statut Planned tant qu’aucun début de chantier n’est vérifié.
- [ ] Lusolândia — Azambuja / Alenquer — identité applicative à résoudre — [LS](#sources-de-découverte). Projet historique non réalisé ; distinguer projet dormant et annulation attestée.
- [ ] Sistema de Teleféricos e Parque Aventura do Curral das Freiras — Madère — identité applicative à résoudre — [CF](#sources-de-découverte). Projet soumis à procédures environnementales ; vérifier décision et chantier, sans convertir les rendus en offre ouverte.

- [ ] Lusitanix — Parque de Neve — Bustelo, Ribeira de Pena — identité applicative à résoudre — [LX](#sources-de-découverte). Projet distinct de Pena Aventura ; marché de construction publié en décembre 2021, chantier documenté par SPRAL en 2024 et investissement encore présenté comme projet en juin 2026. Vérifier le statut et l’ouverture actuels ; ne pas convertir les anciennes échéances 2019/2024 en dates d’ouverture réelles.

## 11. Attractions autonomes à intégrer ou à qualifier

- [ ] DreamFly Porto — Maia — identité applicative à résoudre — [DP](#sources-de-découverte). Candidat StandaloneAttraction : installation de simulation de chute libre exploitée seule ; confirmer le contrat et les doublons cachés.
- [ ] DreamFly Lisboa — Région de Lisboa — identité applicative à résoudre — [DL](#sources-de-découverte). Candidat StandaloneAttraction ; adresse et état courant à confirmer auprès de l’exploitant.
- [ ] World of Discoveries — Porto — identité applicative à résoudre — [TP](#sources-de-découverte). Arbitrer le modèle : parcours scénique autonome avec espaces muséaux ou véritable parc structuré ; aucun faux parc mono-attraction.

## Pistes de découverte à qualifier sans création automatique

- AquaPark do Tâmega — Ribeira de Pena : projet de plage fluviale et d’ancoradouro présenté en juin 2026 dans la même visite que Pena Aventura et Lusitanix. Aucun équipement de parc d’attractions établi dans cette annonce ; qualification à poursuivre, sans le confondre avec le Parque Aquático de Amarante. Source [Canal N, 18 juin 2026](https://www.canaln.tv/presidente-do-turismo-do-porto-e-norte-visita-ribeira-de-pena-e-aponta-o-alto-tamega-a-novos-voos-no-turismo/).

- Limitezero — Sanlúcar de Guadiana / Alcoutim : installation transfrontalière fixe ; le départ, la billetterie et l’exploitation sont en Espagne. Examiner une seule identité autonome avec son contexte portugais, sans créer une copie PT et une copie ES. Source LZ.
- Alpine Coaster et Alpine Coaster Kids de Pena Aventura : pages officielles et plan 2026 rapprochés. « Kids » désigne le billet enfant accompagné sur la même installation ; une seule attraction interne au parc, sans doublon ni fiche autonome. Source PA.
- Petite montagne russe de Porta do Mezio : existence spécialisée confirmée ; ne pas l’appeler alpine coaster ni convertir automatiquement tout le site en attraction isolée. Source PM.
- Projet d’alpine coaster de São João da Mata : installation annoncée au sein du futur parc ; conserver le statut de projet et le rattachement correct. Source SJ.
- Feijão Verde Alverca, Aveiro et Santarém Natura : présents dans une ancienne liste de l’exploitant, absents de la liste actuelle consultée. Rechercher une fermeture, un déménagement ou un changement de nom avant de les ajouter comme sites actifs ou historiques. Source FO.
- Aquapark Fratelli, Water Park Gerês et autres parcs flottants saisonniers : vérifier une exploitation stable sur un site identifié, le modèle pertinent et les équipements réellement nommés ; ne pas créer de StandaloneAttraction pour un parcours démontable. Sources PU et PN.
- Aquapark / Complexo de Piscinas Foz do Cávado, Esposende ; Bioparque de Carvalhais, São Pedro do Sul ; Parque Aventura do Pilar da Bretanha, São Miguel ; ancien Madeira Adventure Park : pistes à recouper avec exploitants et sources locales avant affectation à une section active.
- Nouvelle Feira Popular de Lisboa / Carnide-Pontinha : séparer le projet des anciens sites Palhavã et Entrecampos ; vérifier la situation réelle du projet à la date de traitement et ne pas réutiliser l’ancien statut Operating.
- Discovery Park, Ota : projet historique distinct de Lusolândia cité dans un dossier d’évaluation environnementale ; confirmer identité, portage et abandon avant d’en faire une fiche autonome.

## Exclusions et rapprochements conservés

- Les dix fiches PT déjà NotRelevant sont des noms de forains ou exploitants : Diversões Alverca (`cac23c30-7ab6-4da6-87fe-0a2da7943bfa`), Diversões Diverlike (`ca5946b2-259a-4d85-bf56-605df1893f77`), Diversões Galvão (`17fbd83e-dd79-43c6-8b72-48be300f99c9`), Diversões Lusitana (`ab0800b3-bed3-49ba-8164-a9c82e0ee9ee`), Jorge da Silva Simoes (`908757f8-ae7d-468b-902c-650711f21169`), Jose Pedro Almeida (`b8bac674-aa11-4696-9b3a-b899bb6c2f98`), Marsan (`369353f5-b08f-4bf3-938f-2450a7c84839`), Mendoca (`8fdea06c-badc-4df9-b145-dc28f05a98c0`), Tiago Medley (`58e5c2ff-c997-4fad-816e-1a321ce1c4f9`) et Unknown Portuguese Showman (`4b0a974b-97c3-40b4-8871-d25d2ac10a2e`). Elles ne sont ni supprimées ni enrichies artificiellement ; une preuve d’un parc fixe distinct rouvrirait la décision.
- Aquashow Outdoor, Indoor et hôtel : périmètre déjà consolidé dans Aquashow Family Park ; aucun second parc créé par défaut.
- Bracalândia de Penafiel appartient à l’histoire de Magikland ; l’ancien site de Braga conserve son intérêt historique propre.
- Parque Biológico do Mezio : rapprochement obligatoire avec Porta do Mezio avant de créer un parc animalier doublon.
- Les quatre implantations 3D Fun Art Museum, Brickopolis, la patinoire du Palácio do Gelo, Skiparque et Parque dos Índios ne sont pas automatiquement transformés en parcs parce que l’annuaire touristique les classe dans une rubrique large. Rechercher une véritable offre de parc ou une attraction fixe pertinente ; une exposition, une installation sportive ou un simple jardin ne suffit pas.
- Les foires itinérantes, roues temporaires de Noël, piscines de natation seules, réserves naturelles non aménagées et excursions de prestataires sans parc fixe ne reçoivent pas de faux parc parent.

## Ordre de traitement

1. Auditer Aquashow déjà public sans le masquer et conserver ses contenus fiables.
2. Compléter les grands parcs privés existants et résoudre les modèles des attractions autonomes candidates.
3. Créer les principaux parcs absents, avec recherche individuelle des doublons avant chaque Apply.
4. Reprendre les fiches historiques existantes et les parcs disparus absents ; vérifier les anciens inventaires, relocalisations et dates de fermeture.
5. Traiter les parcs locaux et les projets documentés en adaptant le volume, jamais la rigueur, puis statuer explicitement sur les pistes restantes.

## Anomalies et opérations réalisées

- Accès cloud vérifié avec Status le 20 octobre 2026 ; tous les appels techniques effectués jusqu’à ce relevé sont des lectures séquentielles.
- Aucun parc ou média n’a encore été créé, modifié, publié ou annoncé sur Facebook pour ce lot au moment de la création du backlog.
- Les valeurs Operating des six fiches historiques existantes sont des données legacy à contrôler, pas un statut validé par ce document.
- Les archives et annuaires de découverte ne constituent ni une preuve de licence actuelle, ni une autorisation de réutiliser une photographie.
- La recherche des brouillons autonomes cachés doit être résolue par les surfaces officielles et le Preview ; aucune route d’administration improvisée n’est autorisée.

## Exceptions explicitement acceptées

Aucune. Le seuil reste strictement supérieur à 95 et l’audit sans bloqueur est obligatoire.

## Sources de découverte

- **TP** — [Turismo de Portugal — annuaire national des parcs, cinq pages consultées](https://www.visitportugal.com/pt-pt/taxonomy/term/400).
- **AQ** — [Turismo de Portugal — aquaparcs, continent et Madère](https://www.visitportugal.com/pt-pt/content/aquaparques-em-portugal-5).
- **RC** — [RCDB — parcs portugais actuels et disparus](https://rcdb.com/r.htm?ol=26320&ot=3).
- **DG** — [DGAV — établissements zoologiques licenciés, document daté du 28 avril 2021](https://www.dgav.pt/wp-content/uploads/2025/05/Lista-Nacional-de-Parques-Zoologicos-Licenciados.pdf).
- **MA** — [Visit Madeira — Parc thématique de Madère](https://visitmadeira.com/pt/onde-ir/madeira/costa-norte/santana/parque-tematico-da-madeira/).
- **MQ** — [Visit Madeira — Aquaparque da Madeira](https://visitmadeira.com/pt/onde-ir/madeira/costa-este/santa-cruz/aquaparque-da-madeira/).
- **AZ** — [Municipalité de Vila Franca do Campo — réouverture de l’aquaparque en 2022](https://www.cmvfc.pt/wp-content/uploads/2024/01/Revista_web_2022.pdf).
- **AT** — [Constructeur Treetop — parc permanent des Furnas, livré en 2023](https://treetop.pt/construcao-parque-aventura-ilha-sao-miguel-azores/).
- **PZ** — [Municipalité de Povoação — parc zoologique](https://www.cm-povoacao.pt/index.php/juntas-de-freguesia/freguesia-da-povoacao).
- **FA** — [Parque Aquático de Fafe — site officiel](https://parqueaquaticofafe.pt/).
- **VA** — [Réseau national des stations nautiques — Vaga Splash](https://www.nauticalportugal.com/pt/partners/vaga-splash-water-park).
- **SA** — [Viver Santarém — complexe aquatique municipal](https://viversantarem.pt/complexo-aquatico-de-santarem/).
- **SC** — [Tempo Livre — parc aquatique Scorpio](https://tempolivre.pt/tag/parque-aquatico-guimaraes/).
- **PU** — [Pumpkin — découverte des aquaparcs portugais en 2026](https://pumpkin.pt/familia/viagens-criancas/sitios-a-visitar-familias/parques-aquaticos-portugal/).
- **PN** — [Pumpkin — découverte des aquaparcs du Nord](https://pumpkin.pt/familia/viagens-criancas/sitios-a-visitar-familias/parque-aquaticos-no-norte-de-portugal/).
- **FV** — [Feijão Verde — liste actuelle des implantations](https://feijao-verde.com/fun-parks).
- **FO** — [Feijão Verde — ancienne liste, utilisée pour rechercher les implantations disparues](https://www.feijao-verde.com/en/alverca/).
- **DI** — [Groupe Diver — ses trois implantations](https://diver.com.pt/en/the-group/).
- **PI** — [Picoto Park — site officiel](https://picotopark.pt/empresas/).
- **RA** — [Rates Park — programme officiel des installations](https://www.ratespark.pt/wp-content/uploads/2023/03/ATLs.pdf).
- **ZU** — [Azurara — inventaire officiel des activités](https://azurara-parque-aventura.com/atividades).
- **AA** — [Adrenaline Adventures Madeira — parc fixe de Porto Moniz, tyroliennes et balançoire géante](https://adrenalineadventures.pt/pt).
- **AL** — [Castro Marim — reconversion de l’ancien Aqualine](https://cm-castromarim.pt/site/noticia/antigo-parque-aquatico-abandonado-transformado-em-tres-pequenas-unidades-de-turismo-rural-de).
- **ON** — [Ondaparque — site mémoriel, à recouper avec archives et presse](https://ondaparque.wixsite.com/ondaparque).
- **RE** — [RTP/Lusa — fermeture du parc du Restelo et projet de reconversion](https://www.rtp.pt/noticias/pais/antigo-aquaparque-de-lisboa-abre-como-parque-infantil-no-inicio-de-2019_n1075871).
- **BE** — [Beja — PDM, ancien parc aquatique de Nossa Senhora das Neves](https://geoportal.cm-beja.pt/MuniSIG/Documentos/IGT/PDM_Aviso_22168_2023.pdf).
- **TE** — [Teimoso — mémoire locale et chronologie à recouper](https://opalhetasnafoz.blogspot.com/2025/02/teimoso-restaurante-ja-nao-existe-mas.html).
- **DF** — [Inventaire secondaire des parcs disparus — filet de découverte](https://en.wikipedia.org/wiki/List_of_defunct_amusement_parks).
- **LU** — [Lisbonne — source contemporaine de Luna Parque, juillet 1933](https://hemerotecadigital.lisboa.pt/fileadmin/user_upload/magalone/m_ilustracao_ano8_n014_16jul1933/pdf/full.pdf).
- **VM** — [AICEP — annonce du projet Viva Mundo](https://www.portugalglobal.pt/pt/noticias/2026/junho/viva-mundo-traz-450-milhoes-de-investimento-a-santarem/).
- **SJ** — [Municipalité de Castanheira de Pera — annonce de São João da Mata](https://www.cm-castanheiradepera.pt/PT/noticias/1504/pic/noticias/classes/support/images/seta_on.png).
- **LS** — [Azambuja — dossier officiel Lusolândia](https://www.cm-azambuja.pt/images/Urbanismo/Planeamento-Urbano/PP_Parque_Tematico-Lusolandia.pdf).
- **CF** — [Madère — dossier du système de téléphériques et parc du Curral das Freiras](https://static-storage.dnoticias.pt/www-assets.dnoticias.pt/documents/Estudo_de_Impacte_Ambiental.pdf).
- **DP** — [DreamFly Porto — billetterie officielle du simulateur](https://bookings-porto.dreamfly.eu/store_1).
- **DL** — [DreamFly Lisboa — site officiel](https://media-lisbon.dreamfly.eu/).
- **LZ** — [Limitezero — départ espagnol et arrivée portugaise](https://www.limitezero.com/pt/the-zip-line/).
- **PA** — [Pena Aventura — Alpine Coaster, installation interne au parc](https://penaaventura.com.pt/atividades.php?id=alpine_coaster).
- **PM** — [Porta do Mezio — parc et petite montagne russe](https://rcdb.com/22099.htm).
- **LX** — [Diário da República — marché Lusitanix, 24 décembre 2021](https://files.diariodarepublica.pt/cp_hora/2021/12/248/414840994.pdf) ; [Canal N — visite touristique et projet toujours annoncé, 18 juin 2026](https://www.canaln.tv/presidente-do-turismo-do-porto-e-norte-visita-ribeira-de-pena-e-aponta-o-alto-tamega-a-novos-voos-no-turismo/).
- **BC** — [Município de Boticas — camp jeunesse au BNB et au parc d’aventure de Carvalhelhos, juillet 2022](https://www.cm-boticas.pt/noticias/default.php?id=AW) ; [Município de Boticas — équipements touristiques et parc d’aventure](https://www.cm-boticas.pt/noticias/default.php?id=S5) ; [Diário da República — construction du parc, marché de 2007](https://files.diariodarepublica.pt/gratuitos/2s/2007/08/2S154A0000S00.pdf) ; [Município de Boticas — résiliation de la concession, séance du 3 juin 2015](https://www.cm-boticas.pt/docs/1103jun2015.pdf).
