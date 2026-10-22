import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const defaultRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../docs/roadmaps/world-integration');
const states = new Set(['ToIntegrate', 'ToQualify', 'InProgress', 'Blocked', 'Integrated', 'PublishedDateUnknown', 'Excluded', 'Duplicate']);
const kinds = new Set(['Park', 'StandaloneAttraction', 'ToQualify']);
const presences = new Set(['Published', 'Hidden', 'NotFound', 'PossibleExisting', 'ExistsVisibilityUnknown']);
const isoUtc = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{3})?Z$/;
const labels = { ToIntegrate: 'À intégrer', ToQualify: 'À qualifier', InProgress: 'En cours', Blocked: 'Bloqué', Integrated: 'Intégration achevée', PublishedDateUnknown: 'Publié, date inconnue', Excluded: 'Hors périmètre enregistré', Duplicate: 'Doublon conservé' };

function requireText(value, label) {
  if (typeof value !== 'string' || !value.trim()) throw new Error(`${label} est obligatoire.`);
}

function instant(value, label) {
  if (!isoUtc.test(value ?? '') || !Number.isFinite(Date.parse(value)) || new Date(value).toISOString().replace('.000Z', 'Z') !== value.replace('.000Z', 'Z')) {
    throw new Error(`${label} doit être un instant UTC ISO 8601 valide.`);
  }
  return Date.parse(value);
}

export function validateCountry(document) {
  if (document.schemaVersion !== 1 || !/^[A-Z]{2}$/.test(document.countryCode)) throw new Error('En-tête de pays invalide.');
  if (!Array.isArray(document.entries)) throw new Error('entries doit être un tableau.');
  const ids = new Set();
  for (const entry of document.entries) {
    requireText(entry.entryId, 'entryId');
    if (ids.has(entry.entryId)) throw new Error(`Identifiant dupliqué : ${entry.entryId}`);
    ids.add(entry.entryId);
    requireText(entry.name, 'name');
    if (!states.has(entry.editorialStatus) || !kinds.has(entry.entityKind)) throw new Error(`État ou type invalide : ${entry.entryId}`);
    if (!presences.has(entry.presence) || !kinds.has(entry.proposedEntityKind)) throw new Error(`Présence ou type proposé invalide : ${entry.entryId}`);
    const knownPresence = ['Published', 'Hidden', 'ExistsVisibilityUnknown'].includes(entry.presence);
    if (knownPresence !== Boolean(entry.siteId)) throw new Error(`Présence et identifiant du site incohérents : ${entry.entryId}`);
    if (!Array.isArray(entry.integrationHistory) || !Array.isArray(entry.sources)) throw new Error(`Historique ou sources invalides : ${entry.entryId}`);
    const eventIds = new Set();
    let latest = null;
    for (const event of entry.integrationHistory) {
      requireText(event.eventId, 'eventId');
      requireText(event.evidence, 'preuve d’intégration');
      requireText(event.summary, 'résumé');
      if (eventIds.has(event.eventId)) throw new Error(`Événement dupliqué : ${event.eventId}`);
      eventIds.add(event.eventId);
      const applied = instant(event.appliedAt, 'appliedAt');
      if (latest && applied < Date.parse(latest)) throw new Error(`Historique non chronologique : ${entry.entryId}`);
      if (!['partial', 'completed'].includes(event.outcome)) throw new Error('Résultat d’intégration invalide.');
      if (event.outcome === 'completed') requireText(event.auditEvidence, 'preuve d’audit final');
      if (!latest || Date.parse(event.appliedAt) > Date.parse(latest)) latest = event.appliedAt;
    }
    if ((entry.lastDataIntegrationAt ?? null) !== latest) throw new Error(`La dernière intégration ne correspond pas à l’historique : ${entry.entryId}`);
    if (entry.editorialStatus === 'Integrated' && entry.integrationHistory.at(-1)?.outcome !== 'completed') throw new Error('Une intégration achevée exige un dernier audit enregistré.');
  }
  return document;
}

export function preserveHistory(previous, next) {
  validateCountry(previous);
  validateCountry(next);
  const entries = new Map(next.entries.map(entry => [entry.entryId, entry]));
  for (const old of previous.entries) {
    const current = entries.get(old.entryId);
    if (!current) throw new Error(`Suppression interdite : ${old.entryId}`);
    for (const [index, event] of old.integrationHistory.entries()) {
      const retained = current.integrationHistory[index];
      if (JSON.stringify(retained) !== JSON.stringify(event)) throw new Error(`Historique supprimé ou réécrit : ${old.entryId}/${event.eventId}`);
    }
  }
}

export function preserveRegistryHistory(previous, next) {
  for (const document of [...previous, ...next]) validateCountry(document);
  const countries = new Set(next.map(document => document.countryCode));
  for (const document of previous) {
    if (!countries.has(document.countryCode)) throw new Error(`Suppression de pays interdite : ${document.countryCode}`);
  }
  // Country corrections move an unchanged stable ID; retention applies worldwide.
  const combined = documents => ({ schemaVersion: 1, countryCode: 'ZZ', entries: documents.flatMap(document => document.entries) });
  preserveHistory(combined(previous), combined(next));
}

export function recordIntegration(document, options, now = new Date()) {
  validateCountry(document);
  const result = structuredClone(document);
  const entry = result.entries.find(item => item.entryId === options.entry);
  if (!entry) throw new Error('Entrée introuvable ; résoudre son identité avant intégration.');
  for (const key of ['event', 'evidence', 'summary', 'entityId']) requireText(options[key], key);
  const applied = instant(options.at, 'at');
  if (applied > now.getTime()) throw new Error('Une intégration future ne peut pas être enregistrée.');
  if (!['partial', 'completed'].includes(options.outcome)) throw new Error('outcome doit être partial ou completed.');
  if (!['Park', 'StandaloneAttraction'].includes(options.kind)) throw new Error('Le type doit être résolu avant de consigner une intégration.');
  if (options.outcome === 'completed') requireText(options.audit, 'audit');
  const event = { eventId: options.event, appliedAt: options.at, outcome: options.outcome, entityKind: options.kind, entityId: options.entityId, evidence: options.evidence, auditEvidence: options.audit ?? null, summary: options.summary };
  const previous = entry.integrationHistory.find(item => item.eventId === event.eventId);
  if (previous) {
    if (JSON.stringify(previous) !== JSON.stringify(event)) throw new Error('Cet eventId existe avec un contenu différent.');
    return result;
  }
  if (entry.lastDataIntegrationAt && applied < Date.parse(entry.lastDataIntegrationAt)) throw new Error('Une date plus ancienne ne peut pas remplacer la dernière intégration.');
  const targetChanged = entry.siteId !== options.entityId || entry.entityKind !== options.kind;
  entry.integrationHistory.push(event);
  entry.lastDataIntegrationAt = options.at;
  entry.entityKind = options.kind;
  entry.proposedEntityKind = options.kind;
  if (entry.siteId && entry.siteId !== options.entityId) entry.legacySiteIds = [...new Set([...(entry.legacySiteIds ?? []), entry.siteId])];
  entry.siteId = options.entityId;
  if (targetChanged || ['NotFound', 'PossibleExisting'].includes(entry.presence)) {
    entry.presence = 'ExistsVisibilityUnknown';
    entry.presenceObservedAt = null;
  }
  entry.editorialStatus = options.outcome === 'completed' ? 'Integrated' : 'InProgress';
  entry.nextAction = options.outcome === 'completed' ? 'Conserver la ligne ; suivre séparément la publication et les prochaines mises à jour.' : 'Poursuivre l’intégration et l’audit final.';
  if (options.outcome === 'completed') entry.assignedTo = null;
  preserveHistory(document, result);
  return result;
}

export function claimEntry(document, options) {
  validateCountry(document);
  const result = structuredClone(document);
  const entry = result.entries.find(item => item.entryId === options.entry);
  if (!entry) throw new Error('Entrée introuvable.');
  requireText(options.owner, 'owner');
  if (entry.assignedTo && entry.assignedTo !== options.owner) throw new Error(`Déjà attribué à ${entry.assignedTo}.`);
  if (['Excluded', 'Duplicate'].includes(entry.editorialStatus)) throw new Error('Qualifier ou réconcilier cette entrée avant de la réserver.');
  entry.assignedTo = options.owner;
  entry.editorialStatus = 'InProgress';
  return result;
}

function escapeCell(value) { return String(value ?? '—').replaceAll('|', '\\|').replaceAll('\n', ' '); }
function linkSource(source, index) { return `[${escapeCell(source.label || `Source ${index + 1}`)}](${source.url})`; }
const pageSize = 250;
function sortedEntries(document) { return [...document.entries].sort((a,b) => a.name.localeCompare(b.name, 'fr') || a.entryId.localeCompare(b.entryId)); }
export function countryCounts(document) {
  const count = predicate => document.entries.filter(predicate).length;
  const park = entry => entry.entityKind === 'Park';
  const standalone = entry => entry.entityKind === 'StandaloneAttraction';
  const live = entry => !['Excluded', 'Duplicate'].includes(entry.editorialStatus);
  const absent = entry => !entry.siteId && entry.presence === 'NotFound';
  const byId = new Map(document.entries.map(entry => [entry.entryId, entry]));
  return {
    parkPublished: count(entry => park(entry) && entry.presence === 'Published' && live(entry)),
    parkHidden: count(entry => park(entry) && entry.presence === 'Hidden' && entry.siteReviewStatusAtInventory !== 'NotRelevant' && entry.editorialStatus !== 'Integrated' && live(entry)),
    parkReview: count(entry => park(entry) && entry.presence === 'Hidden' && entry.siteReviewStatusAtInventory === 'NotRelevant' && !['Duplicate', 'Integrated'].includes(entry.editorialStatus)),
    parkAbsent: count(entry => absent(entry) && entry.proposedEntityKind === 'Park' && live(entry)),
    standalonePublished: count(entry => standalone(entry) && entry.presence === 'Published' && live(entry)),
    standaloneHidden: count(entry => standalone(entry) && entry.presence === 'Hidden' && entry.editorialStatus !== 'Integrated' && live(entry)),
    standaloneAbsent: count(entry => absent(entry) && entry.proposedEntityKind === 'StandaloneAttraction' && live(entry)),
    kindUnknown: count(entry => absent(entry) && entry.proposedEntityKind === 'ToQualify' && live(entry)),
    possibleExisting: count(entry => entry.presence === 'PossibleExisting' && live(entry)),
    legacy: count(entry => entry.editorialStatus === 'Duplicate' && entry.siteId && park(entry) && byId.get(entry.duplicateOf)?.entityKind === 'StandaloneAttraction'),
    integratedWithAudit: count(entry => entry.editorialStatus === 'Integrated'),
    integratedHidden: count(entry => entry.editorialStatus === 'Integrated' && entry.presence === 'Hidden'),
    visibilityUnknown: count(entry => entry.siteId && entry.presence === 'ExistsVisibilityUnknown' && live(entry)),
    countryResolvedBySource: count(entry => Boolean(entry.countryResolution)),
    total: document.entries.length,
  };
}

export function renderSummary(documents) {
  const sorted = [...documents].sort((a,b) => a.countryName.localeCompare(b.countryName, 'fr'));
  const groupNames = ['Europe', 'Pays transcontinentaux et Caucase', 'Asie', 'Afrique', 'Amérique du Nord, centrale et Caraïbes', 'Amérique du Sud', 'Océanie', 'Territoires antarctiques', 'Pays à déterminer'];
  const groupOf = document => document.geographicGroup ?? 'Europe';
  for (const document of sorted) if (!groupNames.includes(groupOf(document))) groupNames.push(groupOf(document));
  const grandTotal = sorted.reduce((sum,doc) => { for (const [key,value] of Object.entries(countryCounts(doc))) sum[key] += value; return sum; }, countryCounts({ entries: [] }));
  const lines = ['# Inventaire mondial et backlog permanent — résumé par pays', '',
    `Photographie initiale du 22 octobre 2026. **${sorted.filter(doc => doc.countryCode !== 'ZZ').length} pays et territoires**, plus la file des pays non résolus ; **${grandTotal.total} lignes conservées**. France et Portugal inclus. Les JSON nationaux sont le registre permanent ; les vues et compteurs sont régénérés par \`node tools/codex/integration-backlog.mjs render\`.`, '',
    '**Lecture des chiffres :** « publiés » répond au repère des intégrations déjà visibles sur le site. Ce recensement n’a pas réaudité leur complétude et ne prétend pas connaître leur dernière date d’intégration. Les fiches masquées ordinaires nécessitent une intégration éditoriale complète. Les fiches masquées `NotRelevant` sont comptées séparément : beaucoup sont des exploitants forains ou des anciennes identités ; une source de vrai lieu peut rouvrir leur examen.', '',
    '**Sans fiche = candidats sans correspondance trouvée dans le relevé**, pas créations validées. Confirmer les alias, l’adresse, le périmètre et le type avant création. Les rapprochements incertains et les types non résolus restent séparés. Les anciennes fiches déjà liées à une standalone ne gonflent pas la file des parcs.', '',
    'Le recensement couvre les annuaires et sources décrits dans [la méthode et ses limites](METHODOLOGY.md). La couverture varie selon le pays : la colonne « observations externes » compte les références de découverte avant dédoublonnage, jamais des parcs supplémentaires. **Un zéro ne signifie ni absence de parcs ni pays terminé** ; les recherches locales restantes sont signalées dans chaque fiche. [Règles et commandes de suivi](WORKFLOW.md).', '',
    `**Pays manquants dans la base :** ${grandTotal.countryResolvedBySource} fiches sont rattachées ici par rapprochement avec des sources, à confirmer pendant leur reprise. Ce classement documentaire ne modifie pas leur pays dans le site. Les cas non résolus restent dans [ZZ](countries/ZZ.md). Les compteurs européens corrigent donc la première photographie qui se limitait aux codes pays renseignés.`, '',
    '## Totaux mondiaux', '', '| Type | Publiés¹ | Fiches masquées à intégrer | Candidats sans fiche |', '| --- | ---: | ---: | ---: |',
    `| Parcs | ${grandTotal.parkPublished} | ${grandTotal.parkHidden} | ${grandTotal.parkAbsent} |`,
    `| Standalone | ${grandTotal.standalonePublished} | ${grandTotal.standaloneHidden} | ${grandTotal.standaloneAbsent} |`, '',
    `À part : **${grandTotal.parkReview}** Park masqués NotRelevant, **${grandTotal.kindUnknown}** types à décider, **${grandTotal.possibleExisting}** rapprochements incertains et **${grandTotal.legacy}** anciens parcs déjà liés à une standalone. Ces catégories ne sont pas additionnées au nombre de créations certaines.`, ''];
  for (const groupName of groupNames) {
    const group = sorted.filter(document => groupOf(document) === groupName);
    if (!group.length) continue;
    lines.push(`## ${groupName}`, '',
      ...(groupName === 'Pays transcontinentaux et Caucase' ? ['Périmètre national entier des sources. Cette rubrique conserve la distinction géographique introduite dans le rapport européen et évite tout double comptage entre continents.', ''] : []),
      '### Parcs', '', '| Pays / territoire | Publiés¹ | Masqués à intégrer | Masqués NotRelevant à réexaminer | Sans fiche : candidats parcs | Observations externes |', '| --- | ---: | ---: | ---: | ---: | ---: |');
    for (const doc of group) { const c = countryCounts(doc); lines.push(`| [${doc.countryName}](countries/${doc.countryCode}.md) | ${c.parkPublished} | ${c.parkHidden} | ${c.parkReview} | ${c.parkAbsent} | ${doc.sourceObservationCount ?? 0} |`); }
    const totals = group.reduce((sum,doc) => { for (const [key,value] of Object.entries(countryCounts(doc))) sum[key] = (sum[key] ?? 0) + value; return sum; }, countryCounts({ entries: [] }));
    lines.push(`| **Total** | **${totals.parkPublished}** | **${totals.parkHidden}** | **${totals.parkReview}** | **${totals.parkAbsent}** | **${group.reduce((sum,doc) => sum + (doc.sourceObservationCount ?? 0), 0)}** |`, '', '### Attractions standalone et dossiers à trancher', '',
      '| Pays | Standalone publiées¹ | Standalone masquées | Sans fiche : candidates standalone | Sans fiche : type à décider | Rapprochements incertains² | Anciens parcs liés à une standalone |', '| --- | ---: | ---: | ---: | ---: | ---: | ---: |');
    for (const doc of group) { const c = countryCounts(doc); lines.push(`| [${doc.countryName}](countries/${doc.countryCode}.md) | ${c.standalonePublished} | ${c.standaloneHidden} | ${c.standaloneAbsent} | ${c.kindUnknown} | ${c.possibleExisting} | ${c.legacy} |`); }
    lines.push(`| **Total** | **${totals.standalonePublished}** | **${totals.standaloneHidden}** | **${totals.standaloneAbsent}** | **${totals.kindUnknown}** | **${totals.possibleExisting}** | **${totals.legacy}** |`, '',
      `Intégrations achevées avec preuve et audit enregistrés depuis la création du registre : **${totals.integratedWithAudit}**. Fiches créées depuis le relevé dont la visibilité reste à contrôler : **${totals.visibilityUnknown}**.`, '');
    for (const doc of group.filter(item => countryCounts(item).integratedWithAudit)) {
      const counts = countryCounts(doc);
      lines.push(`- ${doc.countryName} : ${counts.integratedWithAudit} intégrations achevées et auditées, dont ${counts.integratedHidden} fiches encore masquées.`);
    }
    lines.push('');
  }
  lines.push('¹ Publié ne signifie pas « audité et complet aujourd’hui ». Date historique inconnue tant qu’aucune preuve datée n’est retrouvée ; la date du recensement ne la remplace pas.', '',
    '² Ces dossiers peuvent désigner une fiche déjà présente ou un alias d’un candidat, notamment pour les noms japonais non encore rapprochés de leurs noms latins, ou une attraction dont le rattachement à un vrai parc reste à décider. Ils sont exclus des colonnes « sans fiche » pour éviter de présenter des doublons possibles comme de nouvelles créations.', '',
    '## Relais France et Portugal', '',
    'Les campagnes nationales doivent relire [FR](countries/FR.md) et [PT](countries/PT.md), rapprocher leurs derniers résultats du relevé, puis réserver les entrées par conversation ou branche avant le prochain lot. Les consignes de leurs anciens backlogs et de `AGENTS.md` renvoient vers ce registre. Il n’existe pas ici de canal de notification directe d’une autre conversation ChatGPT : ce relais versionné doit être repris dans ces conversations.', '',
    'Une intégration achevée reste dans le registre avec son identifiant stable, sa date UTC réelle, sa preuve et son audit. Chaque intégration partielle ultérieure ajoute un événement ; aucune ligne ni aucun événement antérieur n’est effacé.', '');
  return `${lines.join('\n').trimEnd()}\n`;
}

export function renderCountry(document) {
  validateCountry(document);
  const lines = [`# ${document.countryName} — backlog permanent`, '', `Relevé initial : ${document.inventoryDate}. Périmètre : ${document.geographicScope}.`, '', 'Source de vérité : le fichier JSON du même pays. Ne jamais supprimer une entrée achevée ; utiliser le script de suivi pour enregistrer les intégrations et régénérer cette vue.', '', ...(document.notes ?? []).map(note => `- ${note}`), '', '| État éditorial | Nombre |', '| --- | ---: |'];
  for (const [state, label] of Object.entries(labels)) lines.push(`| ${label} | ${document.entries.filter(entry => entry.editorialStatus === state).length} |`);
  const c = countryCounts(document);
  lines.push('', `Couverture : **${escapeCell(document.coverageStatus ?? 'À documenter')}**. Observations externes : **${document.sourceObservationCount ?? 0}**. Sources : ${escapeCell((document.sourceFamilies ?? []).join(', ') || 'aucune liste nationale collectée')}.`, '',
    `Pays proposé par rapprochement pour **${c.countryResolvedBySource}** fiches du site sans code pays au relevé ; confirmation à effectuer avant modification de production.`);
  lines.push('', `Parcs : **${c.parkPublished} publiés**, **${c.parkHidden} masqués à intégrer**, **${c.parkReview} masqués NotRelevant à réexaminer**, **${c.parkAbsent} candidats sans fiche**.`, '',
    `Standalone : **${c.standalonePublished} publiées**, **${c.standaloneHidden} masquées**, **${c.standaloneAbsent} candidates sans fiche**. Autres dossiers : **${c.kindUnknown} types à décider**, **${c.possibleExisting} rapprochements incertains**, **${c.legacy} anciens parcs liés à une standalone**.`, '',
    'Chaque candidat passe par une qualification avant création. Le type proposé ne prouve pas l’autonomie d’une attraction. Les signaux de fermeture ou d’exploitation ci-dessous sont ceux des sources, à dater et à vérifier.', '');
  const pages = renderCountryPages(document);
  if (Object.keys(pages).length) {
    lines.push('## Liste intégrale, par ordre alphabétique', '', 'La liste est paginée pour rester lisible dans GitHub. Toutes les lignes restent dans le JSON national.', '');
    for (const [index, name] of Object.keys(pages).entries()) lines.push(`- [Page ${index + 1} — lignes ${index * pageSize + 1} à ${Math.min((index + 1) * pageSize, document.entries.length)}](${document.countryCode}/${name})`);
  } else lines.push(entryTable(sortedEntries(document)));
  return `${lines.join('\n').trimEnd()}\n`;
}

function entryTable(entries) {
  const lines = ['| ID stable | Lieu | Localité | Type / proposition | Présence relevée | État | Dernière intégration UTC | Affectation | Suite / sources |', '| --- | --- | --- | --- | --- | --- | --- | --- | --- |'];
  for (const entry of entries) {
    const sources = entry.sources.map(linkSource).join(' · ');
    const details = [entry.nextAction, entry.sourceLifecycleSignal && entry.sourceLifecycleSignal !== 'Non vérifié' ? `Signal source : ${entry.sourceLifecycleSignal}.` : '', ...(entry.possibleMatches ?? []).map(match => `Correspondance possible : ${match.name} (${match.siteId}).`), entry.duplicateOf ? `Cible : ${entry.duplicateOf}.` : ''].filter(Boolean).join(' ');
    lines.push(`| ${escapeCell(entry.entryId)} | ${escapeCell(entry.name)} | ${escapeCell(entry.locality)} | ${escapeCell(entry.entityKind === 'ToQualify' ? `À qualifier ; proposé : ${entry.proposedEntityKind}` : entry.entityKind)} | ${escapeCell(entry.presence)} | ${labels[entry.editorialStatus]} | ${entry.lastDataIntegrationAt ?? 'Inconnue — non inventée'} | ${escapeCell(entry.assignedTo)} | ${escapeCell(details)} ${sources} |`);
  }
  return `${lines.join('\n')}\n`;
}

export function renderCountryPages(document) {
  if (document.entries.length <= pageSize) return {};
  const entries = sortedEntries(document);
  const pages = {};
  for (let offset = 0; offset < entries.length; offset += pageSize) {
    const number = offset / pageSize + 1;
    pages[`${String(number).padStart(3, '0')}.md`] = `# ${document.countryName} — page ${number}\n\n[Résumé national et règles de lecture](../${document.countryCode}.md). Lignes ${offset + 1} à ${Math.min(offset + pageSize, entries.length)} sur ${entries.length}.\n\n${entryTable(entries.slice(offset, offset + pageSize))}`;
  }
  return pages;
}

function atomicWrite(filename, text) {
  const temporary = `${filename}.${process.pid}.tmp`;
  try { fs.writeFileSync(temporary, text, { flag: 'wx' }); fs.renameSync(temporary, filename); }
  finally { if (fs.existsSync(temporary)) fs.unlinkSync(temporary); }
}

function writeCountryViews(filename, document) {
  atomicWrite(filename.replace(/\.json$/, '.md'), renderCountry(document));
  const pages = renderCountryPages(document);
  const directory = filename.replace(/\.json$/, '');
  if (Object.keys(pages).length) fs.mkdirSync(directory, { recursive: true });
  for (const [name, content] of Object.entries(pages)) atomicWrite(path.join(directory, name), content);
}

function readCountry(root, country) {
  if (!/^[A-Z]{2}$/.test(country ?? '')) throw new Error('country doit être un code de deux lettres.');
  const filename = path.join(root, 'countries', `${country}.json`);
  return { filename, document: validateCountry(JSON.parse(fs.readFileSync(filename, 'utf8'))) };
}

export function run(args) {
  const [command, ...rest] = args;
  const options = {};
  for (let index = 0; index < rest.length; index += 2) {
    if (!rest[index].startsWith('--') || rest[index + 1] === undefined) throw new Error('Arguments attendus : --clé valeur.');
    const key = rest[index].slice(2).replace(/-([a-z])/g, (_, letter) => letter.toUpperCase());
    if (key in options) throw new Error(`Argument répété : ${key}`);
    options[key] = rest[index + 1];
  }
  const root = options.root ? path.resolve(options.root) : defaultRoot;
  if (command === 'record' || command === 'claim') {
    const { filename, document } = readCountry(root, options.country);
    const lock = path.join(root, '.write.lock');
    const descriptor = fs.openSync(lock, 'wx');
    try {
      // Read again after acquiring the lock so another writer cannot be overwritten.
      const current = JSON.parse(fs.readFileSync(filename, 'utf8'));
      const next = command === 'record' ? recordIntegration(current, options) : claimEntry(current, options);
      preserveHistory(document, next);
      atomicWrite(filename, `${JSON.stringify(next, null, 2)}\n`);
      writeCountryViews(filename, next);
      const documents = fs.readdirSync(path.join(root, 'countries')).filter(file => file.endsWith('.json')).map(file => JSON.parse(fs.readFileSync(path.join(root, 'countries', file), 'utf8')));
      atomicWrite(path.join(root, 'README.md'), renderSummary(documents));
    } finally { fs.closeSync(descriptor); fs.unlinkSync(lock); }
    console.log(`${options.country}/${options.entry} mis à jour ; toutes les entrées sont conservées.`);
    return;
  }
  if (!['validate', 'render'].includes(command)) throw new Error('Commandes : validate, render, claim, record.');
  const files = fs.readdirSync(path.join(root, 'countries')).filter(file => file.endsWith('.json')).sort();
  const allIds = new Set();
  const documents = [];
  for (const file of files) {
    const filename = path.join(root, 'countries', file);
    const document = validateCountry(JSON.parse(fs.readFileSync(filename, 'utf8')));
    documents.push(document);
    if (document.countryCode !== file.slice(0, -5)) throw new Error(`Pays incohérent : ${file}`);
    for (const entry of document.entries) {
      if (allIds.has(entry.entryId)) throw new Error(`Identifiant global dupliqué : ${entry.entryId}`);
      allIds.add(entry.entryId);
    }
    const rendered = renderCountry(document);
    if (command === 'render') writeCountryViews(filename, document);
    else if (fs.readFileSync(filename.replace(/\.json$/, '.md'), 'utf8') !== rendered) throw new Error(`Vue à régénérer : ${file}`);
    if (command === 'validate') for (const [name, content] of Object.entries(renderCountryPages(document))) {
      if (fs.readFileSync(path.join(filename.replace(/\.json$/, ''), name), 'utf8') !== content) throw new Error(`Page à régénérer : ${file}/${name}`);
    }
  }
  const summary = renderSummary(documents);
  if (command === 'render') atomicWrite(path.join(root, 'README.md'), summary);
  else if (fs.readFileSync(path.join(root, 'README.md'), 'utf8') !== summary) throw new Error('Résumé par pays à régénérer.');
  if (options.baseline) {
    const directory = path.join(options.baseline, 'countries');
    const previous = fs.readdirSync(directory).filter(file => file.endsWith('.json')).map(file => JSON.parse(fs.readFileSync(path.join(directory, file), 'utf8')));
    preserveRegistryHistory(previous, documents);
  }
  console.log(`${files.length} pays, ${allIds.size} entrées : ${command} OK.`);
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try { run(process.argv.slice(2)); }
  catch (error) { console.error(error.message); process.exitCode = 1; }
}
