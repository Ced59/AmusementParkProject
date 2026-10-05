# Backlog d'intégration des parcs français — 2026-09-29

## Objectif

Cette liste versionnée fixe le périmètre de l'intégration nationale demandée le 29 septembre 2026. Elle couvre les parcs d'attractions, parcs à thème, parcs aquatiques structurés, parcs familiaux à installations fixes et parcs hybrides comportant une offre de loisirs nommable. Elle inclut aussi les anciens parcs dont une page historique apporte une valeur éditoriale réelle.

Chaque ligne active est traitée avec le workflow `PARK_DATA_EDITOR`, les étapes 0 à 9 et un audit final individuel. Une ligne ne peut être retirée qu'après les deux résultats suivants :

1. la fiche et ses contenus retenus sont réellement publics sur le site, le score post-publication est strictement supérieur à 95, aucun bloqueur ne subsiste et la page publique a été contrôlée anonymement ;
2. l'annonce Facebook officielle de cette même fiche est au statut `Published`, sans recréer une publication existante.

Une fusion de doublons ou un classement `NotRelevant` ne compte pas comme publication d'un parc et ne déclenche pas d'annonce Facebook artificielle. Une ligne d'attraction autonome suit en revanche sa propre fiche publique : elle ne peut être retirée qu'après publication et contrôle anonyme de la `StandaloneAttraction`, annonce Facebook `Published`, puis suppression effective de l'ancien parkItem et de l'ancien parc artificiel lorsqu'ils existent. Le simple masquage ou classement `NotRelevant` du legacy ne suffit pas.

La météo est alimentée par le batch automatique. Une prévision momentanément absente ne bloque ni la complétude, ni la publication, ni le retrait d'une ligne du backlog ; seul le bon fonctionnement de l'affichage lorsque le batch fournit des données fait partie du contrôle applicatif.

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
| Fiches déjà publiques à compléter | 13 | 0 |
| Fiches privées existantes à intégrer | 82 | 38 |
| Parcs en activité absents de la photographie FR | 60 | 60 |
| Fiches historiques privées existantes | 10 | 10 |
| Parcs historiques absents de la photographie FR | 20 | 20 |
| Attractions autonomes à migrer ou intégrer | 40 | 0 |
| **Total** | **225** | **128** |

## 1. Fiches publiques à compléter

Aucune fiche ne reste dans cette catégorie.

## 2. Fiches privées existantes à intégrer

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
- [ ] Parc de loisirs du Hautacam — `c0434ae9-1778-47f4-90b1-d514abc37985` — parc de loisirs multi-activités confirmé, à conserver comme parc
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
- `cd7332ea-45f6-4e07-a70e-44fd2d60b862` — Parc de l'Étang : identité trop générique à résoudre.
- `5c4ec94e-c4d8-4d38-bee9-9a97e7609dbf` — Parc de Moine : parc municipal à distinguer d'un parc de loisirs structuré.
- `d365dd74-a30f-4230-8f77-a89642d8cd2f` — Parc de Procé : parc urbain à distinguer d'un parc de loisirs structuré.

### Entrées déjà `NotRelevant`

Les 101 entrées françaises déjà classées `NotRelevant` ont été relues comme lot. Elles sont essentiellement des personnes physiques, familles ou exploitants forains et ne sont pas ajoutées au backlog. `L’île aux Enfants` reste la seule appellation de ce lot à réexaminer individuellement en cas de preuve d'un site fixe distinct.

### Suppressions contrôlées terminées

- Luna Park Carnon — la commune atteste la fermeture définitive et irréversible du Luna Park en 2021 puis documente en 2026 l'aménagement du parking qui occupe son ancien site. La fiche privée était un ancien regroupement artificiel de cinq montagnes russes Captain Coaster, dont trois déjà délocalisées, sans description, média, horaire, tarif ni contenu historique. Les cinq parkItems ont été supprimés individuellement après Preview, un export intermédiaire a confirmé un inventaire vide, puis le parc masqué a été classé `NotRelevant` et supprimé par une dernière opération contrôlée. Les recherches finales par nom et par identifiant renvoient toutes deux zéro résultat.

### Métadonnées globales hors portée du jeton parc

- Les descriptions internes des logos MACK Rides `01ec66b67e574368a3a406115958485a` et ZIERER `ad46c05063a244a5a04f3a996c985152` partagent une formulation technique. L’audit individuel de Parc Astérix n’en fait pas un bloqueur de publication, mais la correction du logo global ZIERER a été refusée par `park-data-editor.image-scope-denied`. Cette dette partagée reste donc explicitement suivie sans contourner le périmètre du jeton parc.

### Médias orphelins hors graphe

- Fraispertuis City — les imports privés `214b9e0769384beda31feef8e31657de` et `57885c3627d7405d8f466694773c80e4` ont été créés avec des clés de lot devenues obsolètes au lieu des identifiants persistés de Golden Driller et Sawmill. Ils n’appartiennent pas au graphe publié et ne sont ni courants ni publics. Leur suppression contrôlée a été refusée parce que le serveur ne peut plus les rattacher au parc cible ; les deux photographies ont été réimportées sur les IDs vérifiés. Les prochains imports doivent recouper chaque `OwnerId` avec l’export courant avant l’appel afin de ne pas reproduire cette anomalie.

### Lacunes éditoriales documentées

- O'Fun Park — la fiche publique atteint 97 avec 61 éléments validés, dont 43 attractions, quatre attractions Zamperla annoncées pour 2027, les six univers officiels et les principaux services du site. Les huit langues couvrent les descriptions du parc, des zones et de chaque élément ; 34 médias officiels sont publiés, avec le logo courant, quatre vues générales et 29 photographies d'éléments. Le plan officiel 2026, les 244 dates du calendrier, les tarifs 2026 corrigés à partir des montants officiels, les pass saison, les formules de groupe et le parking gratuit sont structurés. Sept jalons et un article développé retracent l'ouverture d'Indian Forest en 2002, le changement de nom, la nouvelle identité western et les ouvertures prévues en 2027. Les quatre rendus des nouveautés futures sont explicitement décrits comme des illustrations conceptuelles et restent marqués non courants. Les éléments sans photographie propre, les anciennes étapes sans image historique, les coordonnées individuelles et la date précise d'ouverture du parc restent volontairement absents faute de source fiable. La page publique répond en 200 ; la météo alimentée par batch n'a pas conditionné la complétude. L'annonce Facebook officielle existait déjà au statut `Published`, sans création de doublon : https://www.facebook.com/1285475681307050/posts/122121506505424431.
- Ô Parc — la fiche historique publique atteint 96 avec les 31 entrées du dernier plan officiel : 27 attractions, deux points de restauration, la boutique d'accueil et le poulailler. Les huit langues couvrent chaque description ; l'Aqua Zone et la Baby Zone regroupent les huit activités explicitement zonées, et onze médias officiels sont publiés, dont le logo, une vue générale et neuf photographies d'éléments. Le plan final 2026, cinq jalons du parc, trois jalons d'attractions et un article développé retracent l'ouverture de 2017, La Chenille, les nouveautés 2025 et la fermeture définitive du 31 août 2026. Les derniers horaires et tarifs sont conservés dans le récit historique sans créer de grille active pour un parc fermé. Aucun constructeur n'a été attribué sans source fiable, et les éléments sans photographie propre n'ont pas reçu de visuel générique. La page publique répond en 200 ; la météo alimentée par batch n'a pas conditionné la complétude. L'annonce Facebook officielle existait déjà au statut `Published`, sans création de doublon : https://www.facebook.com/1285475681307050/posts/122121487455424431.
- Normandie Luge — la fiche publique atteint 100 avec huit éléments validés : six activités, le restaurant-bar-glaces et le parking P2. Les huit langues couvrent chaque description ; neuf médias officiels sont publiés, dont le logo, une vue générale et une photographie pour sept éléments, le parking P2 restant sans visuel propre faute d'image officielle distincte. Le plan officiel 2025, les 160 dates d'ouverture 2026, l'accès libre au site, les deux parkings gratuits et les dix-huit offres d'activités ou de packs sont structurés. Quinze jalons et un article développé retracent l'ouverture de 2013 et les extensions durables du site ; les fondateurs, l'exploitant et Wiegand sont reliés et décrits. Les pages publiques de la fiche, des activités, des images, du plan, de la météo, des horaires et des tarifs répondent en 200 ; la prévision météo momentanément absente, alimentée par batch, n'a pas conditionné la complétude. L'annonce Facebook officielle est confirmée au statut `Published` : https://www.facebook.com/1285475681307050/posts/122121477363424431.
- Nigloland — la fiche publique atteint 100 avec 46 éléments documentés : 39 expériences actuelles, Supersonic annoncé pour 2027, les hôtels Hôtel des Pirates et Cabaïana, ainsi que quatre attractions historiques conservées privées avec le statut `Removed`. Les 42 éléments visibles possèdent une photographie propre et les huit langues couvrent chaque description ; le logo, une vue représentative et le plan officiel sont également publiés. Les 147 dates d'ouverture de la saison 2026, sept offres d'entrée, le pass saison et le parking gratuit sont structurés. Treize jalons et un article développé retracent l'histoire et les nouveautés durables du parc. Les huit routes publiques contrôlées — fiche, éléments, images, plan, zones, météo, horaires et tarifs — répondent en 200 ; la météo alimentée par batch n'a pas conditionné la complétude. Deux médias privés issus d'identifiants temporaires de Preview restent orphelins et non rattachés à la fiche ; leur suppression nécessite une autorisation distincte et ne bloque pas la publication. L'annonce Facebook officielle est confirmée au statut `Published`.
- Mignon's Park — la fiche publique atteint 97 avec huit éléments validés : cinq attractions actuelles, Mignon's Diner, le parking et Pomme conservée comme montagne russe retirée après ses saisons 2024 et 2025. Les huit langues couvrent chaque description ; deux médias sont publiés, avec le logo officiel et une vue générale, mais aucune photographie individuelle suffisamment attribuable n'a été trouvée pour les éléments. Les horaires 2026 sont structurés ; aucune grille de base actuelle n'étant publiée, aucun montant n'a été extrapolé, tandis que les forfaits illimités de 5, 10 et 15 € annoncés à Noël 2021 restent clairement présentés comme un repère historique. Quatre jalons et un article développé retracent l'ouverture d'avril 2021 par David et Olivia Langlais, la création d'OSK en 2023 et le passage de Pomme. Aucun plan officiel n'a été trouvé. La page météo existe mais attend encore son alimentation par le batch automatique, qui n'a pas conditionné la complétude. La fiche, les horaires et l'absence explicite de tarif courant ont été vérifiés publiquement ; l'annonce Facebook officielle existait déjà au statut `Published`, sans création de doublon.
- Magic World — la fiche publique atteint 96 avec 43 éléments validés : 37 attractions actuelles, quatre services ou lieux de restauration et deux attractions historiques classées `Removed`. Les huit langues couvrent chaque description ; 33 médias sont publiés, dont le logo, deux vues du parc et 30 photographies d’éléments. Les 99 soirées du 23 mai au 29 août 2026, de 20 h à 2 h, l’entrée libre, les fourchettes indicatives de 3–5 €, 5–7 € et 8–10 €, ainsi que le parking gratuit sont structurés et affichables toute l’année ; les dates de validité artificielles des tarifs ont été retirées après vérification publique. Sept jalons et un article développé retracent les débuts autour de 1970 et le renouvellement des attractions ; trois constructeurs sont reliés, dont Troisne pour Banzai. L’inventaire officiel annonce plus de 40 attractions, mais seules les 37 attractions actuelles nommées ont été créées afin de ne pas inventer les éléments non identifiés. Les conditions d’accès individuelles, l’exploitant juridique et certaines photographies restent absents faute de source fiable ; l’exploitant collectif Magic World Hyères est distingué des forains indépendants. La fiche, les horaires et les tarifs répondent publiquement ; la météo reste en attente du batch automatique et n’a pas conditionné la complétude. L’annonce Facebook officielle existait déjà au statut `Published`, sans création de doublon.
- Lunapark Fréjus — la fiche publique atteint 98 avec 41 éléments validés : 36 attractions actuelles, trois montagnes russes historiques classées `Removed`, la Brasserie du Luna Park et les toilettes. Les huit langues couvrent chaque description ; 33 médias propres sont publiés, dont le logo, une vue générale et 31 photographies d’éléments. Les sept fiches sans photographie individuelle attribuable sont Carrousel, Family Roller Coaster, Gonflable Bob l’éponge, Manège d’avion, Toilettes, Tokaido Express et Trampoline. Les 100 soirées du 22 mai au 29 août 2026, l’entrée libre et le parking gratuit sont structurés ; les prix unitaires restent omis, chaque métier gérant son tarif et ses moyens de paiement sans grille 2026 commune. Les tailles minimales officielles de Gravity et Shaker sont structurées ; aucune condition n’a été extrapolée pour les autres attractions. Six jalons et un article développé retracent l’ouverture de 2006 et l’évolution des montagnes russes. La page publique et son URL canonique répondent en 200 ; la météo alimentée par batch n’a pas conditionné la complétude. L’annonce Facebook officielle existait déjà au statut `Published`, sans création de doublon.
- La Coccinelle — la fiche publique atteint 97 avec les 23 éléments de l’inventaire officiel, les huit langues sur chaque description, 26 médias publiés dont le logo et 20 photographies d’éléments, le plan officiel 2026, 141 dates d’ouverture, sept offres d’entrée, un pass, le parking gratuit, sept jalons historiques et un article développé. La Table des Coccinelles, Le Coin des Douceurs et Les Cocc'Italiennes restent sans photographie individuelle suffisamment attribuable ; les autres éléments disposent d’un visuel propre. L’historique courant est servi publiquement par la timeline du parc. La météo alimentée par batch n’a pas conditionné la publication ni le retrait.
- L'Île aux Géants — la fiche publique atteint 98 avec 21 éléments validés : les 18 attractions du site officiel, la pataugeoire, O'resto d'Iros et les aires de pique-nique. Les huit langues couvrent chaque description ; 19 médias officiels sont publiés, dont le logo, deux vues générales et une image pour 16 attractions. Aucun plan visiteur officiel n'a été trouvé : l'image-calendrier 2026 n'a pas été détournée en carte. Manège enchanté, Petit train, Pataugeoire, O'resto d'Iros et les aires de pique-nique restent sans visuel propre faute de photographie officielle distincte. Les horaires et cinq tarifs d'entrée 2026 ainsi que le parking gratuit sont structurés ; trois jalons et un article développé documentent l'ouverture de 2013 et les deux montagnes russes. La météo alimentée par batch n'a pas conditionné la publication ni le retrait.
- Koaland — la fiche publique atteint 98 avec 19 éléments validés : 16 activités actuelles illustrées, Music Lab et lʼespace fitness conservés privés comme développements annoncés, ainsi que Chenille publiée comme attraction définitivement fermée et reliée à un jalon historique. Les huit langues couvrent chaque description ; 22 médias officiels sont publiés, dont le logo, cinq vues générales et une image pour chacune des 16 activités actuelles, avec le plan officiel 2026, les horaires sourcés, lʼentrée gratuite, quatre lots de jetons, la pêche aux canards, deux jalons et un article développé. Aucun visuel historique officiel réutilisable nʼa été trouvé pour Chenille. Les horaires dynamiques du site officiel étant contradictoires, la dernière communication sociale officielle fiable — mercredi, samedi et dimanche de 10 h à 19 h — a été structurée ; les tarifs sportifs et de restauration restent omis faute de grille publique complète. La météo alimentée par batch nʼa pas conditionné la publication ni le retrait.
- Kingoland — la fiche publique atteint 97 avec les 43 éléments de l’inventaire officiel, les huit langues sur chaque description, 49 médias propres publiés dont le logo et une image pour chacun des 43 éléments, le plan officiel 2026, 109 dates d’ouverture, six offres tarifaires, un pass, le parking gratuit, neuf jalons historiques et un article développé. Six aires ou expériences sans seuil officiel conservent volontairement leurs règles d’accès vides, et aucune géolocalisation individuelle ni zone thématique durable n’a été inventée. La source tarifaire 2026 reste la page officielle archivée sous `/test/`, la page de billetterie canonique annonçant déjà la saison 2027 sans nouvelle grille. La météo alimentée par batch n’a pas conditionné la publication ni le retrait.
- Jardin d'Acclimatation — la fiche publique atteint 100 avec les 62 éléments de l'inventaire officiel courant, cinq attractions historiques, les huit langues sur chaque description, 74 médias publiés dont le logo et une image pour chacun des 62 éléments actuels, le plan officiel, 92 dates d'ouverture, huit offres tarifaires, dix jalons de parc, cinq jalons d'attraction et un article développé. Les cinq attractions historiques restent sans photographie plutôt que de recevoir un visuel actuel ou insuffisamment attribuable. La page météo alimentée par batch n'a pas conditionné la publication ni le retrait.
- Jardin des Bêtes — la fiche publique atteint 96 avec 24 éléments actuels, les huit langues sur chaque description, 24 médias publiés dont le logo, le plan officiel 2026, 104 dates d'ouverture, les tarifs officiels 2026, quatre jalons historiques et un article développé. Vingt et un éléments disposent d'une photographie officielle attribuable ; La Paillote, Les Palanges et Le Kiosque restent sans vue propre, la page officielle de restauration n'exposant qu'une illustration générique du parc. Les tarifs 2026 restent visibles jusqu'à la fin de leur année de référence tandis que le calendrier gouverne seul les jours d'ouverture. La page météo répond correctement avec une prévision momentanément vide ; son alimentation par batch n'a pas conditionné la publication ni le retrait.
- Jacquou Parc — la fiche publiée atteint 98 avec 26 éléments actuels, les huit langues sur chaque description, les horaires et tarifs officiels 2026, huit jalons historiques, un article développé, le plan officiel 2025 et treize médias publiés dont le logo. Huit éléments possèdent une photographie individuelle clairement attribuable ; les autres conservent leur description sourcée sans recevoir de visuel générique. Les quatre vues générales documentent notamment l'espace aquatique et l'ambiance boisée. La saison 2026 étant achevée et aucun calendrier 2027 n'étant encore publié, la grille vérifiée reste conservée sans inventer de dates futures. La route météo répond correctement ; son contenu alimenté par batch n'a pas conditionné la publication ni le retrait.
- Grinyland — la fiche historique publiée atteint 97 avec les 30 entrées nommées du dernier plan officiel, sept zones, six jalons de parc, un jalon d'attraction, un article développé et sept médias publiés dont le logo officiel. Six visuels représentatifs ont été conservés après recherche ; les 26 éléments sans photographie individuelle clairement attribuable n'ont pas reçu d'image générique. Le parc ayant fermé définitivement le 30 septembre 2025, aucune grille tarifaire ni aucun horaire courant n'a été inventé ; les dernières informations 2025 sont documentées dans le récit historique. La météo alimentée par batch n'a pas conditionné la publication ni le retrait.
- Funny Land — la fiche publique atteint 96 avec 20 lieux actuels, les huit langues sur chaque description, huit médias officiels, le plan 2024, les horaires et tarifs vérifiés, deux jalons historiques et un article développé. Cinq lieux disposent d’une photographie individuelle clairement attribuable ; les quinze autres restent sans image plutôt que de recevoir un visuel générique. Aucun constructeur n’a été attribué sans source explicite : les sources spécialisées consultées décrivent Crazy Chenille comme un parcours de type Wacky Worm sans identifier son fabricant. La météo, alimentée par batch, n’a pas conditionné la publication ni le retrait.
- Fraispertuis City — la fiche publiée atteint 98 avec 70 éléments, les huit langues sur chaque description, les horaires et tarifs officiels 2026, huit jalons historiques, un article développé et sept médias propres au parc ou à ses attractions. Cinq attractions majeures disposent d’une photographie officielle ; les autres éléments conservent des descriptions sourcées sans image arbitraire. Les pages officielles très brèves ont conduit à retenir deux paragraphes spécifiques plutôt qu’un remplissage générique. La grille 2026, échue le 27 septembre, reste archivée dans le graphe mais l’API publique la masque désormais comme périmée ; aucune grille 2027 n’était publiée lors du contrôle. La page météo répondait correctement avec une prévision momentanément vide, ce qui n’a pas conditionné le retrait.
- Fiesta Parc — la fiche publiée atteint 100 avec 12 éléments actuels, 13 médias officiels, le calendrier et les tarifs 2026, cinq jalons historiques et un article développé. Chacune des dix attractions actuelles possède une photographie attribuable ; la Pizzeria Maisons et l'aire de pique-nique restent sans vue individuelle suffisamment explicite. Aucun plan public exploitable n'a été trouvé sur le site officiel, et l'ancien coaster retiré en 2019 n'a pas reçu l'image de Bassotto. La météo est bien accessible mais ne contenait momentanément aucune prévision lors du contrôle ; conformément à la règle du lot, ce contenu alimenté par batch n'a pas conditionné le retrait.
- Fermyland — la fiche publiée réunit 21 éléments actuels, 21 médias officiels, les horaires et tarifs 2026, cinq jalons historiques, un article développé et le constructeur LMQ Rides sourcé pour l’ancienne Chenille. Dix-neuf des 21 éléments actuels possèdent une image attribuable ; Les Rapidos et Le snack de Fermyland restent sans photographie individuelle suffisamment explicite, et l’ancienne Chenille fermée n’a pas reçu l’image de sa remplaçante. Un plan 2024 indexé par un office de tourisme a été retrouvé, mais son original et sa dérivée renvoient désormais 404 ; aucune carte dégradée ou non téléchargeable n’a été importée.
- Fééryland — les 30 attractions de l’inventaire officiel 2026 disposent chacune d’une photographie officielle, mais le site ne fournit pas de série de trois vues distinctes par attraction. Une seule vue générale actuelle et clairement attribuable au parc a été retenue ; les neuf services visibles restent sans photographie individuelle. Le logo déjà publié a été conservé sans inventer la provenance manquante de son fichier historique.
- Family Park — la fiche canonique a été réconciliée avec l'implantation actuelle de Sorigny, publiée avec ses 45 éléments, son calendrier 2026, ses tarifs 2026, son plan officiel, 46 médias propres, neuf jalons historiques et un article développé. Seul le point de restauration Ô ti’snack reste sans photographie attribuable avec certitude. L'ancien doublon artificiel `Family Park Monts` et ses deux parkItems ont été supprimés après consolidation ; le logo constructeur ZIERER partagé a été conservé.
- Le PAL — la fiche publique atteint 100 avec 131 éléments validés sur 132 enregistrements, le doublon ancien d’Azteka restant masqué et classé `NotRelevant`. Les huit langues couvrent chaque description ; 134 médias propres sont publiés, dont le logo, cinq vues du domaine et 128 photographies d’éléments. Le plan officiel 2025, 134 dates d’ouverture 2026, dix offres d’entrée, deux pass, le parking gratuit, treize jalons historiques et un article développé sont publics. La Chenille Fantastique est conservée comme attraction historique de 1981 à 2003. Les huit routes publiques contrôlées — fiche, éléments, météo, horaires, tarifs, histoire, images et plan — répondent en 200 ; la météo alimentée par batch n’a pas conditionné la complétude. L’annonce Facebook officielle existait déjà au statut `Published`, sans création de doublon.
- LennyPark — la fiche publique atteint 97 avec les 14 éléments de l’inventaire officiel, les huit langues sur chaque description, le logo, deux vues documentaires du parc et deux photographies d’éléments. Les 86 dates d’ouverture 2026, l’entrée libre, le bracelet journée, les lots de tickets et le parking gratuit sont structurés ; un jalon et un article développé relient l’ouverture du 18 avril 2026 à l’histoire du Parc du Tremblay. Les illustrations promotionnelles officielles des autres manèges dépassaient la limite d’import de production et n’ont pas été relayées par un proxy ni présentées comme des photographies. Les routes publiques de la fiche et de l’inventaire répondent en 200 avec horaires, tarifs et météo affichables ; la météo alimentée par batch n’a pas conditionné la complétude. L’annonce Facebook officielle existait déjà au statut `Published`, sans création de doublon.
- Breizh Land Parc — la fiche historique publique atteint 96 avec les 14 éléments attestés de l’unique saison 2018, les huit langues sur chaque description, six médias publiés, sept jalons sourcés et un article développé. Les horaires quotidiens de 10 h 30 à 18 h 30 et les tarifs historiques de 13,50 € pour les adultes, 10 € de trois à huit ans et la gratuité avant trois ans sont conservés dans le récit sans créer de grille actuelle pour un parc fermé. La Chenille est documentée comme montagne russe familiale principalement destinée aux enfants, sans inventer de seuil de taille absent des sources. La fiche et l’inventaire publics répondent en 200 et signalent correctement la fermeture définitive ; ni météo, ni horaires, ni tarifs courants ne sont affichés pour ce site disparu. L’annonce Facebook officielle existait déjà au statut `Published`, sans création de doublon.
- Lulu Parc — la fiche publique atteint 96 avec les 40 expériences et services nommés par les pages et le plan officiels 2026, les huit langues sur chaque description, douze médias officiels, la carte illustrée, 103 dates d’ouverture, six offres d’entrée, le pass annuel et les crédits des voitures électriques. Trois jalons sourcés et un article développé retracent la création de 1996, l’arrivée de Turbobob et le trentième anniversaire. Neuf éléments disposent d’une photographie individuelle attribuable ; les autres restent sans image plutôt que de recevoir un visuel générique ou une découpe artificielle du plan. Aucune condition d’accès n’a été inventée faute de seuil officiel exploitable. Les pages publiques de la fiche et de l’inventaire répondent en 200 avec horaires, tarifs et météo affichables ; la météo alimentée par batch n’a pas conditionné la complétude. L’annonce Facebook officielle existait déjà au statut `Published`, sans création de doublon.
- Luna Park d'Argelès-sur-Mer — la fiche publique atteint 96 avec 19 éléments validés : les 18 attractions, jeux et points de restauration nommément attestés, ainsi que le parking gratuit voisin. Les huit langues couvrent chaque description ; cinq médias sont publiés, dont le logo, trois vues générales de l'office de tourisme et une photographie de Techno. Les 76 soirées du 23 juin au 6 septembre 2026, l'entrée libre, le paiement séparé des métiers et le parking gratuit sont structurés ; cinq jalons sourcés et un article développé retracent la fondation de 1975, le transfert de 1993, Grand Huit, Cobra et la convention GLPA de 2023. L'office de tourisme annonce environ 40 attractions, mais les autres concessions ne sont pas nommées de façon suffisamment stable en ligne : elles n'ont pas été inventées. Les seuils d'accès et les tarifs unitaires restent absents faute de grille commune publiée ; la saison 2027 est datée du 26 juin au 6 septembre sans horaires détaillés. Les pages publiques de la fiche, de l'inventaire, de la météo, des horaires et des tarifs répondent en 200 ; la prévision météo momentanément vide, alimentée par batch, n'a pas conditionné la complétude. L'annonce Facebook officielle est confirmée au statut `Published`, sans création de doublon.
- Luna Park de Palavas — la fiche publique atteint 96 avec 17 éléments validés : douze attractions ou jeux nommés, deux offres familiales, deux services et le parking de l'allée des Loisirs ; Mach 1 est conservé avec son statut historique `Removed`. Les huit langues couvrent les 136 descriptions d'éléments ; dix médias propres sont publiés, dont l'affiche officielle, quatre vues générales actuelles ou d'archive et cinq photographies d'éléments. Les 80 soirées du 12 juin au 30 août 2026, de 20 h à 1 h, l'entrée libre, le paiement séparé des métiers et le parking gratuit de 700 places sont structurés. Quatre jalons et un article développé retracent la fondation de 1969, le transfert de 2026 et ses deux nouveautés. L'annonce municipale évoque environ 50 métiers et boutiques sans fournir de liste nominative exhaustive : aucun commerce supplémentaire n'a été inventé. Les tarifs unitaires, le prix du parking payant de 400 places, plusieurs constructeurs et certaines restrictions précises restent absents faute de publication officielle. Les pages publiques de la fiche, de l'inventaire, de la météo, des horaires et des tarifs répondent en 200 ; la météo alimentée par batch n'a pas conditionné la complétude. L'annonce Facebook officielle existait déjà au statut `Published`, sans création de doublon.
- Luna Park La Palmyre — la fiche publique atteint 96 avec 18 éléments validés : neuf montagnes russes actuelles ou historiques, cinq attractions actuelles, deux services, un commerce et un restaurant. Les huit langues couvrent chaque description ; cinq médias propres sont publiés, dont le logo 2026, l'affiche de saison, une vue nocturne et deux photographies d'éléments. Les 64 soirées du 27 juin au 29 août 2026, de 19 h à 2 h, l'entrée libre et le parking gratuit sont structurés ; le tarif de 3 € par attraction, hors offres extrêmes, reste explicitement présenté comme une information 2026 à reconfirmer. Quatre jalons et un article développé retracent la création de 1988, l'arrivée de Jet Star, l'incendie de 2010 et l'application officielle de 2026. L'inventaire saisonnier changeant ne permet pas d'attester davantage de métiers nommés sans les inventer, et plusieurs constructeurs ou restrictions restent absents faute de source fiable. Les pages publiques de la fiche, de l'inventaire, de la météo, des horaires et des tarifs répondent en 200 ; la météo alimentée par batch n'a pas conditionné la complétude. L'annonce Facebook officielle existait déjà au statut `Published`, sans création de doublon.
- Amigoland — la galerie officielle ne permet pas d’attribuer avec certitude une photographie distincte à Beach Party, Bomber Maxxx ou Gravity ; aucune image générique n’a donc été associée arbitrairement. Les tarifs unitaires des manèges ne sont pas publiés en ligne : la fiche indique uniquement les faits vérifiables, à savoir l’entrée et le parking gratuits puis le paiement séparé de chaque attraction.
- Bid’A Parc — le site et le plan officiels 2026 établissent l’inventaire courant, mais la galerie officielle ne fournit des photographies attribuables sans ambiguïté qu’au parc, au Carrousel, à Pomme, au Bateau Pirate, au Karting et au Palmito Resto. Les autres éléments restent donc sans image plutôt que de recevoir un visuel générique ou ancien. Aucun constructeur n’a été attribué sans source explicite. Le tarif du parking municipal varie selon les pages officielles consultées ; seule sa période payante est décrite, sans publier de grille contradictoire.

### Inventaire historique à confirmer

- Parc Saint Paul — L’Aire de Jeux Dino est attestée par le plan officiel 2025, mais elle est absente de l’inventaire officiel courant 2026 alors qu’une source secondaire 2026 la maintient. Grandyzer est également attesté par le plan officiel 2025 et absent de l’inventaire 2026. Faute de preuve explicite de fermeture ou de date fiable après recherche, aucun statut ni jalon historique n’a été inventé ; ces deux cas restent à reprendre lorsqu’une source probante apparaîtra.

## Ordre de traitement

1. migrer ou créer les 40 attractions autonomes avec `standaloneAttractionGraph`, sans conserver de faux parc parent ;
2. supprimer les 99 faux parcs certains et leurs dépendances après Preview contrôlée ;
3. reprendre les 13 fiches déjà publiques, par score croissant, pour supprimer rapidement les dettes et bloqueurs visibles ;
4. intégrer les fiches privées existantes, en donnant la priorité aux parcs majeurs et aux identités déjà bien établies ;
5. créer les parcs absents après recherche d'identité et de doublons ;
6. traiter les parcs fermés avec une exigence historique proportionnée ;
7. maintenir les autres anomalies hors publication tant que leur identité ou leur pertinence n'est pas résolue.

Les traitements API restent strictement séquentiels. Un contrôle global `operations/status` précède chaque export, Preview, Apply ou import de média ; tout `operation-busy` suspend le traitement jusqu'au délai recommandé.
