# PASS-20 — Stabilisation finale du passeport

Date : 2026-09-05  
Version : 5.0.43

## Résultat métier

Le Passeport ne dépend plus d'un interrupteur de déploiement temporaire pour afficher
les suggestions de révision d'une note globale. Tous les environnements suivent
désormais le même parcours applicatif. La personne conserve toutefois la maîtrise de
son expérience : sa préférence d'activation ou de désactivation reste persistée et
respectée.

Cette stabilisation ne modifie aucune note, visite, occurrence ni préférence déjà
enregistrée. Elle ne nécessite aucune migration MongoDB.

## Inventaire des chemins temporaires

Six flags avaient été envisagés au cadrage. Les visites, occurrences, observations
temporelles, statistiques et brouillons anonymes ont été livrés directement par
tranches compatibles et n'ont pas produit de flag de runtime. Seul
`Features:Passport:GlobalRatingSuggestions:Enabled` existait réellement.

Ce dernier flag, son port Application, son implémentation Infrastructure, son
enregistrement d'injection de dépendances et sa configuration ont été supprimés.
Le contrat HTTP additif `isAvailable` reste présent et vaut désormais toujours
`true`, afin de ne pas casser les clients déjà déployés. `isEnabled` continue de
représenter uniquement la préférence de la personne.

## Architecture stabilisée

```text
Interface Angular
  └─ GlobalRatingSuggestionsStateFacade
       └─ GLOBAL_RATING_SUGGESTIONS_API_PORT
            └─ API privée /api/me/passport/rating-update-suggestions
                 └─ handlers Application
                      ├─ préférence utilisateur persistée
                      ├─ sources privées bornées
                      ├─ métadonnées parc/attraction
                      └─ GlobalRatingSuggestionPolicy (Core)
```

Les responsabilités restent séparées : Core décide de l'éligibilité, Application
orchestre les ports, Infrastructure persiste les états et WebAPI transporte les
résultats. Aucun contrôleur ni composant Angular ne récupère de règle métier.

## Comportements garantis

- une préférence désactivée renvoie une fonctionnalité disponible mais inactive ;
- ce chemin s'arrête avant toute lecture des observations privées ;
- une préférence réactivée retrouve le même moteur de suggestion ;
- la présentation et la résolution restent bornées, versionnées et idempotentes ;
- aucun identifiant, texte libre ou contenu privé supplémentaire n'est exposé ;
- les collections et index MongoDB existants restent inchangés.

## Preuves automatisées

- tests Application du chemin actif, de l'opt-out précoce et de la persistance de la
  préférence ;
- tests Application des présentations et interactions idempotentes ;
- test Infrastructure des ports réellement enregistrés après retrait du gate ;
- contrôle de configuration pour l'absence de clé morte ;
- contrôle architectural « une classe par fichier », avec suppression des deux
  doubles de test imbriqués dans le fichier modifié.

## Limite assumée

PASS-20 stabilise le logiciel ; il ne remplace pas la validation qualitative terrain
de la gate `PASS-G`. Les entretiens, scénarios guidés et mesures de charge décrits par
PASS-19 restent des preuves métier à collecter avant de déclarer cette gate validée.
