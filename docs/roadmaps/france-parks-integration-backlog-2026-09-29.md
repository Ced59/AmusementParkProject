# Backlog d'intégration des parcs français — 2026-09-29

## Objectif

Cette liste versionnée fixe le périmètre de l'intégration nationale demandée le 29 septembre 2026. Elle couvre les parcs d'attractions, parcs à thème, parcs aquatiques structurés, parcs familiaux à installations fixes et parcs hybrides comportant une offre de loisirs nommable. Elle inclut aussi les anciens parcs dont une page historique apporte une valeur éditoriale réelle.

Chaque ligne active est traitée avec le workflow `PARK_DATA_EDITOR`, les étapes 0 à 9 et un audit final individuel. Une ligne ne peut être retirée qu'après les deux résultats suivants :

1. la fiche et ses contenus retenus sont réellement publics sur le site, le score post-publication est strictement supérieur à 95, aucun bloqueur ne subsiste et la page publique a été contrôlée anonymement ;
2. l'annonce Facebook officielle de cette même fiche est au statut `Published`, sans recréer une publication existante.

Une fusion de doublons ou un classement `NotRelevant` ne compte pas comme publication d'un parc et ne déclenche pas d'annonce Facebook artificielle. Une ligne d'attraction autonome suit en revanche sa propre fiche publique : elle ne peut être retirée qu'après publication et contrôle anonyme de la `StandaloneAttraction`, annonce Facebook `Published`, puis suppression effective de l'ancien parkItem et de l'ancien parc artificiel lorsqu'ils existent. Le simple masquage ou classement `NotRelevant` du legacy ne suffit pas.

La demande du 29 septembre 2026 autorise explicitement la publication Facebook de chaque fiche terminée. Cette autorisation sociale est propre à ce lot français et ne doit pas être déduite d'une future demande de complétude ordinaire.

## Sources et photographie initiale

- Recherche séquentielle `PARK_DATA_EDITOR` du 29 septembre 2026 : 268 enregistrements avec `countryCode: FR`, dont 30 visibles, 137 invisibles à revoir ou traiter et 101 invisibles déjà classés `NotRelevant`.
- Calcul individuel de complétude des 30 fiches visibles : 13 fiches à reprendre et 17 fiches déjà strictement au-dessus de 95 sans bloqueur.
- [Membres du SNELAC](https://www.snelac.com/adherent), source professionnelle courante utilisée pour recouper les exploitants et les sites vivants.
- [Liste des parcs de loisirs en France](https://fr.wikipedia.org/wiki/Liste_de_parcs_de_loisirs_en_France), inventaire secondaire non exhaustif utilisé uniquement comme filet de découverte.
- [Annuaire Parcs.fr](https://parcs.fr/parcs) et [guide Parcs France](https://www.parcs-france.com/carte/), recoupements spécialisés pour les parcs d'attractions structurés.
- [RCDB — France](https://rcdb.com/location.htm?id=26056), recoupement spécialisé pour les parcs à montagnes russes et les sites disparus.

Le statut « en activité » ci-dessous est une hypothèse de tri, pas une donnée à publier sans vérification. L'étape 0 de chaque parc doit confirmer l'identité, l'adresse, l'exploitant et le cycle de vie auprès de sources actuelles ; Marineland d'Antibes et toute autre fermeture récente doivent notamment être basculés vers le traitement historique approprié.

## Compteurs actifs

| Catégorie | Nombre initial | Restant |
| --- | ---: | ---: |
| Fiches déjà publiques à compléter | 13 | 13 |
| Fiches privées existantes à intégrer | 80 | 80 |
| Parcs en activité absents de la photographie FR | 60 | 60 |
| Fiches historiques privées existantes | 10 | 10 |
| Parcs historiques absents de la photographie FR | 20 | 20 |
| Attractions autonomes à migrer ou intégrer | 42 | 38 |
| **Total** | **225** | **221** |

## 1. Fiches publiques à compléter

- [ ] Loisiparc — `eb98bd7b-a068-41d5-b196-2aa301d0feed` — score 93 — bloqueur `public-text.forbidden-editorial-language`
- [ ] Parc Bagatelle — `34fe89e3-3d5f-44db-9b98-8106e065929a` — score 94 — bloqueur `public-text.forbidden-editorial-language`
- [ ] Aqualud — `14fe1db9-98eb-4301-86ff-21583bf31edf` — score 95 — fermé définitivement
- [ ] Aquascope — `1f6dc5db-d598-46f3-9ef9-6855e3e4fbeb` — score 95 — bloqueur `public-text.forbidden-editorial-language`
- [ ] Cobac Parc — `a2db3ed7-8f17-47dd-808f-4340936c82b7` — score 95
- [ ] Dennlys Parc — `8023eb10-e761-4c67-8765-f079f3d66cd8` — score 95 — bloqueur `public-text.formulaic-content`
- [ ] Festyland — `4fc0aa20-b0d7-47cc-a403-81b0d6e10f04` — score 95
- [ ] Futuroscope — `6407a639-14a5-49d1-9a21-2336d33161f5` — score 95 — bloqueur `public-text.forbidden-editorial-language`
- [ ] Île de loisirs d'Étampes — `3b9abd45-167e-4b02-aeec-8d2904e58f0a` — score 95 — bloqueur `public-text.forbidden-editorial-language`
- [ ] La Mer de Sable — `01a421d6-35b8-4227-ae30-78bab25ca7e6` — score 95 — bloqueur `public-text.forbidden-editorial-language`
- [ ] Magic Park Land — `a6123932-ff0b-41a8-9812-71ee96c4c67a` — score 95 — bloqueur `public-text.forbidden-editorial-language`
- [ ] Parc Astérix — `efebc041-9288-40a6-9094-c701ac2af323` — score 95 — bloqueur `public-text.formulaic-content`
- [ ] Parc Saint Paul — `6190c433-c7b4-4914-af6f-6c2646af08c5` — score 95 — bloqueur `public-text.forbidden-editorial-language`

## 2. Fiches privées existantes à intégrer

- [ ] Amigoland — `666740e4-c561-44b5-ac45-a4febd5b7f19`
- [ ] Bid'A Parc — `bb1605f3-58ca-45cd-bbca-6fdc811bee4c`
- [ ] Breizh Land Parc — `fd97efd8-9ebb-4a76-94cb-9cfa95100314`
- [ ] Cap Découverte — `8a131482-e133-4535-aca2-21563c282f90`
- [ ] Cigoland — `4f246cec-7a5a-4eb7-a98c-e32f9d5fa5b8`
- [ ] Coco Park — `9c031826-5afc-4035-ba58-f1ac3e737f0e`
- [ ] Corbi Park — `a11082bc-a3a9-4ada-a235-2f76b0de130d`
- [ ] Didi'Land — `4653aba1-869f-42d3-9254-57417d71d72b`
- [ ] Dinosaures Parc — `674fd8e9-0510-4623-ba65-9c541d2a17ab`
- [ ] Diverty Parc — `148ad11d-7b3f-49a9-b055-a57065798c6a`
- [ ] Fabrikus World — `baf98de1-33de-44b5-8a03-c5c1b76d24ee`
- [ ] Family Park Saint-Martin-le-Beau — `2f436a73-8a36-4adc-9723-f14d569ed4f8` — identité et implantation actuelle à réconcilier
- [ ] Fééryland — `711877e8-6309-4f30-8f4f-8cf24c99373d`
- [ ] Fermy Land — `1345965d-9d8e-43e1-a1d3-13f0b5a5bdd4`
- [ ] Fiesta Parc — `0baa84a5-ee92-4959-9f22-341cb97fe9a8`
- [ ] Fraispertuis City — `ea22716e-64d2-44ef-b068-6b26ba5cf9d7`
- [ ] Funny Land — `27798d4d-9e30-42c1-864d-cff0520d6511`
- [ ] Grinyland — `d5d59442-1fb4-4aca-a084-98029a6a9a58`
- [ ] Jacquou Parc — `d536babf-4e8a-4cb8-9584-abe4d6d0ae6f`
- [ ] Jardin d'Acclimatation — `ce97d925-9044-47dd-ad19-47467f663ce1`
- [ ] Jardin des Bêtes — `da438248-c5da-4c0a-8578-8d78ff2c64d2`
- [ ] Kangoo Park — `bac6532e-ed3e-40dd-9c8f-2a9b50f3d980`
- [ ] Kid Parc — `a48caf8e-871b-40cc-8b98-9faff4219e96`
- [ ] Kingoland — `900ceb7b-7bb8-40c0-9e4a-6340d019a6d8`
- [ ] Koaland — `a01011e8-b400-40c7-bbe2-5635d7dd24f7`
- [ ] L'Île aux Géants — `29a2498e-9b6c-4c55-a2ac-e521dc6e4586`
- [ ] L'Île aux Pirates, Capbreton — candidats `efac8649-a15b-4ed6-866c-2e926579d6de` et `5e9e6a61-3d74-49cd-9c3a-8ca107edd018` à réconcilier avant intégration
- [ ] La Coccinelle — `e1fa4656-8011-4908-a1bf-e7d581285a83`
- [ ] Le Pal — `1675e96c-6361-48ff-862b-43218167facd`
- [ ] LennyPark — `0d9fb07f-a134-40b5-94b1-6b449a684772`
- [ ] Lulu Parc — `29c89bcf-d733-42f1-982b-a8aa54aeeb09`
- [ ] Luna Park Carnon — `7321d4b5-b994-48c4-9d26-bd3fe6cc0354`
- [ ] Luna Park d'Argelès-sur-Mer — `63593dcd-fe87-4a9f-b6a9-91594eb69286`
- [ ] Luna Park de Palavas — `cdf42c3e-a34b-4c3c-bc7a-914cc9028903`
- [ ] Luna Park La Palmyre — `3f28a900-9e1d-4728-a987-ba5781ea0075`
- [ ] Luna Park Le Barcarès — `bf2c2fc3-91b4-4ff1-807a-159b68ec01a2`
- [ ] Lunapark Agde — `25747158-6f76-4a93-abc3-21cfad2a3385`
- [ ] Lunapark Fréjus — `dd83349f-e060-493c-a5d2-615c9b44967d`
- [ ] Magic World — `ae0dccf0-1079-4dd0-a736-7a98ccaaa487`
- [ ] Mignon's Park — `fd1a6d57-8e35-4086-b00e-a9de15f34b6b`
- [ ] Nigloland — `6b49033e-353e-4dab-af4a-6da12e272937`
- [ ] Ô Parc — `6644b892-d03c-461e-855d-a61cadba76f4`
- [ ] O'Fun Park — `81f86001-3485-418e-9266-7977dff7f56a`
- [ ] O'Gliss Park — `6f17412f-5a84-4449-9e00-75927717b8bf`
- [ ] OK Corral — `6ba29848-f17a-4982-b583-4f0dbba2e201`
- [ ] Papéa Parc — `5c22b408-5e7c-4f3b-a55f-ccf4bbe16cd2`
- [ ] Paradis Land — `0d9d2926-ec5e-4ba8-b992-21bc5bcab91f`
- [ ] Parc Ange Michel — `6c673827-ae94-450d-b164-73ffdf519491`
- [ ] Parc Aventureland — `5b0ef928-66ac-4d1b-b426-8c631a5d5b75`
- [ ] Parc Babyland — `99f3ddbb-cb30-4a17-9814-c227706cc8c9`
- [ ] Parc Bellevue — `995d713b-a338-46f0-81de-b0e23656a2ca`
- [ ] Parc d'Attractions Marseillan-Plage — `be33ee68-0db5-4342-8f52-705b61752fc7`
- [ ] Parc d'attractions Odet Loisirs — `3274297f-e3c1-417a-a4fd-9408943c9dcd`
- [ ] Parc de la Mignardière — `d431c493-5727-400c-9693-d22e1b99ae30` — cycle de vie à vérifier
- [ ] Parc de la Vallée — `f24b9993-8768-4e36-b3cb-544401210007`
- [ ] Parc de Loisirs de la Demi Lune — `0bfbbbf6-cc60-4a37-8d11-d7265299fefe`
- [ ] Parc des Combes — `271602e3-28bb-42cc-966d-aca0e04269aa`
- [ ] Parc des Dunes — `e3c53897-49e5-4036-a165-b0e2ef9ac448`
- [ ] Parc des Naudières — `f7f8cba8-62b4-4a91-8732-70a1cbc02b42`
- [ ] Parc du Bocasse — `79b3c038-bff1-4170-b51d-4c7ed76b1a2d`
- [ ] Parc du Petit Prince — `b1073fb7-1fa7-4d5b-9e3b-29412c12eb1f`
- [ ] Parc Fenestre — `4af8f8f8-2be7-4438-a02f-0054b36cb25e`
- [ ] Parc Spirou Provence — `fa6698dd-950b-4c78-b351-cd7deb501967`
- [ ] Pirat' Parc — `fba6fd0b-56c2-4b46-b321-2f901bb363a7`
- [ ] Piratland — `a2396980-0591-4ed6-8af2-ea683ef69503`
- [ ] Piratland Kids — `ce238026-7156-4d70-99c1-2929b15ce6d9`
- [ ] Pokeyland — `dc876288-7c8b-4684-8150-993908cdcf7a`
- [ ] Royaume des Enfants Draveil — `280c9edb-9ca3-402f-bca7-59bec53da93b`
- [ ] Royaume des Enfants Jablines — `72ac8f2e-e8c4-4bae-ad6f-c949f6b44f6a`
- [ ] Royaume des Enfants Neuville-sur-Oise — `df4fbc60-2eeb-47be-b287-90e994881b5a`
- [ ] Sam Parc — `622c4684-ea01-49d5-a4c5-82f80f44509c`
- [ ] Touroparc — `7e00d9c1-1e8b-4593-8be3-2c876c71ad1b`
- [ ] Ttiki Leku — `cd98d817-a9d5-4d8c-8644-e85721616193`
- [ ] Vulcania — `fb62b6ae-6802-4a92-9d54-6315bb15d1da`
- [ ] Walygator Grand Est — `e3fd2dee-462d-4649-bd34-8e2d71d1d943`
- [ ] Walygator Sud-Ouest — `333a75a7-a872-4e48-8ea2-7eb94ddfa3c1`
- [ ] Winnoland — `31f07250-ec46-4660-84a1-62e7e6540b05`
- [ ] Yaka-jouer — `51bd6d52-cd44-447a-b779-58ef47a55d04`
- [ ] Youpiland — `578f9edc-e057-46d4-973c-0da3dc831364`
- [ ] Zygo Park — `b2dca918-9747-4623-a0c0-38e89634aaff`

## 3. Parcs en activité absents de la photographie FR

- [ ] Acting Loisirs — Tillières-sur-Avre
- [ ] Aquaboulevard — Paris
- [ ] Aquafly — Chaillac
- [ ] Aquafly — Saulxures-sur-Moselotte
- [ ] Aqualand Agen — Roquefort
- [ ] Aqualand Bassin d'Arcachon — Gujan-Mestras
- [ ] Aqualand Cap d'Agde — Agde
- [ ] Aqualand Fréjus — Fréjus
- [ ] Aqualand Port Leucate — Port Leucate
- [ ] Aqualand Saint-Cyprien — Saint-Cyprien
- [ ] Aqualand Sainte-Maxime — Sainte-Maxime
- [ ] Aqualand St-Cyr-sur-Mer — Saint-Cyr-sur-Mer
- [ ] Aquaparc Isis — Dole
- [ ] Aquaspace — Beauvais
- [ ] Ardèche Miniatures — Soyons
- [ ] Atlantic Park — Seignosse
- [ ] Bal Parc — Tournehem-sur-la-Hem
- [ ] Cité de l'espace — Toulouse
- [ ] Dinopedia Parc — Champclauson
- [ ] Diverti'Parc — Toulon-sur-Arroux
- [ ] France Miniature — Élancourt
- [ ] La Cité de la Mer — Cherbourg-en-Cotentin
- [ ] Labyrinthe de Beaugency — Beaugency
- [ ] Le Bois des Lutins — Peypin
- [ ] Le Bois des Lutins — Villeneuve-Loubet
- [ ] Le Château des Énigmes — Pons
- [ ] Le Château des Énigmes — Val de Loire
- [ ] Le P'tit Délire — Ploemel
- [ ] Le Petit Paris — Vaïssac
- [ ] Le Village des Fous — Villeneuve-Loubet
- [ ] Le Village enchanté — Bellefontaine
- [ ] Le Village Gaulois, Parc du Radôme — Pleumeur-Bodou
- [ ] Les Poussins, Parc de la Citadelle — Lille
- [ ] Ludina — Mirande
- [ ] Ludolac — Vesoul
- [ ] Marineland d'Antibes — Antibes — fermeture récente à intégrer historiquement
- [ ] Micropolis, la cité des insectes — Saint-Léons
- [ ] Movinpark — Cernay
- [ ] Mysterra — Montendre
- [ ] Paléopolis — Gannat
- [ ] Parc AkOatys — L'Étang-Salé
- [ ] Parc de Gondrin — Gondrin
- [ ] Parc de la Préhistoire — Tarascon-sur-Ariège
- [ ] Parc de l'Auxois — Arnay-sous-Vitteaux
- [ ] Parc de Pierre Brune — Mervent
- [ ] Parc de Samara — La Chaussée-Tirancourt
- [ ] Parc Évasion — Curley
- [ ] Parc les Campaines — Accolans
- [ ] Parc Mini-Châteaux — Amboise
- [ ] Parc Miniature Alsace Lorraine — Plombières-les-Bains
- [ ] Parc Mirabel — Riom
- [ ] Plaine Oxygène — Le Mesnil-Amelot
- [ ] Puy du Fou — Les Épesses
- [ ] Rocher Mistral — La Barben
- [ ] Terra Botanica — Angers
- [ ] TerrAltitude — Fumay
- [ ] Tipipark — Bois-de-Haye
- [ ] Wave Island Provence — Monteux
- [ ] Wow Park — Urrugne
- [ ] Zig Zag Parc — Val-de-Vesle

## 4. Fiches historiques privées existantes

- [ ] Fami P.A.R.C — `bafa7e08-9858-472d-8357-9c84aa7479b7`
- [ ] Florida Parc — `b8ab1e68-5fe5-4bb9-8986-daf2fd3fe1e5`
- [ ] Holly Park — `1c5c0675-8d86-459c-9c7c-b2bc79c1dad4`
- [ ] Luna Park Paris — `77106934-0f4c-4eb3-b075-4097ab6078cd`
- [ ] Magic City Paris — `6cc196ad-27bc-444e-84e8-fb97c1baa9f0`
- [ ] Parc Avenue — `f019e441-aecf-487b-93db-2b0246dce185`
- [ ] Parc de la Toison d'Or — `66653da2-93bb-499f-95df-15e240673e58`
- [ ] Parc de Lillom — `8167f80c-db82-49d8-91a6-bf5a1f6a6f51`
- [ ] Parc de Lomme — `a2df3ac6-4627-4dc9-bee2-7fa2e2d96cf5`
- [ ] Ty'Bamboo — `6919c77b-1865-444d-8c5c-964b255d9c27`

## 5. Parcs historiques absents de la photographie FR

- [ ] American Park — Marseille — 1910-1914
- [ ] Aquagif — Gif-sur-Yvette — 1988-2002
- [ ] Aquagliss — Porticcio — 1986-2014
- [ ] Aqwaland Martinique — Le Carbet — 2004-2013
- [ ] Canyon Park — Épretot — 1976-2004
- [ ] La Vallée des Peaux-Rouges — Fleurines — 1966-1988
- [ ] Le Triton — Yerres — 1987-1989
- [ ] Océade Rouen — 1989-1991
- [ ] Océade Strasbourg — 1986-1996
- [ ] Parc de la Cabosse — Jurques — 1970-1977
- [ ] Parc de la Récréation — Monts/Sorigny — 1994-2018 — distinct du Family Park qui a repris le site
- [ ] Parc des Miniatures de Nice — Nice — 1990-1995
- [ ] Parc Eana — Gruchet-le-Valasse — 2008-2012
- [ ] Parc océanique Cousteau — Paris — 1989-1992
- [ ] Pira Park — Nice — fermeture à dater
- [ ] Planète magique — Paris — 1989-1991
- [ ] Playmobil FunPark Fresnes — Fresnes — 1999-2022
- [ ] Tivoli — Paris — 1730-1842
- [ ] Village de la Droséra - Forez Miniature — Jeansagnière — 1992-2009
- [ ] Zygofolis — Nice — 1987-1991

## 6. Attractions autonomes à migrer ou intégrer

Ces cibles suivent le workflow `standaloneAttractionGraph`. Le nom du domaine ou de la station sert à identifier l'installation fixe réelle ; il ne doit pas devenir le nom public d'un faux parc. L'ancien parkItem puis l'ancien parc artificiel sont supprimés, dans cet ordre, uniquement après la migration contrôlée, la publication et la vérification publique de la nouvelle entité `StandaloneAttraction`. Toute dépendance restante doit bloquer la suppression et être traitée explicitement ; un legacy seulement masqué ou `NotRelevant` ne clôt pas la ligne.

- [ ] Espace San Bernardo — parc legacy `53da66a3-c70b-4766-97b5-81b1da6edac2`
- [ ] Font-Romeu Pyrénées 2000 — parc legacy `9faa2fdb-d834-4927-a80d-87bc6ea6f7e9`
- [ ] Guzet — parc legacy `9ac7402e-b9d5-4e94-a6d8-630f472b4458`
- [ ] La Bresse Hohneck — parc legacy `2e8a0192-89e9-4af3-8428-1f871097c3bf`
- [ ] La Clusaz — parc legacy `4d61e26e-9384-4492-8195-b9c2fd6d15c9`
- [ ] La Colmiane — parc legacy `79c757cf-ef8b-4004-9cb9-3d144d0d0cb7`
- [ ] La Luge Alpine du Plan Incliné — parc legacy `60d4d7e6-741b-43f7-b0d9-966825cb3e7b`
- [ ] La Norma Ski Resort — parc legacy `616240a8-2aec-4afd-8289-cb7cb56b61bd`
- [ ] La Sambuy — parc legacy `3ed514ff-1f6f-412e-ac68-2327b3b6d36a`
- [ ] Le Dévoluy — parc legacy `e6b97b08-14d7-4958-b0a4-0ef308ae80fc`
- [ ] Le Lioran — parc legacy `9a42efe4-0cee-47f5-968a-ed35b44ff15c`
- [ ] Les 7 Laux — parc legacy `686b6dae-fc8a-4db2-a2d2-358ca79d5758`
- [ ] Les Carroz — parc legacy `eb45b3a2-eb9b-42c6-a42c-343b55617fa8`
- [ ] Les Gets — parc legacy `77d24707-66dd-48e1-9622-377ea6f66f1e`
- [ ] Les Menuires — parc legacy `4aaa3df2-0f6e-4f78-a0af-7445237da8d0`
- [ ] Les Orres — parc legacy `a578af64-de3d-4ad9-86f0-878277c634f4`
- [ ] Lou Bac Mountain — parc legacy `e1fae3e5-9c55-4fb6-a21f-5763667b3ca0`
- [ ] Luge Park Chamrousse — parc legacy `55cf1f08-0c5d-4c5b-bb87-e4665988e69c`
- [ ] Luges d'Été La Schlucht — parc legacy `1c59ce9e-55af-4353-931b-fee8c3699155`
- [ ] Lugik Park — parc legacy `d82e36ed-0fbc-4478-99a0-ac9eab0550a8`
- [ ] Markstein Grand-Ballon — parc legacy `1ce8e3b2-bb20-46a1-9f00-e4cea47c10f4`
- [ ] Montgenèvre — parc legacy `02fb8442-2107-4b53-ac23-dc4fe268a41e`
- [ ] Normandie Luge — parc legacy `c344f253-61a0-4b07-b892-3783fd3ecea8`
- [ ] Parc de loisirs du Hautacam — parc legacy `c0434ae9-1778-47f4-90b1-d514abc37985`
- [ ] Pra-Loup — parc legacy `7bbb10e0-d515-48db-8375-9124e3ed807b`
- [ ] Queyras Montagne — parc legacy `e0ea4fa2-5c75-4975-84d1-12dccbb04391`
- [ ] Régie des Saisies — parc legacy `36334076-f35b-47ee-ad9d-c084ef187a5c`
- [ ] Risoul Labellemontagne — parc legacy `a7ee0a1d-61d8-40e3-b1bf-c8aff2eedd94`
- [ ] Saint François Longchamp — parc legacy `4d43b576-ff37-45a4-9aed-0320af28d2af`
- [ ] Savoie Mont Blanc — parc legacy `08c7b84d-7f31-41f9-8c42-11304e3f4af0`
- [ ] Speed Luge Vercors — parc legacy `876951f7-55c4-48f1-8952-62f444ef1b87`
- [ ] Station de Métabief — parc legacy `9da09ef8-8cdc-469e-90ff-5c18e06ed32f`
- [ ] Station du Col de Rousset — parc legacy `54e5eeb7-cea4-4247-b7bc-3fd455c18cdc`
- [ ] Super Besse — parc legacy `743d2391-56c6-4ad1-be72-cf993ccf65ce`
- [ ] Syndicat Mixte des Monts Jura — parc legacy `97044a7e-cd42-48a2-a1a9-a7a7f12ad613`
- [ ] Tricky Track - Station du Lac Blanc — parc legacy `e3efaca9-683f-4938-915f-22c91f35b132`
- [ ] Val d'Allos — parc legacy `a059c4a3-9918-481c-8279-7cdee8dc18a7`
- [ ] Vars — parc legacy `4bc358c3-e01c-4841-91a7-fff608de7d50`

## Fiches publiques déjà conformes

Ces 17 fiches étaient strictement au-dessus de 95 et sans bloqueur lors du calcul individuel du 29 septembre 2026. Elles ne sont pas ajoutées à la file active, mais restent soumises à la maintenance normale des données.

- AcroJungle Outdoor — `1566972b-fbf9-461c-ac92-5df9d6c3358e` — score 96
- Animaparc Occitanie — `66316bb5-6401-4103-88ee-c5fe8dce4052` — score 100
- Antibes Land — `b0607555-4092-407e-9ee6-7d61423a8c73` — score 97
- Armoripark — `444d0ae3-ad1f-4e87-9304-139df277451d` — score 96
- Au Paradis des Enfants — `b3e11451-5350-4d53-b6c4-3d586a338683` — score 96
- Azur Park — `de091ae6-7a00-4446-ae69-e7bae1c60a67` — score 96
- Cita-Parc — `1d4ec4ab-5f19-4b1b-b4cf-8e240dce3304` — score 98
- Denain Évasion — `acdd7664-cf2b-42c6-b05a-7f67ee307aee` — score 96
- Disneyland Paris - Disney Adventure World — `6e367136-d75d-4366-9d6a-ef4afb303e10` — score 97
- Disneyland Paris - Disneyland Park — `834a7b68-1c6c-42b4-893c-2c5082dbc603` — score 98
- La Récré des 3 Curés — `9cebd5ae-dc2c-4c8c-a6a4-d2a2dda33d1c` — score 100
- Le Ch'ti Parc — `da07fda1-4b87-4142-85d2-23e4c0bbb585` — score 97
- Le Fleury — `4e0155db-9dbf-4755-a32f-0cc8950d61c4` — score 96
- Loos Parc — `80b9ed92-b303-4000-a484-66872ee6412f` — score 97
- Mirapolis — `327f4af5-10e5-4744-aa77-d3755ec7a9dc` — score 96
- Parc d’Olhain — `a3a2f6c5-3c81-4591-8ca0-246e677956e7` — score 97
- Walibi Rhône-Alpes — `79f7ad14-8b8e-437c-9512-7344341b5212` — score 96

## Nettoyage préalable : faux parcs à supprimer

Ces 99 cibles sont invisibles et déjà classées `NotRelevant`. Elles correspondent à des personnes, familles, exploitants forains, sociétés sans parc fixe ou libellés manifestement non publiables. Chaque suppression doit utiliser `PreviewDeletion` puis `ApplyDeletion`, retirer unitairement les dépendances indiquées par l'export et le Preview, supprimer le parc parent en toute dernière opération contrôlée, puis prouver l'absence du parc et de ses parkItems. `L’île aux Enfants` est volontairement exclu de ce lot tant que son identité n'est pas tranchée ; Alpe d'Huez a été reclassée dans la file des attractions autonomes après confirmation de sa luge sur rail fixe.

- [ ] Alexis Coquoz — `fd223f71-faf3-408a-a6a7-d9a9ca7f7892`
- [ ] Anthony Prunier — `28e3f84a-99c5-4201-b099-3e1ab317253f`
- [ ] Antony Baillet — `9a1dcafa-1dad-4b5f-a343-e3735a6e26d8`
- [ ] Arnaud Lec Perc — `3e17211c-10a7-413f-b366-da197e7aa156`
- [ ] Attractions Foraines Blitz (William Bouvier) — `0c39b85c-d30a-43e0-aeab-caf2684e6396`
- [ ] B. Hoffman — `fe569bb3-6363-4241-9464-ecd0ad3d8f5d`
- [ ] Bailer — `405bbf82-72ca-48b2-b4b2-dea0ca8ad143`
- [ ] Bonneau — `343f7bbf-f1a3-4dcf-8ccf-ce1726765630`
- [ ] Brad Laurier — `37684c55-1f69-4065-85c1-2c3d1546c3cf`
- [ ] Brigitte Morel — `ecc5915e-e4b3-426e-85b0-f156b5ba20ab`
- [ ] Cécile Penessot — `ac0fa053-191b-4442-a7ba-fcaab013d9c8`
- [ ] Champetier — `c7f5c035-9b4d-4990-a10e-216e6ae9ce58`
- [ ] Chauveau — `d4cfb052-073e-4601-bd4e-72f73b818713`
- [ ] Christian Chauvet — `474bec49-9384-4ebf-8a64-7ee20def2f20`
- [ ] Clark Kiener — `0496567f-3782-4dc2-b888-2265bcbfe6ec`
- [ ] Coppier — `5b8e79ed-6f34-43ce-9e3a-fdf70cd9d8e6`
- [ ] Davys Marrod — `4bdf3e26-dcc3-40cd-94cc-5ae9966b1ed2`
- [ ] Degousée — `d39c8834-aa3a-4e07-a835-dd397f82a43d`
- [ ] Degray — `cf56379a-5bcd-4455-8b8a-92230844da4a`
- [ ] Dell — `578098b0-7378-4432-a5b7-d67f74f575d4`
- [ ] Dominique Vanhaescbroeck — `068c5435-e99d-4ff9-9f9c-bdd1c7b8df75`
- [ ] Drouet — `fa6c0de8-1300-4253-a768-2e95c8023703`
- [ ] Dubois — `977b6204-e8a9-462a-b61b-26e2a3f904f7`
- [ ] Eddy Leraitre — `b1b1da5d-1eb3-47b2-906f-d8cf2919298a`
- [ ] Florian Guillemin — `3c855ad9-0d8c-442e-85b8-7b71085d5136`
- [ ] Franz Guevar — `ce75fa79-bf3b-4993-9901-0f4c5773619f`
- [ ] Gerald Debarre — `c2c7a33a-4d8f-470c-a9a7-5d75446929b7`
- [ ] Gino Paillet — `00e30ba4-fc53-4f83-8997-6a7473ab2dcc`
- [ ] Gino Ventrice — `0efbc0ba-9d72-4fd0-a590-dec54c5e83b2`
- [ ] Gregory Kopp — `da12047f-024f-40a3-a85d-ab852fa0b454`
- [ ] Guevar — `eea7c83c-8ac8-4edc-bb9a-dbaf478655af`
- [ ] Guigouret — `4523a4aa-5631-48b4-9080-817960667a9a`
- [ ] Gunther Starck — `64c152cd-a5ee-446e-a0ff-c324af4c70c2`
- [ ] Henrique Starck — `1ca55610-8244-490a-8f89-1806e3066319`
- [ ] Henry Vancraeyenest — `2f5499ee-e85b-4f40-8fe7-516ddb88c124`
- [ ] J. Lagnier — `62a3ecad-1320-4f4a-8f3f-f97a96936603`
- [ ] Jacky Lafosse & Joyce Montaletang — `e66df30f-4989-47ca-804b-334217f2cad4`
- [ ] Jacky Sorrel — `afdb71e3-fcd4-4267-9ee2-ac953a883734`
- [ ] Jacquier — `a6fb26cf-6c01-47a7-92c7-a59db62adfc0`
- [ ] James Montalétang — `b3665d66-e09d-42db-987e-ccfc3f7d0350`
- [ ] Jardon — `feef213b-b8bc-4177-8741-717f11d1dbb7`
- [ ] Jean-Charles Ferrari — `c41f4a25-72c5-4ac3-8369-d3d0d7ad2218`
- [ ] Jérémy Buttier — `a0a20db0-c8a9-4f4b-96c3-2a3f01306859`
- [ ] Joel Villette — `aecaaa26-db90-4c9e-908f-93596c8b848a`
- [ ] Jose Luis Santillan — `580cebef-e35a-4a4d-ada6-f27c5da2022b`
- [ ] Julien — `1f4393bb-e04e-4988-9a18-41af1f29664d`
- [ ] Karl Vancraeyenest — `bcd8e6f8-064f-4af9-b74f-0419a0e082a4`
- [ ] Kerwich — `724f2ca0-1e71-4de6-bf74-70c25c180807`
- [ ] Lapère & Bufkens — `7c91fede-99a1-4872-acb2-1d7275ea5ac6`
- [ ] Lapere & Roopers — `f4c4daee-2055-4bf2-af70-906391c1a32d`
- [ ] Laso — `ba630938-3542-43dd-9da5-de929ede77a7`
- [ ] Leny Beernaert — `155ff618-86df-4ecf-9adb-338897c04da8`
- [ ] Léo Charles Rannou — `f1610e4f-501f-4002-b258-2057b9ad010f`
- [ ] Lepine — `5e6b081b-b9a5-4e15-9d85-a505ac985c63`
- [ ] Lespinasse — `072bc7d4-727a-44ff-95c8-691720f18281`
- [ ] Llaurent Desert — `10502150-cdbc-45d2-8bca-fa32dd01b42e`
- [ ] Loulou/Horion — `23a08658-c1dd-452a-b3cf-35357e9fa583`
- [ ] Lucien Temperado — `b76816e0-d15a-4a5c-95f1-dd8bcb732eb1`
- [ ] M. Meric et fils — `d94a6b93-e487-4037-8320-b148debc1d68`
- [ ] Marc Sagnier — `913f0c25-7a60-4473-b480-63847c1f0ede`
- [ ] Mayer — `69743060-987d-4fd9-83f2-33fba213c6a2`
- [ ] Mazallon — `2b59ac4c-3fdf-4b40-baf9-114e8ac9d256`
- [ ] Melvin Fabulet — `6775a1f7-a8ce-4d76-857e-19cb52dfc793`
- [ ] Meyer — `3761ddc2-d4ab-4689-8bd3-a72fe2b5ba18`
- [ ] Michael Masson — `b3327c30-aeeb-448f-b9d4-0dc1b80743d5`
- [ ] Mutter — `b89eede7-68a5-4bb9-a6af-82adc40012a6`
- [ ] Nestor Le Castor — `d4e90ffa-8d1d-4d57-8e1d-1e1ae94047f1`
- [ ] Nino Aelters — `6b35dbf6-0cbe-4032-9f64-77e5efb892e1`
- [ ] Noah & Karl-Heinz Starck — `afe1a060-6f83-4147-bf1c-da59b7956292`
- [ ] Patrick Clouet — `cdbd53fe-44f6-4f1a-8113-43563e1e9955`
- [ ] Pauly — `fc2281dc-9e20-4ca5-8c2e-c608d0d89796`
- [ ] Peillex — `fc1db940-fc64-465f-8a35-78328b24cc64`
- [ ] Pfausser — `dc65198e-8a97-4b41-b4f3-88e36be9788b`
- [ ] Portigliati — `8e634a2f-13bd-46c4-9c60-8ee4bae4a852`
- [ ] Pouzet Groupe — `8ff7a167-615d-4aeb-b2d3-821ae34a201b`
- [ ] Proost — `0429bef4-cab7-4a2e-89ec-7dd5985bdb65`
- [ ] R. Bonnet — `8821ea13-0818-4f68-a809-88771c2c2f0e`
- [ ] Rassin — `63e38650-5de8-42e6-9c64-185f4f956c0a`
- [ ] Richard Lapère — `5ea4d8c2-46cb-4819-a988-29016076d473`
- [ ] Robert Baudrier — `b52d40ba-598f-4191-a4a9-2e1d63d788e5`
- [ ] Rudy Kiener — `d5bc9f6a-2bed-40cb-b265-10c8db40a5d5`
- [ ] Sacha & Clarisse Kopp — `d3199e26-757d-49a7-bd8c-76097f14d71e`
- [ ] Theophilos — `bc8f3cd0-bda5-4df5-b0ef-2938b0d43044`
- [ ] Thierry Gueglio — `21e5ae10-81af-44b5-a9aa-7d207fae5a31`
- [ ] Tixier — `39b27664-b5d2-4c1e-8b14-8c04f77f824a`
- [ ] Tony Vancraeyenest — `cb8d44f1-a38e-4118-a159-b5b51abd91c0`
- [ ] Unknown French Showman — `8e05ac0f-c8aa-4224-882f-354bc4fc7672`
- [ ] Vogler — `6d804395-cb53-42f2-ae44-f4f48cb0a352`
- [ ] Wesley Crouzier — `d843f48e-0226-4e8d-b113-2aa53dec8420`
- [ ] Wilfried Troisne — `8aeb88d9-f13e-4599-a37d-a6fbafdcfac0`
- [ ] William Bourez — `32341932-af06-4ce9-a3c9-fd1e5533faf9`
- [ ] William Mundwiller — `23c356ea-546a-476b-95db-d8ec11205360`
- [ ] William Ricordel — `82a82392-4a6c-4ec4-978b-8fd52529f890`
- [ ] Wilson Gribel — `1663307b-cd72-455b-91c6-6bc4e9a16546`
- [ ] Xavier Lapere — `cd0028d8-d6a9-4710-8482-6769a1d38ce5`
- [ ] Xavier Saguet — `cebbe729-1c0b-4e98-8e92-760e44ddb039`
- [ ] Yannick Guérin — `a2ff8ffa-07db-443b-9673-18e1cae4c6ce`
- [ ] Yohann Fougerat — `38a7ee3d-29a1-4156-92d6-1a1aedbcf4cf`
- [ ] Yohann Rassin — `6977c457-4e33-477e-9e2f-a49342854dae`

## Hors backlog de publication : anomalies à résoudre

### Identités ou pertinence à confirmer

- `1c83e45c-3335-4948-b15f-ce2805bc42e3` — Exposition Coloniale Internationale : événement historique temporaire, pas encore confirmé comme parc autonome pertinent.
- `5f6e3757-f7d0-423b-9c77-34f64dd4aeab` — Family Park Monts : probable ancien emplacement ou doublon de la lignée Family Park.
- `cd7332ea-45f6-4e07-a70e-44fd2d60b862` — Parc de l'Étang : identité trop générique à résoudre.
- `5c4ec94e-c4d8-4d38-bee9-9a97e7609dbf` — Parc de Moine : parc municipal à distinguer d'un parc de loisirs structuré.
- `d365dd74-a30f-4230-8f77-a89642d8cd2f` — Parc de Procé : parc urbain à distinguer d'un parc de loisirs structuré.

### Entrées déjà `NotRelevant`

Les 101 entrées françaises déjà classées `NotRelevant` ont été relues comme lot. Elles sont essentiellement des personnes physiques, familles ou exploitants forains et ne sont pas ajoutées au backlog. `L’île aux Enfants` reste la seule appellation de ce lot à réexaminer individuellement en cas de preuve d'un site fixe distinct.

## Ordre de traitement

1. migrer ou créer les 42 attractions autonomes avec `standaloneAttractionGraph`, sans conserver de faux parc parent ;
2. supprimer les 99 faux parcs certains et leurs dépendances après Preview contrôlée ;
3. reprendre les 13 fiches déjà publiques, par score croissant, pour supprimer rapidement les dettes et bloqueurs visibles ;
4. intégrer les fiches privées existantes, en donnant la priorité aux parcs majeurs et aux identités déjà bien établies ;
5. créer les parcs absents après recherche d'identité et de doublons ;
6. traiter les parcs fermés avec une exigence historique proportionnée ;
7. maintenir les autres anomalies hors publication tant que leur identité ou leur pertinence n'est pas résolue.

Les traitements API restent strictement séquentiels. Un contrôle global `operations/status` précède chaque export, Preview, Apply ou import de média ; tout `operation-busy` suspend le traitement jusqu'au délai recommandé.
