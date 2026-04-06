# Phase 2 - compile/tooling fix

Cette itération ne fait pas avancer la migration métier.
Elle corrige d'abord les points bloquants de démarrage/chargement du squelette :

- ajout d'un profil `IIS Express` dans `API/AmusementPark.WebAPI/Properties/launchSettings.json`
- ajout des `iisSettings`
- normalisation des GUID de type projet SDK-style dans `AmusementPark.sln` pour les 4 nouveaux projets
- nettoyage des dossiers `bin` et `obj` du zip livré

Objectif : obtenir une base phase 1/2 qui se charge proprement dans Visual Studio / Rider avant toute suite de migration.
