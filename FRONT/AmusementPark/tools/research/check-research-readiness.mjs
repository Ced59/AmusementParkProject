import { readFile } from 'node:fs/promises';
import { dirname, isAbsolute, relative, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const toolDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(toolDirectory, '../../../..');
const defaultCatalogPath = resolve(repositoryRoot, 'docs/product/research/catalog.json');
const roadmapRoot = resolve(repositoryRoot, 'docs/roadmaps/product-growth');
const productDocumentationRoot = resolve(repositoryRoot, 'docs/product');

export const requiredProgramIds = Object.freeze([
  'RANK',
  'PASS',
  'SHARE',
  'FIT',
  'WATCH',
  'TRIP',
  'HIST',
  'LIVE',
]);

export const requiredRoadmapByProgram = Object.freeze({
  RANK: 'docs/roadmaps/product-growth/01-ranking-trust-and-methodology-roadmap.md',
  PASS: 'docs/roadmaps/product-growth/02-visit-passport-and-ride-log-roadmap.md',
  SHARE: 'docs/roadmaps/product-growth/03-shareable-recaps-and-comparisons-roadmap.md',
  FIT: 'docs/roadmaps/product-growth/04-park-fit-recommendation-and-comparison-roadmap.md',
  WATCH: 'docs/roadmaps/product-growth/05-favorites-watchlists-and-factual-alerts-roadmap.md',
  TRIP: 'docs/roadmaps/product-growth/06-collaborative-trip-planning-roadmap.md',
  HIST: 'docs/roadmaps/product-growth/07-park-history-explorer-roadmap.md',
  LIVE: 'docs/roadmaps/product-growth/08-live-wait-times-and-crowd-intelligence-roadmap.md',
});

export const requiredExtensionDocumentsByProgram = Object.freeze({
  PASS: Object.freeze([
    'docs/product/passport-beta-validation-protocol.md',
  ]),
});

export const requiredProfileIds = Object.freeze([
  'journal-enthusiast',
  'tool-free-enthusiast',
  'occasional-visitor',
  'family-planner',
  'assistive-technology',
  'modest-device-network',
  'historical-contributor',
]);

export const requiredProfileLabels = Object.freeze({
  'journal-enthusiast': 'Passionné tenant déjà un journal',
  'tool-free-enthusiast': 'Passionné sans outil structuré',
  'occasional-visitor': 'Visiteur occasionnel',
  'family-planner': 'Famille préparant une sortie',
  'assistive-technology': "Personne utilisant le clavier ou une technologie d'assistance",
  'modest-device-network': 'Personne sur appareil ou connexion modeste',
  'historical-contributor': 'Contributeur historique',
});

export const requiredProfileIdsByProgram = Object.freeze({
  RANK: Object.freeze([
    'journal-enthusiast',
    'tool-free-enthusiast',
    'occasional-visitor',
    'assistive-technology',
    'modest-device-network',
    'historical-contributor',
  ]),
  PASS: Object.freeze([
    'journal-enthusiast',
    'tool-free-enthusiast',
    'occasional-visitor',
    'assistive-technology',
    'modest-device-network',
  ]),
  SHARE: Object.freeze([
    'journal-enthusiast',
    'occasional-visitor',
    'family-planner',
    'assistive-technology',
    'modest-device-network',
  ]),
  FIT: Object.freeze([
    'tool-free-enthusiast',
    'occasional-visitor',
    'family-planner',
    'assistive-technology',
    'modest-device-network',
  ]),
  WATCH: Object.freeze([
    'journal-enthusiast',
    'tool-free-enthusiast',
    'occasional-visitor',
    'family-planner',
    'assistive-technology',
    'modest-device-network',
  ]),
  TRIP: Object.freeze([
    'journal-enthusiast',
    'occasional-visitor',
    'family-planner',
    'assistive-technology',
    'modest-device-network',
  ]),
  HIST: Object.freeze([
    'journal-enthusiast',
    'tool-free-enthusiast',
    'occasional-visitor',
    'assistive-technology',
    'modest-device-network',
    'historical-contributor',
  ]),
  LIVE: Object.freeze([
    'journal-enthusiast',
    'tool-free-enthusiast',
    'occasional-visitor',
    'family-planner',
    'assistive-technology',
    'modest-device-network',
  ]),
});

export const requiredTaskIds = Object.freeze([
  'consent',
  'primary-value',
  'core-concept-distinction',
  'label-comprehension',
  'privacy-export-deletion',
  'unknown-data',
  'accessible-use',
  'delayed-return',
]);

export const requiredTaskSemantics = Object.freeze({
  consent: Object.freeze({
    instruction: "Présenter la finalité, les données conservées et le droit d'arrêter avant toute observation.",
    comparableMeasure: 'consent-confirmed-before-observation',
  }),
  'primary-value': Object.freeze({
    instruction: "Donner l'objectif métier sans indiquer le chemin, puis observer le premier succès sans aide immédiate.",
    comparableMeasure: 'first-success-outcome',
  }),
  'core-concept-distinction': Object.freeze({
    instruction: "Demander à la personne d'expliquer avec ses propres mots les concepts que le produit ne doit pas confondre.",
    comparableMeasure: 'concepts-explained-without-prompt',
  }),
  'label-comprehension': Object.freeze({
    instruction: 'Faire relire les libellés déterminants et relever uniquement les hésitations observées.',
    comparableMeasure: 'critical-labels-understood',
  }),
  'privacy-export-deletion': Object.freeze({
    instruction: "Vérifier les contrôles d'export et de suppression applicables, ou faire constater explicitement qu'aucune donnée personnelle n'est créée.",
    comparableMeasure: 'privacy-controls-correctly-predicted',
  }),
  'unknown-data': Object.freeze({
    instruction: "Présenter une donnée absente, ancienne ou insuffisante et demander ce que l'interface permet réellement de conclure.",
    comparableMeasure: 'unknown-remains-distinct-from-negative',
  }),
  'accessible-use': Object.freeze({
    instruction: "Réaliser le scénario principal au clavier ou avec la technologie d'assistance habituelle, puis sur un appareil ou réseau modeste.",
    comparableMeasure: 'primary-scenario-accessible',
  }),
  'delayed-return': Object.freeze({
    instruction: 'Prévoir un retour différé sans rappel guidé pour mesurer la valeur répétée plutôt que la seule découverte.',
    comparableMeasure: 'delayed-return-outcome',
  }),
});

export const requiredEvidenceIds = Object.freeze([
  'objective',
  'profile',
  'scenario',
  'outcome',
  'observedFacts',
  'authorizedQuote',
  'issues',
  'hypotheses',
  'decisions',
  'inconclusive',
]);

export const requiredEvidenceConstraints = Object.freeze({
  objective: 'Question décidée avant la session',
  profile: 'Profil canonique et contexte utile, sans identité civile',
  scenario: 'Tâche et produit testés',
  outcome: 'unassisted, assisted, failed ou not-observable',
  observedFacts: 'Actions vues, sans interprétation',
  authorizedQuote: "Citation courte seulement si le consentement l'autorise",
  issues: 'Problème et sévérité critical, high, medium ou low',
  hypotheses: 'Hypothèses séparées des faits',
  decisions: 'Corriger, approfondir, accepter avec justification ou arrêter',
  inconclusive: 'Éléments insuffisants explicitement conservés comme tels',
});

export const requiredStatusPolicyStatement = "Les protocoles sont exécutables, mais aucune observation terrain n'est revendiquée tant qu'une fiche de session consentie n'existe pas.";

export const requiredBetaGateIds = Object.freeze([
  'internal-alpha',
  'closed-beta',
  'limited-open-beta',
  'general-availability',
]);

export const requiredBetaGateRequirements = Object.freeze({
  'internal-alpha': Object.freeze([
    'données de test uniquement',
    'aucune publication publique',
    'instrumentation disponible',
    'erreurs visibles',
    'export et suppression vérifiés',
    'feature flag administrable',
  ]),
  'closed-beta': Object.freeze([
    'invitation manuelle',
    'données réelles',
    'consentement clair',
    'support direct',
    'migrations réversibles',
    'limites connues',
    'mesure qualitative',
  ]),
  'limited-open-beta': Object.freeze([
    'capacité VPS vérifiée',
    'support dimensionné',
    'modération dimensionnée',
    'monitoring disponible',
    'documentation disponible',
    'réponse aux incidents opérable',
    'aucune promesse de disponibilité excessive',
  ]),
  'general-availability': Object.freeze([
    'valeur répétée réellement observée',
    'erreurs sous le seuil décidé',
    'confidentialité validée',
    'accessibilité validée',
    'huit langues vérifiées',
    'performance vérifiée',
    'coûts acceptables',
    'flags temporaires retirés',
    'runbook disponible',
  ]),
});

export const requiredTaskContextFields = Object.freeze([
  'primaryScenario',
  'coreConceptDistinction',
  'labelReview',
  'privacyExportDeletionScope',
  'unknownDataScenario',
  'delayedReturnTrigger',
  'accessibleMode',
]);

export const requiredTaskContextsByProgram = Object.freeze({
  RANK: Object.freeze({
    primaryScenario: 'Choisir un classement, ouvrir un résultat et retrouver les preuves qui justifient sa position.',
    coreConceptDistinction: 'Moyenne brute, score agrégé, rang principal et absence de rang faute de contributeurs.',
    labelReview: 'Rang principal, volume, confiance, données insuffisantes et méthode.',
    privacyExportDeletionScope: "Vérifier les préférences personnelles et leur export ou suppression ; le classement public ne doit pas révéler l'identité d'un votant.",
    unknownDataScenario: 'Un lieu possède une moyenne mais pas assez de contributeurs pour publier un rang.',
    delayedReturnTrigger: 'Revenir comparer le même lieu après une nouvelle publication de snapshot.',
    accessibleMode: "Parcourir et comparer les résultats sans dépendre de la couleur ni d'un pointeur précis.",
  }),
  PASS: Object.freeze({
    primaryScenario: 'Créer, organiser et terminer une visite avec plusieurs tours de la même attraction.',
    coreConceptDistinction: "Note globale actuelle, note privée de visite et note privée d'un tour.",
    labelReview: 'Brouillon, terminée, privée, tour effectué, date approximative et statistiques.',
    privacyExportDeletionScope: "Exporter le passeport puis supprimer une donnée de test en prédisant précisément la portée de l'action.",
    unknownDataScenario: "Créer une ancienne visite dont seule l'année est connue et traiter une attraction désormais fermée.",
    delayedReturnTrigger: 'Revenir sans aide enregistrer et terminer une deuxième visite.',
    accessibleMode: 'Réordonner la timeline et terminer la visite au clavier puis sur mobile étroit.',
  }),
  SHARE: Object.freeze({
    primaryScenario: "Choisir les sections d'un récapitulatif, relire l'aperçu, publier et vérifier le lien public.",
    coreConceptDistinction: 'Donnée privée source, section explicitement publiée, comparaison publique et lien révoqué.',
    labelReview: 'Aperçu public, visible, masqué, révoquer et comparer.',
    privacyExportDeletionScope: 'Exporter la politique de partage puis révoquer et supprimer la publication de test sans effacer le passeport privé.',
    unknownDataScenario: "Comparer deux passeports publics lorsqu'une statistique manque chez l'un des membres.",
    delayedReturnTrigger: "Revenir modifier la politique d'un partage existant puis contrôler l'ancien lien.",
    accessibleMode: 'Composer et vérifier un partage au clavier et à 200 % de zoom.',
  }),
  FIT: Object.freeze({
    primaryScenario: 'Préparer une sortie avec des contraintes réelles, comparer deux résultats et sauvegarder un projet pertinent.',
    coreConceptDistinction: 'Compatibilité avec les critères, préférence déclarée, donnée inconnue et impossibilité factuelle.',
    labelReview: 'Pourquoi ce résultat, données inconnues, incompatible, comparer et sauvegarder.',
    privacyExportDeletionScope: 'Exporter puis supprimer les préférences ou le projet de test sans affecter les données publiques du parc.',
    unknownDataScenario: "Un parc ne possède pas une donnée nécessaire à l'un des critères importants.",
    delayedReturnTrigger: 'Revenir sur le projet après quelques jours pour ajuster une contrainte et refaire le choix.',
    accessibleMode: 'Comparer les résultats au clavier sur téléphone et connexion limités.',
  }),
  WATCH: Object.freeze({
    primaryScenario: 'Ajouter un favori, créer une alerte factuelle, relire sa condition puis la désactiver.',
    coreConceptDistinction: 'Favori, abonnement, observation factuelle, notification envoyée et événement encore inconnu.',
    labelReview: 'Suivre, condition, source, dernière vérification, suspendre et supprimer.',
    privacyExportDeletionScope: "Exporter les suivis puis supprimer l'alerte de test en distinguant abonnement et historique d'envoi.",
    unknownDataScenario: "La source n'a pas publié de valeur récente et aucune alerte ne peut être confirmée.",
    delayedReturnTrigger: 'Revenir après un changement factuel pour vérifier la notification et son explication.',
    accessibleMode: "Créer et suspendre une alerte au clavier sans dépendre de la couleur d'état.",
  }),
  TRIP: Object.freeze({
    primaryScenario: 'Créer un voyage, inviter deux rôles, proposer une étape et résoudre une décision contradictoire.',
    coreConceptDistinction: 'Propriétaire, participant, invitation, proposition, décision et élément privé hors du groupe.',
    labelReview: 'Inviter, rôle, proposer, décider, quitter et supprimer le voyage.',
    privacyExportDeletionScope: 'Exporter le voyage puis quitter ou supprimer selon son rôle en prédisant ce que les autres conservent.',
    unknownDataScenario: "Une étape comporte un prix ou un horaire inconnu au moment de décider.",
    delayedReturnTrigger: "Revenir après une modification effectuée par un autre participant et retrouver la décision courante.",
    accessibleMode: 'Réorganiser et valider le plan au clavier sur une largeur mobile.',
  }),
  HIST: Object.freeze({
    primaryScenario: "Choisir une année, suivre une chronologie et ouvrir la preuve d'un changement durable.",
    coreConceptDistinction: "État actuel, fait historique, intervalle approximatif, source et absence de preuve.",
    labelReview: 'À cette date, période estimée, actuel, source, correction et chronologie.',
    privacyExportDeletionScope: "Vérifier qu'une navigation publique ne crée pas de dossier personnel ; exporter ou supprimer seulement les contributions ou préférences applicables.",
    unknownDataScenario: "La fermeture d'une attraction est attestée mais son mois exact reste inconnu.",
    delayedReturnTrigger: "Revenir après l'ajout d'une source et retrouver ce qui a changé dans la chronologie.",
    accessibleMode: 'Parcourir une chronologie au clavier sans dépendre de sa géométrie ou de ses couleurs.',
  }),
  LIVE: Object.freeze({
    primaryScenario: "Consulter les attentes d'un parc, comparer deux attractions et comprendre le repli lorsque la source disparaît.",
    coreConceptDistinction: 'Observation courante, historique, prévision, fraîcheur, source et vote communautaire.',
    labelReview: 'Mis à jour, observé, estimé, source indisponible, historique et prévision.',
    privacyExportDeletionScope: 'Exporter ou supprimer les contributions personnelles applicables sans effacer les observations publiques agrégées légitimes.',
    unknownDataScenario: 'La source est indisponible ou la dernière observation est trop ancienne pour guider une décision.',
    delayedReturnTrigger: 'Revenir plus tard dans la journée et expliquer pourquoi la valeur ou son niveau de confiance a changé.',
    accessibleMode: "Comparer les attentes au clavier sur réseau instable sans dépendre d'une animation.",
  }),
});

export const requiredProgramDecisionsByProgram = Object.freeze({
  RANK: Object.freeze({
    businessQuestion: "Les visiteurs comprennent-ils pourquoi un lieu est classé et ce que les preuves permettent réellement d'affirmer ?",
    firstSuccess: 'Trouver un classement pertinent puis expliquer son ordre sans confondre moyenne brute, score agrégé et rang publié.',
    stopConditions: Object.freeze([
      'Le rang reste interprété comme une moyenne simple après corrections répétées.',
      "La confiance exige d'exposer des données personnelles ou une précision statistique injustifiée.",
    ]),
    generalizationEvidence: Object.freeze([
      'La méthode et les volumes sont correctement reformulés par plusieurs profils.',
      'Les lieux non classés restent compris comme inconnus ou insuffisamment documentés.',
    ]),
  }),
  PASS: Object.freeze({
    businessQuestion: 'Le passeport remplace-t-il avantageusement une note libre tout en restant compréhensible et privé par défaut ?',
    firstSuccess: "Créer une visite, ajouter plusieurs tours et retrouver l'historique sans assistance.",
    stopConditions: Object.freeze([
      'La deuxième utilisation reste absente malgré une première activation réussie.',
      'La distinction des notes ou la portée de la suppression reste incomprise après corrections répétées.',
    ]),
    generalizationEvidence: Object.freeze([
      'Plusieurs testeurs terminent une seconde visite sans assistance.',
      'Les trois types de notes et leur influence communautaire sont correctement distingués.',
    ]),
  }),
  SHARE: Object.freeze({
    businessQuestion: 'Une personne peut-elle publier un récapitulatif utile en comprenant exactement ce qui devient public et comment le révoquer ?',
    firstSuccess: 'Prévisualiser, publier puis révoquer un partage sans exposer un champ resté privé.',
    stopConditions: Object.freeze([
      "Un testeur publie une donnée qu'il pensait privée.",
      'La révocation ou la suppression ne produit pas un résultat immédiatement vérifiable.',
    ]),
    generalizationEvidence: Object.freeze([
      'Les participants prédisent correctement chaque champ rendu public avant publication.',
      "Un lien révoqué n'est jamais interprété comme encore partageable.",
    ]),
  }),
  FIT: Object.freeze({
    businessQuestion: 'Park Fit aide-t-il réellement à réduire un choix sans déguiser les données manquantes en recommandation certaine ?',
    firstSuccess: 'Décrire ses contraintes, obtenir des résultats et expliquer au moins une recommandation et une inconnue.',
    stopConditions: Object.freeze([
      'La recommandation est comprise comme une garantie ou une probabilité calculée.',
      'Les inconnues sont régulièrement lues comme des réponses négatives.',
    ]),
    generalizationEvidence: Object.freeze([
      'Les raisons et inconnues sont reformulées correctement par plusieurs profils.',
      "Le choix final reste attribué à l'utilisateur, jamais présenté comme une vérité algorithmique.",
    ]),
  }),
  WATCH: Object.freeze({
    businessQuestion: 'Les favoris et alertes restent-ils utiles, factuels et contrôlables sans créer de pression à revenir ?',
    firstSuccess: "Suivre une cible, comprendre la condition d'alerte et arrêter le suivi sans ambiguïté.",
    stopConditions: Object.freeze([
      'Les notifications poussent à revenir sans fait nouveau vérifiable.',
      "La charge de modération ou la fréquence d'envoi dépasse les moyens d'exploitation.",
    ]),
    generalizationEvidence: Object.freeze([
      "La condition et la source d'une alerte sont comprises avant activation.",
      'La suspension et la suppression sont retrouvées sans assistance.',
    ]),
  }),
  TRIP: Object.freeze({
    businessQuestion: 'Un groupe peut-il construire un séjour ensemble sans perdre la maîtrise des rôles, des décisions ou des données privées ?',
    firstSuccess: 'Créer un voyage, inviter un participant et prendre une décision commune sans contourner les permissions.',
    stopConditions: Object.freeze([
      'Un rôle peut agir au-delà de ce que les participants comprennent ou acceptent.',
      'Les conflits produisent des pertes silencieuses ou exigent un support disproportionné.',
    ]),
    generalizationEvidence: Object.freeze([
      "Les rôles et conséquences d'une action sont correctement prédits.",
      'Le groupe retrouve une décision commune après un retour différé.',
    ]),
  }),
  HIST: Object.freeze({
    businessQuestion: "L'exploration historique permet-elle de comprendre l'évolution d'un parc sans mélanger faits datés, incertitudes et état actuel ?",
    firstSuccess: "Explorer une période, relier un changement à sa source et distinguer l'historique de l'inventaire actuel.",
    stopConditions: Object.freeze([
      'Une date approximative est régulièrement interprétée comme exacte.',
      "La provenance ou l'état actuel devient impossible à retrouver sans expertise éditoriale.",
    ]),
    generalizationEvidence: Object.freeze([
      'Faits, estimations et lacunes sont distingués par plusieurs profils.',
      "La navigation temporelle reste compréhensible sans explication de l'équipe.",
    ]),
  }),
  LIVE: Object.freeze({
    businessQuestion: 'Les données en direct aident-elles à décider sans être prises pour une promesse ni contaminer les notes communautaires ?',
    firstSuccess: 'Lire une attente, sa fraîcheur et sa provenance puis prendre une décision proportionnée à sa fiabilité.',
    stopConditions: Object.freeze([
      'Les visiteurs prennent une prévision pour une garantie malgré les corrections de libellé.',
      'La source ou la charge de collecte ne peut pas être exploitée honnêtement sur le VPS.',
    ]),
    generalizationEvidence: Object.freeze([
      "Fraîcheur, source et niveau d'incertitude sont correctement reformulés.",
      "Aucune observation temporelle n'est confondue avec une note ou un vote.",
    ]),
  }),
});

export const requiredProtocolDocumentPaths = Object.freeze([
  'docs/product/research/README.md',
  'docs/product/research/session-result-template.md',
]);

export const requiredProtocolDocumentHeadings = Object.freeze({
  'docs/roadmaps/product-growth/01-ranking-trust-and-methodology-roadmap.md': Object.freeze([
    '## 19. Gate finale `RANK-G`',
  ]),
  'docs/roadmaps/product-growth/02-visit-passport-and-ride-log-roadmap.md': Object.freeze([
    '## 27. `PASS-G` — socle technique et suivi terrain',
  ]),
  'docs/roadmaps/product-growth/03-shareable-recaps-and-comparisons-roadmap.md': Object.freeze([
    '## 21. Gate finale `SHARE-G`',
  ]),
  'docs/roadmaps/product-growth/04-park-fit-recommendation-and-comparison-roadmap.md': Object.freeze([
    '## 22. Gate finale `FIT-G`',
  ]),
  'docs/roadmaps/product-growth/05-favorites-watchlists-and-factual-alerts-roadmap.md': Object.freeze([
    '## 19. Gate finale `WATCH-G`',
  ]),
  'docs/roadmaps/product-growth/06-collaborative-trip-planning-roadmap.md': Object.freeze([
    '## 23. Gate finale `TRIP-G`',
  ]),
  'docs/roadmaps/product-growth/07-park-history-explorer-roadmap.md': Object.freeze([
    '## 22. Gate finale `HIST-G`',
  ]),
  'docs/roadmaps/product-growth/08-live-wait-times-and-crowd-intelligence-roadmap.md': Object.freeze([
    '## 24. Gate finale `LIVE-G`',
  ]),
  'docs/product/research/README.md': Object.freeze([
    '# Protocole commun de recherche produit',
    '## 2. Préparer une session',
    '## 3. Conduire les huit tâches',
    '## 4. Séparer les faits des décisions',
    '## 5. Passer une gate de bêta',
    "## 6. Conditions d'arrêt communes",
  ]),
  'docs/product/research/session-result-template.md': Object.freeze([
    '# Fiche de session produit',
    '## Cadre',
    '## Résultats par tâche',
    '## Problèmes',
    '## Synthèse minimisée',
    '## Clôture',
  ]),
  'docs/product/passport-beta-validation-protocol.md': Object.freeze([
    '# Protocole de validation de la bêta passeport',
    '## Objectif',
    '## Cohorte minimale',
    '## Préparation',
    '## Scénarios',
    '## Fiche de résultat',
    '## Critères de validation qualitative `PASS-G` — suivi non bloquant',
    "## Conditions d'arrêt ou de réduction",
  ]),
});

const allowedOutcomeValues = ['unassisted', 'assisted', 'failed', 'not-observable'];
const minimumResearchSectionContentLength = 20;
const sessionResultTableHeaders = Object.freeze([
  'Tâche commune',
  'Contexte produit présenté',
  'Résultat',
  'Faits observés',
  'Aide minimale donnée',
]);

function isNonEmptyText(value) {
  return typeof value === 'string' && value.trim().length > 0;
}

function markdownHeadingLevel(line) {
  const match = line.match(/^ {0,3}(#{1,6})\s+/);
  return match ? match[1].length : null;
}

function markdownHeadingText(line) {
  const match = line.match(/^ {0,3}(#{1,6}\s+.*?\S)\s*$/);
  return match ? match[1] : null;
}

function isIndentedCodeLine(line) {
  return /^(?: {4}|\t)/.test(line);
}

function stripNonRenderedHtmlBlocks(content) {
  const blockTags = 'pre|script|style|textarea|xmp|iframe|noembed|noframes|listing';
  const pairedBlockPattern = new RegExp(
    `<(${blockTags})\\b[^>]*>[\\s\\S]*?<\\/\\1\\s*>`,
    'gi',
  );
  const unclosedBlockPattern = new RegExp(
    `<(?:${blockTags}|plaintext)\\b[^>]*>[\\s\\S]*$`,
    'gi',
  );
  return content
    .replace(pairedBlockPattern, '')
    .replace(unclosedBlockPattern, '');
}

function markdownSectionBodyLines(lines, headingIndex) {
  const headingLevel = markdownHeadingLevel(lines[headingIndex]);
  let sectionEndIndex = lines.length;
  for (let index = headingIndex + 1; index < lines.length; index += 1) {
    const candidateLevel = markdownHeadingLevel(lines[index]);
    if (candidateLevel !== null && candidateLevel <= headingLevel) {
      sectionEndIndex = index;
      break;
    }
  }

  return lines.slice(headingIndex + 1, sectionEndIndex);
}

function excludeFencedCodeBlocks(lines) {
  const visibleLines = [];
  let activeFence = null;
  for (const line of lines) {
    if (activeFence !== null) {
      const closingFenceMatch = line.match(/^ {0,3}(`{3,}|~{3,})[ \t]*$/);
      if (closingFenceMatch
        && activeFence.character === closingFenceMatch[1][0]
        && closingFenceMatch[1].length >= activeFence.length) {
        activeFence = null;
      }
      continue;
    }

    const openingFenceMatch = line.match(/^ {0,3}(`{3,}|~{3,})/);
    if (openingFenceMatch) {
      activeFence = Object.freeze({
        character: openingFenceMatch[1][0],
        length: openingFenceMatch[1].length,
      });
      continue;
    }

    visibleLines.push(line);
  }
  return visibleLines;
}

function markdownTableCells(line) {
  if (isIndentedCodeLine(line)) {
    return null;
  }

  const trimmedLine = line.trim();
  if (!trimmedLine.startsWith('|') || !trimmedLine.endsWith('|')) {
    return null;
  }

  return trimmedLine
    .slice(1, -1)
    .split('|')
    .map((cell) => cell.trim());
}

function isMarkdownTableDelimiter(line, columnCount) {
  const cells = markdownTableCells(line);
  return cells !== null
    && cells.length === columnCount
    && cells.every((cell) => /^:?-{3,}:?$/.test(cell));
}

function hasMeaningfulSectionContent(lines, headingIndex) {
  const normalizedContent = excludeFencedCodeBlocks(markdownSectionBodyLines(lines, headingIndex))
    .filter((line) => !line.trim().startsWith('<!--')
      && markdownHeadingLevel(line) === null
      && !isIndentedCodeLine(line))
    .join(' ')
    .replace(/[`*_>#|\[\]():-]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();
  return normalizedContent.length >= minimumResearchSectionContentLength;
}

function hasSameOrderedValues(actualValues, expectedValues) {
  return Array.isArray(actualValues)
    && actualValues.length === expectedValues.length
    && expectedValues.every((expectedValue, index) => actualValues[index] === expectedValue);
}

function hasOnlyRequiredIds(items, requiredIds, label, errors) {
  if (!Array.isArray(items)) {
    errors.push(`${label}: tableau absent.`);
    return new Set();
  }

  const ids = new Set();
  for (const item of items) {
    if (!isNonEmptyText(item?.id)) {
      errors.push(`${label}: identifiant absent.`);
      continue;
    }
    if (ids.has(item.id)) {
      errors.push(`${label}: identifiant dupliqué ${item.id}.`);
    }
    ids.add(item.id);
  }

  for (const requiredId of requiredIds) {
    if (!ids.has(requiredId)) {
      errors.push(`${label}: identifiant obligatoire absent ${requiredId}.`);
    }
  }
  for (const id of ids) {
    if (!requiredIds.includes(id)) {
      errors.push(`${label}: identifiant inconnu ${id}.`);
    }
  }
  return ids;
}

function isPathInside(candidatePath, allowedRoot) {
  if (!isNonEmptyText(candidatePath) || !candidatePath.endsWith('.md')) {
    return false;
  }

  const candidate = resolve(repositoryRoot, candidatePath);
  const pathFromRoot = relative(allowedRoot, candidate);
  return pathFromRoot.length > 0
    && !isAbsolute(pathFromRoot)
    && pathFromRoot !== '..'
    && !pathFromRoot.startsWith(`..${process.platform === 'win32' ? '\\' : '/'}`);
}

export function isProductGrowthRoadmapPath(candidatePath) {
  return isPathInside(candidatePath, roadmapRoot);
}

export function isResearchExtensionPath(candidatePath) {
  return isPathInside(candidatePath, productDocumentationRoot);
}

export function validateResearchDocumentContent(documentPath, content) {
  const renderedContent = typeof content === 'string'
    ? stripNonRenderedHtmlBlocks(content.replace(/<!--[\s\S]*?(?:-->|$)/g, ''))
    : content;
  if (!isNonEmptyText(renderedContent)) {
    return [`Document de recherche vide: ${documentPath}.`];
  }

  const errors = [];
  const lines = excludeFencedCodeBlocks(renderedContent.split(/\r?\n/));
  for (const heading of requiredProtocolDocumentHeadings[documentPath] ?? []) {
    const headingIndex = lines.findIndex((line) => markdownHeadingText(line) === heading);
    if (headingIndex < 0) {
      errors.push(`Document ${documentPath}: section obligatoire absente ${heading}.`);
    } else if (!hasMeaningfulSectionContent(lines, headingIndex)) {
      errors.push(`Document ${documentPath}: section obligatoire vide ou insuffisante ${heading}.`);
    }
  }

  if (documentPath === 'docs/product/research/session-result-template.md') {
    const resultsHeadingIndex = lines.findIndex((line) => markdownHeadingText(line) === '## Résultats par tâche');
    const resultLines = resultsHeadingIndex < 0
      ? []
      : markdownSectionBodyLines(lines, resultsHeadingIndex);
    const tableHeaderIndex = resultLines.findIndex((line) => {
      const cells = markdownTableCells(line);
      return cells !== null
        && cells.length === sessionResultTableHeaders.length
        && cells.every((cell, index) => cell === sessionResultTableHeaders[index]);
    });
    const hasTableDelimiter = tableHeaderIndex >= 0
      && isMarkdownTableDelimiter(
        resultLines[tableHeaderIndex + 1] ?? '',
        sessionResultTableHeaders.length,
      );
    if (!hasTableDelimiter) {
      errors.push(`Document ${documentPath}: tableau de résultats canonique absent ou invalide.`);
    }

    const taskRows = [];
    if (hasTableDelimiter) {
      for (let index = tableHeaderIndex + 2; index < resultLines.length; index += 1) {
        const cells = markdownTableCells(resultLines[index]);
        if (cells === null || cells.length !== sessionResultTableHeaders.length) {
          break;
        }
        taskRows.push(cells[0]);
      }
    }
    for (const taskId of requiredTaskIds) {
      if (!taskRows.includes(taskId)) {
        errors.push(`Document ${documentPath}: ligne de tâche obligatoire absente ${taskId}.`);
      }
    }
    if (hasTableDelimiter
      && (taskRows.length !== requiredTaskIds.length
        || requiredTaskIds.some((taskId, index) => taskRows[index] !== taskId))) {
      errors.push(`Document ${documentPath}: ordre canonique des tâches du tableau modifié.`);
    }

    for (const retentionRequirement of [
      Object.freeze({
        heading: '## Cadre',
        field: '- Date de suppression prévue pour cette fiche :',
      }),
      Object.freeze({
        heading: '## Clôture',
        field: '- Fiche supprimée à la date prévue :',
      }),
    ]) {
      const headingIndex = lines.findIndex((line) => markdownHeadingText(line) === retentionRequirement.heading);
      const sectionLines = headingIndex < 0
        ? []
        : markdownSectionBodyLines(lines, headingIndex).map((line) => line.trimEnd());
      if (!sectionLines.includes(retentionRequirement.field)) {
        errors.push(`Document ${documentPath}: champ de rétention obligatoire absent ${retentionRequirement.field}.`);
      }
    }
  }

  return errors;
}

export function validateResearchCatalog(catalog) {
  const errors = [];
  const canonicalProfiles = Array.isArray(catalog?.canonicalProfiles) ? catalog.canonicalProfiles : [];
  const commonTasks = Array.isArray(catalog?.commonTasks) ? catalog.commonTasks : [];
  const evidenceSchema = Array.isArray(catalog?.evidenceSchema) ? catalog.evidenceSchema : [];
  const betaGates = Array.isArray(catalog?.betaGates) ? catalog.betaGates : [];
  const programs = Array.isArray(catalog?.programs) ? catalog.programs : [];
  if (catalog?.schemaVersion !== 1) {
    errors.push('Le catalogue de recherche doit utiliser schemaVersion 1.');
  }

  const statusPolicy = catalog?.statusPolicy;
  if (statusPolicy?.protocolStatus !== 'ready') {
    errors.push('La politique doit déclarer le protocole prêt.');
  }
  if (statusPolicy?.fieldEvidenceStatus !== 'pending') {
    errors.push('Les preuves terrain doivent rester pending tant qu’elles ne sont pas collectées.');
  }
  if (statusPolicy?.blocksDelivery !== false) {
    errors.push('Les observations terrain en attente ne doivent pas bloquer les livraisons indépendantes.');
  }
  if (statusPolicy?.allowGeneralizationWithoutEvidence !== false) {
    errors.push('La généralisation sans preuve terrain doit être interdite.');
  }
  if (!isNonEmptyText(statusPolicy?.statement)) {
    errors.push('La politique doit expliquer honnêtement le statut des preuves.');
  } else if (statusPolicy.statement !== requiredStatusPolicyStatement) {
    errors.push('La déclaration canonique du statut des preuves a été modifiée.');
  }

  hasOnlyRequiredIds(catalog?.canonicalProfiles, requiredProfileIds, 'Profils', errors);
  for (const profile of canonicalProfiles) {
    if (!isNonEmptyText(profile?.label)) {
      errors.push(`Profil ${profile?.id ?? '<sans-id>'}: libellé absent.`);
    }
    if (requiredProfileLabels[profile?.id]
      && profile.label !== requiredProfileLabels[profile.id]) {
      errors.push(`Profil ${profile.id}: libellé canonique modifié.`);
    }
  }

  hasOnlyRequiredIds(catalog?.commonTasks, requiredTaskIds, 'Tâches communes', errors);
  for (let index = 0; index < requiredTaskIds.length; index += 1) {
    if (commonTasks[index]?.id !== requiredTaskIds[index]) {
      errors.push(`Tâches communes: ${requiredTaskIds[index]} doit occuper la position ${index + 1}.`);
    }
  }
  for (const task of commonTasks) {
    if (!isNonEmptyText(task?.instruction) || !isNonEmptyText(task?.comparableMeasure)) {
      errors.push(`Tâche ${task?.id ?? '<sans-id>'}: instruction ou mesure comparable absente.`);
    }

    const canonicalSemantics = requiredTaskSemantics[task?.id];
    if (canonicalSemantics && task.instruction !== canonicalSemantics.instruction) {
      errors.push(`Tâche ${task.id}: instruction canonique modifiée.`);
    }
    if (canonicalSemantics && task.comparableMeasure !== canonicalSemantics.comparableMeasure) {
      errors.push(`Tâche ${task.id}: mesure comparable canonique modifiée.`);
    }
  }

  hasOnlyRequiredIds(catalog?.evidenceSchema, requiredEvidenceIds, 'Schéma de preuve', errors);
  for (const field of evidenceSchema) {
    if (typeof field?.required !== 'boolean' || !isNonEmptyText(field?.constraint)) {
      errors.push(`Preuve ${field?.id ?? '<sans-id>'}: obligation ou contrainte absente.`);
    }
    const expectedRequired = field?.id !== 'authorizedQuote';
    if (requiredEvidenceIds.includes(field?.id) && field?.required !== expectedRequired) {
      errors.push(`Preuve ${field.id}: required doit valoir ${expectedRequired}.`);
    }
    if (requiredEvidenceConstraints[field?.id]
      && field.constraint !== requiredEvidenceConstraints[field.id]) {
      errors.push(`Preuve ${field.id}: contrainte canonique modifiée.`);
    }
  }

  hasOnlyRequiredIds(catalog?.betaGates, requiredBetaGateIds, 'Gates bêta', errors);
  for (let index = 0; index < requiredBetaGateIds.length; index += 1) {
    if (betaGates[index]?.id !== requiredBetaGateIds[index]) {
      errors.push(`Gates bêta: ${requiredBetaGateIds[index]} doit occuper la position ${index + 1}.`);
    }
  }
  for (const gate of betaGates) {
    if (!Array.isArray(gate?.requirements)) {
      errors.push(`Gate ${gate?.id ?? '<sans-id>'}: exigences insuffisantes.`);
      continue;
    }
    if (gate.requirements.length < 3
      || gate.requirements.some((requirement) => !isNonEmptyText(requirement))) {
      errors.push(`Gate ${gate?.id ?? '<sans-id>'}: exigences insuffisantes.`);
    }

    const canonicalRequirements = requiredBetaGateRequirements[gate?.id];
    if (!canonicalRequirements) {
      continue;
    }
    if (gate.requirements.length !== canonicalRequirements.length) {
      errors.push(`Gate ${gate.id}: ${canonicalRequirements.length} exigences canoniques attendues.`);
    }
    for (let index = 0; index < canonicalRequirements.length; index += 1) {
      if (gate.requirements[index] !== canonicalRequirements[index]) {
        errors.push(`Gate ${gate.id}: exigence attendue en position ${index + 1}: ${canonicalRequirements[index]}.`);
      }
    }
  }

  hasOnlyRequiredIds(catalog?.programs, requiredProgramIds, 'Programmes', errors);
  const knownProfiles = new Set(requiredProfileIds);
  const roadmapPaths = new Set();
  const canonicalExtensionOwners = new Map();
  for (const [ownerProgramId, extensionPaths] of Object.entries(requiredExtensionDocumentsByProgram)) {
    for (const extensionPath of extensionPaths) {
      canonicalExtensionOwners.set(extensionPath, ownerProgramId);
    }
  }
  for (const program of programs ?? []) {
    const programId = program?.id ?? '<sans-id>';
    for (const field of ['gate', 'businessQuestion', 'firstSuccess']) {
      if (!isNonEmptyText(program?.[field])) {
        errors.push(`Programme ${programId}: champ ${field} absent.`);
      }
    }

    const canonicalDecision = requiredProgramDecisionsByProgram[programId];
    for (const field of ['businessQuestion', 'firstSuccess']) {
      if (canonicalDecision && program?.[field] !== canonicalDecision[field]) {
        errors.push(`Programme ${programId}: décision canonique modifiée ${field}.`);
      }
    }

    if (!isProductGrowthRoadmapPath(program?.roadmap)) {
      errors.push(`Programme ${programId}: roadmap hors du périmètre produit.`);
    } else if (roadmapPaths.has(program.roadmap)) {
      errors.push(`Programme ${programId}: roadmap déjà attribuée à un autre programme.`);
    } else {
      roadmapPaths.add(program.roadmap);
    }

    if (requiredRoadmapByProgram[programId] && program?.roadmap !== requiredRoadmapByProgram[programId]) {
      errors.push(`Programme ${programId}: roadmap canonique attendue ${requiredRoadmapByProgram[programId]}.`);
    }

    if (program?.gate !== `${programId}-G`) {
      errors.push(`Programme ${programId}: gate attendue ${programId}-G.`);
    }

    if (!Array.isArray(program?.profileIds) || program.profileIds.length < 3) {
      errors.push(`Programme ${programId}: au moins trois profils sont requis.`);
    } else {
      const profileIds = new Set(program.profileIds);
      if (profileIds.size !== program.profileIds.length) {
        errors.push(`Programme ${programId}: profil dupliqué.`);
      }
      for (const profileId of profileIds) {
        if (!knownProfiles.has(profileId)) {
          errors.push(`Programme ${programId}: profil inconnu ${profileId}.`);
        }
      }
      for (const accessibilityProfile of ['assistive-technology', 'modest-device-network']) {
        if (!profileIds.has(accessibilityProfile)) {
          errors.push(`Programme ${programId}: profil obligatoire absent ${accessibilityProfile}.`);
        }
      }
      for (const requiredProfileId of requiredProfileIdsByProgram[programId] ?? []) {
        if (!profileIds.has(requiredProfileId)) {
          errors.push(`Programme ${programId}: profil canonique absent ${requiredProfileId}.`);
        }
      }
    }

    for (const field of requiredTaskContextFields) {
      if (!isNonEmptyText(program?.taskContext?.[field])) {
        errors.push(`Programme ${programId}: contexte de tâche absent ${field}.`);
      }
      const canonicalContext = requiredTaskContextsByProgram[programId];
      if (canonicalContext && program?.taskContext?.[field] !== canonicalContext[field]) {
        errors.push(`Programme ${programId}: contexte canonique modifié ${field}.`);
      }
    }

    for (const [field, minimum] of [['stopConditions', 2], ['generalizationEvidence', 2]]) {
      if (!Array.isArray(program?.[field])
        || program[field].length < minimum
        || program[field].some((value) => !isNonEmptyText(value))) {
        errors.push(`Programme ${programId}: ${field} insuffisant.`);
      }
      if (canonicalDecision && !hasSameOrderedValues(program?.[field], canonicalDecision[field])) {
        errors.push(`Programme ${programId}: décision canonique modifiée ${field}.`);
      }
    }

    if (!Array.isArray(program?.extensionDocuments)) {
      errors.push(`Programme ${programId}: extensionDocuments doit être un tableau.`);
    } else {
      const extensionPaths = new Set();
      for (const extensionPath of program.extensionDocuments) {
        if (!isResearchExtensionPath(extensionPath)) {
          errors.push(`Programme ${programId}: extension hors du périmètre produit.`);
        } else if (extensionPaths.has(extensionPath)) {
          errors.push(`Programme ${programId}: extension dupliquée ${extensionPath}.`);
        } else {
          extensionPaths.add(extensionPath);
        }

        const canonicalOwner = canonicalExtensionOwners.get(extensionPath);
        if (canonicalOwner && canonicalOwner !== programId) {
          errors.push(`Programme ${programId}: extension canonique réservée à ${canonicalOwner}: ${extensionPath}.`);
        }
      }

      for (const requiredExtensionPath of requiredExtensionDocumentsByProgram[programId] ?? []) {
        if (!extensionPaths.has(requiredExtensionPath)) {
          errors.push(`Programme ${programId}: extension canonique absente ${requiredExtensionPath}.`);
        }
      }
    }
  }

  const outcomeConstraint = evidenceSchema.find((field) => field.id === 'outcome')?.constraint ?? '';
  for (const allowedOutcome of allowedOutcomeValues) {
    const outcomePattern = new RegExp(`(^|[^a-z-])${allowedOutcome}([^a-z-]|$)`, 'i');
    if (!outcomePattern.test(outcomeConstraint)) {
      errors.push(`Schéma de preuve: résultat obligatoire absent ${allowedOutcome}.`);
    }
  }

  return errors;
}

export async function validateResearchReadiness(catalogPath = defaultCatalogPath) {
  const catalog = JSON.parse(await readFile(catalogPath, 'utf8'));
  const errors = validateResearchCatalog(catalog);

  for (const program of Array.isArray(catalog.programs) ? catalog.programs : []) {
    const documentPaths = [program.roadmap, ...(program.extensionDocuments ?? [])];
    for (const documentPath of documentPaths) {
      if (!isProductGrowthRoadmapPath(documentPath) && !isResearchExtensionPath(documentPath)) {
        continue;
      }
      try {
        const content = await readFile(resolve(repositoryRoot, documentPath), 'utf8');
        errors.push(...validateResearchDocumentContent(documentPath, content));
      } catch (error) {
        errors.push(`Programme ${program.id}: document illisible ${documentPath} (${error instanceof Error ? error.code ?? error.message : 'erreur inconnue'}).`);
      }
    }
  }

  for (const documentPath of requiredProtocolDocumentPaths) {
    try {
      const content = await readFile(resolve(repositoryRoot, documentPath), 'utf8');
      errors.push(...validateResearchDocumentContent(documentPath, content));
    } catch (error) {
      errors.push(`Document QUAL-10 illisible ${documentPath} (${error instanceof Error ? error.code ?? error.message : 'erreur inconnue'}).`);
    }
  }

  return errors;
}

async function runCli() {
  const errors = await validateResearchReadiness();
  if (errors.length > 0) {
    process.stderr.write(`${errors.join('\n')}\n`);
    process.exitCode = 1;
    return;
  }

  process.stdout.write(`Préparation recherche validée: ${requiredProgramIds.length} programmes et ${requiredTaskIds.length} tâches comparables.\n`);
}

const invokedPath = process.argv[1] ? pathToFileURL(resolve(process.argv[1])).href : null;
if (invokedPath === import.meta.url) {
  await runCli();
}
