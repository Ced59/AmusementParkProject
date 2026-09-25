import { spawn, spawnSync } from 'node:child_process';
import { once } from 'node:events';
import { existsSync } from 'node:fs';
import { mkdtemp, rm, writeFile } from 'node:fs/promises';
import { createServer } from 'node:net';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

import { compile } from 'sass';

const viewportWidths = [320, 360, 390, 768, 1280];
const projectRoot = process.cwd();
const componentStyles = [
  'src/app/features/profile/trips/pages/trip-list-page/trip-list-page.component.scss',
  'src/app/features/profile/trips/pages/trip-overview-page/trip-overview-page.component.scss',
  'src/app/features/profile/trips/components/trip-candidate-card/trip-candidate-card.component.scss',
  'src/app/features/profile/trips/components/trip-invitation-panel/trip-invitation-panel.component.scss',
  'src/app/features/profile/trips/pages/trip-preferences-page/trip-preferences-page.component.scss',
  'src/app/features/profile/trips/pages/trip-preference-summary-page/trip-preference-summary-page.component.scss',
  'src/app/features/profile/trips/pages/trip-program-coherence-page/trip-program-coherence-page.component.scss',
  'src/app/features/profile/trips/pages/trip-activity-page/trip-activity-page.component.scss',
  'src/app/features/profile/trips/pages/trip-export-page/trip-export-page.component.scss',
  'src/app/features/public/trips/pages/trip-invitation-preview-page/trip-invitation-preview-page.component.scss'
]
  .map((relativePath) => compile(resolve(projectRoot, relativePath)).css)
  .join('\n')
  .replaceAll(':host', '.responsive-fixture-host');

const chromeCandidates = [
  process.env.CHROME_PATH,
  process.env.GOOGLE_CHROME_BIN,
  process.env.PROGRAMFILES && join(process.env.PROGRAMFILES, 'Google/Chrome/Application/chrome.exe'),
  process.env['PROGRAMFILES(X86)'] && join(process.env['PROGRAMFILES(X86)'], 'Google/Chrome/Application/chrome.exe'),
  process.env.LOCALAPPDATA && join(process.env.LOCALAPPDATA, 'Google/Chrome/Application/chrome.exe'),
  'google-chrome-stable',
  'google-chrome',
  'chromium',
  'chromium-browser'
].filter(Boolean);

const chromeExecutable = chromeCandidates.find((candidate) => {
  if (candidate.includes('/') || candidate.includes('\\')) {
    return existsSync(candidate);
  }

  return spawnSync(candidate, ['--version'], { encoding: 'utf8' }).status === 0;
});

if (!chromeExecutable) {
  throw new Error('A Chromium or Google Chrome executable is required for the trip responsive check.');
}

const fixtureMarkup = `
  <div class="responsive-fixture-host">
    <main class="trip-list-page" data-responsive-root="trip-list">
      <header class="trip-list-page__hero surface" data-check-bound>
        <a href="#">Retour vers le profil du membre</a>
        <h1>Prépare ton prochain voyage dans les parcs</h1>
        <p>Un titre volontairement long vérifie que les textes localisés restent contenus dans le viewport.</p>
        <div class="trip-list-page__hero-actions"><button>Créer un nouveau voyage privé</button></div>
      </header>
      <section class="trip-composer surface" data-check-bound>
        <div><h2>Commence ton programme</h2><p>Choisis les premières informations sans rien publier.</p></div>
        <label><span>Titre du voyage</span><input value="Un très long voyage entre plusieurs destinations"></label>
        <div class="trip-composer__dates">
          <label><span>Date de début</span><input type="date" value="2026-10-03"></label>
          <label><span>Date de fin</span><input type="date" value="2026-10-09"></label>
        </div>
        <p class="trip-composer__hint">Fuseau de la destination : Europe/Paris</p>
        <button>Commencer la préparation du voyage</button>
      </section>
      <section class="trip-list-page__grid" data-check-bound>
        <article class="trip-card surface"><h2>Un voyage au titre particulièrement long</h2><p class="trip-card__dates">3 octobre 2026 → 9 octobre 2026</p><button>Continuer la préparation</button></article>
        <article class="trip-card surface"><h2>Voyage sans dates</h2><p class="trip-card__dates">Dates à décider ensemble</p><button>Continuer la préparation</button></article>
      </section>
    </main>

    <main class="trip-overview-page" data-responsive-root="trip-overview">
      <header class="trip-overview-page__hero surface" data-check-bound>
        <a href="#">Tous mes voyages</a>
        <h1>Une aventure européenne avec un titre très long</h1>
        <p>Programme privé · aucune donnée n'est publiée.</p>
      </header>
      <section class="trip-section surface" data-check-bound>
        <div class="trip-section__heading"><span class="trip-section__step">1</span><div><h2>Cadre du voyage</h2><p>Les dates et le fuseau organisent les journées.</p></div></div>
        <div class="trip-dates__fields">
          <label><span>Début</span><input type="date" value="2026-10-03"></label>
          <label><span>Fin</span><input type="date" value="2026-10-09"></label>
          <label><span>Fuseau</span><input value="Europe/Paris"></label>
          <div class="trip-dates__actions"><button>Enregistrer les dates</button><button>Retirer les dates</button></div>
        </div>
      </section>
      <section class="trip-section surface" data-check-bound>
        <div class="trip-section__heading"><span class="trip-section__step">2</span><div><h2>Idées enregistrées</h2><p>Le carrousel reste défilable sans agrandir la page.</p></div></div>
        <div class="wishlist-strip">
          <button class="wishlist-card"><span class="wishlist-card__visual">Image</span><strong>Parc au nom très long</strong><span>Ajouter au voyage</span></button>
          <button class="wishlist-card"><span class="wishlist-card__visual">Image</span><strong>Deuxième destination</strong><span>Ajouter au voyage</span></button>
          <button class="wishlist-card"><span class="wishlist-card__visual">Image</span><strong>Troisième destination</strong><span>Ajouter au voyage</span></button>
        </div>
      </section>
      <section class="trip-section surface" data-check-bound>
        <div class="trip-section__heading"><span class="trip-section__step">3</span><div><h2>Parcs candidats</h2><p>Le glisser-déposer conserve ses commandes accessibles.</p></div></div>
        <div class="candidate-board">
          <app-trip-candidate-card class="responsive-fixture-host" data-check-bound>
            <article class="candidate-card surface">
              <div class="candidate-card__visual"><span class="candidate-card__placeholder">Image</span></div>
              <div class="candidate-card__body">
                <div class="candidate-card__heading"><button class="candidate-card__drag">↕</button><div><h3>Destination au libellé extrêmement long</h3><p>Ajoutée depuis la liste d'envies</p></div><app-ui-chip>Choisi</app-ui-chip></div>
                <div class="candidate-card__states"><button>Idée</button><button>Présélection</button><button>Choisi</button><button>Écarté</button></div>
                <div class="candidate-card__move"><button>Premier</button><button>Monter</button><button>Descendre</button><button>Dernier</button></div>
              </div>
            </article>
          </app-trip-candidate-card>
        </div>
      </section>
      <section class="trip-section surface" data-check-bound>
        <div class="trip-section__heading"><span class="trip-section__step">4</span><div><h2>Journées</h2><p>Chaque contrôle doit rester entièrement utilisable.</p></div></div>
        <div class="day-board">
          <article class="day-card"><div class="day-card__date"><strong>3 octobre 2026</strong></div><label><span>Parc</span><select><option>Parc au nom très long</option></select></label><label><span>Arrivée</span><input type="time" value="09:00"></label><label class="day-card__note"><span>Note du groupe</span><textarea>Rendez-vous devant l'entrée principale.</textarea></label><div class="day-card__actions"><button>Enregistrer la journée</button><button>Vider la journée</button></div></article>
          <article class="day-card"><div class="day-card__date"><strong>4 octobre 2026</strong></div><label><span>Parc</span><select><option>Deuxième destination</option></select></label><label><span>Arrivée</span><input type="time" value="10:00"></label><label class="day-card__note"><span>Note du groupe</span><textarea>Une autre note représentative.</textarea></label><div class="day-card__actions"><button>Enregistrer la journée</button><button>Vider la journée</button></div></article>
        </div>
      </section>
      <section class="invitation-panel surface" data-check-bound>
        <div class="invitation-panel__heading"><span class="invitation-panel__step">5</span><div><h2>Invite tes compagnons de voyage</h2><p>Le formulaire et son aperçu restent contenus.</p></div></div>
        <div class="invitation-panel__workspace">
          <form class="invitation-panel__form">
            <fieldset><legend>Que pourra faire cette personne ?</legend><div class="invitation-panel__roles"><button class="invitation-role"><i>✎</i><strong>Co-organisateur</strong><span>Modifie le programme avec toi.</span></button><button class="invitation-role invitation-role--active"><i>★</i><strong>Participant</strong><span>Participe aux choix.</span></button><button class="invitation-role"><i>◉</i><strong>Lecteur</strong><span>Consulte sans modifier.</span></button></div></fieldset>
            <div class="invitation-panel__fields"><label><span>Durée de validité</span><select><option>7 jours</option></select></label><label><span>Adresse du destinataire facultative</span><input value="compagnon@example.com"></label></div>
            <aside class="invitation-preview"><h3>Aperçu avant envoi</h3><dl><div><dt>Rôle proposé</dt><dd>Participant</dd></div><div><dt>Période approximative</dt><dd>2027-07 → 2027-08</dd></div></dl><p>Les détails privés restent invisibles.</p></aside>
            <button>Créer le lien sécurisé</button>
          </form>
          <div class="invitation-created"><h3>Partage-le maintenant</h3><a class="invitation-created__link">https://amusement-parks.fun/fr/trip-invitations/un-tres-long-token-opaque</a><div class="invitation-created__actions"><button>Copier le lien</button><button>Masquer</button></div></div>
        </div>
      </section>
    </main>

    <main class="trip-preferences-page" data-responsive-root="trip-preferences">
      <header class="trip-preferences-hero surface" data-check-bound>
        <a href="#">Retour au voyage</a>
        <h1>Une aventure européenne avec un titre très long</h1>
        <p>Dis ce que tu veux vraiment faire, sans agrandir la page sur mobile.</p>
        <div class="preference-summary"><span>12 indispensables</span><span>18 souhaitées</span><span>7 à décider</span></div>
      </header>
      <section class="preference-filters surface" data-check-bound>
        <label class="preference-search"><span>Rechercher une attraction</span><span class="preference-input-shell"><i>⌕</i><input value="Une attraction au nom particulièrement long"></span></label>
        <label><span>Parc</span><select><option>Destination au nom particulièrement long</option></select></label>
        <label><span>Préférence</span><select><option>Toutes les préférences</option></select></label>
      </section>
      <div class="preference-groups" data-check-bound>
        <section class="preference-group">
          <div class="preference-group__heading"><i>⌖</i><h2>Destination au nom particulièrement long</h2><span>2</span></div>
          <div class="preference-grid">
            <article class="preference-card surface" data-check-bound>
              <div class="preference-card__identity"><span class="preference-card__image">Image</span><div><h3>Attraction au nom extrêmement long</h3><span class="preference-card__current">Indispensable</span></div></div>
              <div class="preference-picker"><button class="preference-choice">Indispensable</button><button class="preference-choice">Souhaitée</button><button class="preference-choice">Facultative</button><button class="preference-choice">Pas pour moi</button><button class="preference-choice">À décider</button></div>
              <label class="preference-reason"><span>Pourquoi ce choix ?</span><select><option>Sensations trop intenses</option></select></label>
            </article>
            <article class="preference-card surface" data-check-bound>
              <div class="preference-card__identity"><span class="preference-card__image">Image</span><div><h3>Deuxième attraction représentative</h3><span class="preference-card__current">Souhaitée</span></div></div>
              <div class="preference-picker"><button class="preference-choice">Indispensable</button><button class="preference-choice">Souhaitée</button><button class="preference-choice">Facultative</button><button class="preference-choice">Pas pour moi</button><button class="preference-choice">À décider</button></div>
            </article>
          </div>
        </section>
      </div>
      <aside class="preference-savebar" data-check-bound><div><strong>2 changements</strong><span>Enregistre-les avant de partir.</span></div><div class="preference-savebar__actions"><button>Annuler</button><button>Enregistrer les choix</button></div></aside>
    </main>

    <main class="trip-summary-page" data-responsive-root="trip-preference-summary">
      <header class="trip-summary-hero surface" data-check-bound>
        <a href="#">Retour au voyage</a>
        <h1>Une aventure européenne avec un titre très long</h1>
        <p>La boussole du groupe garde chaque opposition visible sans exposer les choix individuels.</p>
        <div class="trip-summary-hero__facts"><span>8 participants</span><span>12 points à discuter</span><span>4 accords</span></div>
        <p class="trip-summary-hero__promise">Une opposition reste visible même lorsqu'une majorité souhaite faire l'attraction.</p>
      </header>
      <section class="trip-summary-tools surface" data-check-bound>
        <label class="trip-summary-search"><span>Rechercher</span><span class="trip-summary-search__field"><i>⌕</i><input value="Une attraction au nom particulièrement long"></span></label>
        <div class="trip-summary-filters"><button class="trip-summary-filter">Tout</button><button class="trip-summary-filter">À discuter <strong>12</strong></button><button class="trip-summary-filter">Avis partagés <strong>7</strong></button><button class="trip-summary-filter">À compléter <strong>9</strong></button></div>
      </section>
      <section class="trip-summary-board" data-check-bound>
        <article class="trip-summary-card trip-summary-card--conflict surface" data-check-bound>
          <div class="trip-summary-card__identity"><span class="trip-summary-card__image">Image</span><div><span class="trip-summary-card__park">Destination au nom particulièrement long</span><h2>Attraction au nom extrêmement long</h2></div><span class="trip-summary-card__compatibility">Point de friction</span></div>
          <div class="trip-summary-counts"><span class="trip-summary-count"><strong>3</strong><small>Incontournable</small></span><span class="trip-summary-count"><strong>2</strong><small>Envie</small></span><span class="trip-summary-count"><strong>1</strong><small>Pourquoi pas</small></span><span class="trip-summary-count"><strong>1</strong><small>Pas pour moi</small></span><span class="trip-summary-count"><strong>1</strong><small>Sans réponse</small></span></div>
          <div class="trip-summary-signals"><p class="trip-summary-signal trip-summary-signal--conflict">Une envie et une opposition coexistent : la majorité ne tranche pas.</p></div>
          <details class="trip-summary-official" open><summary>État officiel et preuve</summary><div><p><strong>État</strong><span>En activité</span></p><a>Consulter une source au libellé long</a></div></details>
          <section class="trip-summary-decision"><span class="trip-summary-decision__icon">✓</span><div><small>Décision du groupe</small><strong>Groupe séparé</strong><p>Une partie du groupe fera cette attraction pendant une pause adaptée aux autres participants.</p><span>Décidé par PseudonymeTrèsLong</span></div></section>
          <details class="trip-summary-editor" open><summary>Faire évoluer la décision</summary><div class="trip-summary-editor__body"><div class="trip-summary-decision-picker"><button>À rediscuter</button><button>Retenue</button><button>Groupe séparé</button><button>Optionnelle</button><button>Écartée</button></div><label><span>Pourquoi ce choix ?</span><textarea>Un compromis explicite et suffisamment détaillé.</textarea><small>La raison reste visible.</small></label><button>Enregistrer la décision</button></div></details>
        </article>
      </section>
    </main>

    <main class="trip-coherence-page" data-responsive-root="trip-program-coherence">
      <header class="trip-coherence-hero surface" data-check-bound>
        <a href="#">Retour au voyage</a>
        <h1>Une aventure européenne avec un titre très long</h1>
        <p>Compare le programme aux faits officiels disponibles et repère ce que le groupe doit revoir.</p>
        <div class="trip-coherence-hero__counts"><span>2 alertes critiques</span><span>3 points à vérifier</span><span>1 mise à jour</span></div>
        <p class="trip-coherence-hero__promise">Aucune alerte ne change le programme automatiquement : le groupe garde toujours la décision.</p>
      </header>
      <section class="trip-coherence-section surface" data-check-bound>
        <div class="trip-coherence-section__heading"><h2>Les points qui demandent ton attention</h2><p>Chaque alerte relie un choix du groupe à un fait actuel.</p></div>
        <div class="trip-coherence-issues">
          <article class="trip-coherence-issue" data-severity="Critical" data-check-bound><div class="trip-coherence-issue__icon">!</div><div><span class="trip-coherence-issue__severity">À corriger</span><h3>Une attraction au nom volontairement très long est maintenant indiquée fermée.</h3><p>État officiel : fermeture temporaire</p><a href="#">Consulter la source officielle</a></div></article>
          <article class="trip-coherence-issue" data-severity="Attention" data-check-bound><div class="trip-coherence-issue__icon">i</div><div><span class="trip-coherence-issue__severity">À vérifier</span><h3>Les horaires de cette destination au nom particulièrement long n'ont pas été vérifiés récemment.</h3><p>Vérifié le 18 septembre 2026</p><a href="#">Consulter la source officielle</a></div></article>
        </div>
      </section>
      <section class="trip-coherence-section surface" data-check-bound>
        <div class="trip-coherence-section__heading"><h2>Ouverture jour par jour</h2><p>L'état officiel reste séparé du planning.</p></div>
        <div class="trip-coherence-days">
          <article class="trip-coherence-day" data-check-bound><div class="trip-coherence-day__date"><strong>3</strong><span>oct.</span></div><div class="trip-coherence-day__body"><h3>Destination au nom particulièrement long</h3><div class="trip-coherence-day__facts"><span>Ouverture confirmée</span><span>En activité</span><span>18 septembre 2026</span></div><a href="#">Consulter la source officielle</a></div></article>
          <article class="trip-coherence-day" data-check-bound><div class="trip-coherence-day__date"><strong>4</strong><span>oct.</span></div><div class="trip-coherence-day__body"><h3>Deuxième destination représentative</h3><div class="trip-coherence-day__facts"><span>Horaires inconnus</span><span>En activité</span></div><small>Aucune source d'horaires n'est disponible.</small></div></article>
        </div>
      </section>
      <section class="trip-coherence-section surface" data-check-bound>
        <div class="trip-coherence-section__heading"><h2>Distances entre les étapes</h2><p>Les liaisons connues sont affichées entre deux parcs consécutifs.</p></div>
        <div class="trip-coherence-travel"><article class="trip-coherence-route" data-check-bound><div class="trip-coherence-route__parks"><strong>Destination au nom particulièrement long</strong><span>→</span><strong>Deuxième destination représentative</strong></div><p>Environ 245,6 km · 211 min</p><small>Estimation géographique indicative, pas un itinéraire routier en temps réel.</small></article></div>
      </section>
    </main>

    <main class="trip-activity-page" data-responsive-root="trip-activity">
      <header class="trip-activity-hero surface" data-check-bound>
        <a href="#">Retour au voyage</a>
        <h1>Une aventure européenne avec un titre très long</h1>
        <p>Retrouve les décisions et changements importants sans exposer les préférences privées.</p>
        <div class="trip-activity-hero__actions"><button>Actualiser l’historique</button></div>
      </header>
      <section class="trip-activity-feed surface" data-check-bound>
        <div class="trip-activity-feed__heading"><h2>Ce qui a changé</h2><p>Les changements les plus récents apparaissent en premier.</p></div>
        <ol class="trip-activity-list">
          <li class="trip-activity-entry" data-check-bound><span class="trip-activity-entry__icon">✎</span><div class="trip-activity-entry__body"><strong>Modification du titre · UnPseudonymeVolontairementTrèsLongPourLeMobile</strong><time>18 septembre 2026 à 14:30</time></div></li>
          <li class="trip-activity-entry" data-check-bound><span class="trip-activity-entry__icon">♥</span><div class="trip-activity-entry__body"><strong>Mise à jour de 250 préférences · Toi</strong><time>18 septembre 2026 à 14:22</time></div></li>
        </ol>
        <button>Charger les changements précédents</button>
      </section>
    </main>

    <main class="trip-export-page" data-responsive-root="trip-export">
      <header class="trip-export-hero surface" data-check-bound>
        <a href="#">Retour au voyage</a>
        <h1>Une aventure européenne avec un titre volontairement très long</h1>
        <p>Un programme privé, transportable et lisible sur tous les écrans.</p>
        <div class="trip-export-hero__legend"><span>Réservé aux participants</span><span>Préparé aujourd’hui</span></div>
        <div class="trip-export-hero__actions"><button>Imprimer ou enregistrer en PDF</button><button>Télécharger le JSON</button></div>
      </header>
      <section class="trip-export-section surface" data-check-bound>
        <div class="trip-export-section__heading"><span class="trip-export-section__step">2</span><div><h2>Les parcs envisagés</h2><p>Les choix retenus et encore en discussion.</p></div></div>
        <div class="trip-export-grid">
          <article class="trip-export-card" data-check-bound><div class="trip-export-card__title"><i>⌖</i><h3>Destination au nom exceptionnellement long pour vérifier le retour à la ligne</h3></div><span>Sélectionné</span><p>Samedi 20 août 2027 · Dimanche 21 août 2027</p><p>Une note collective très détaillée qui doit rester entièrement dans la carte.</p></article>
          <article class="trip-export-card" data-check-bound><div class="trip-export-card__title"><i>⌖</i><h3>Deuxième destination</h3></div><span>À départager</span></article>
        </div>
      </section>
      <section class="trip-export-section surface" data-check-bound>
        <div class="trip-export-section__heading"><span class="trip-export-section__step">3</span><div><h2>Le programme</h2><p>Les rendez-vous partagés, sans données privées.</p></div></div>
        <div class="trip-export-days"><article class="trip-export-day" data-check-bound><div class="trip-export-day__heading"><div><time>Samedi 20 août 2027</time><h3>Destination au nom très long</h3></div><span>Arrivée souhaitée à 09:30</span></div><p class="trip-export-day__note">Rendez-vous devant l’entrée principale avec toute la famille.</p><ol class="trip-export-blocks"><li><span>12:30</span><div><strong>Déjeuner au restaurant dont le nom est particulièrement long</strong><p>Réservation du groupe.</p></div></li></ol></article></div>
      </section>
      <section class="trip-export-section surface" data-check-bound>
        <div class="trip-export-section__heading"><span class="trip-export-section__step">4</span><div><h2>Les décisions collectives</h2></div></div>
        <ul class="trip-export-decisions"><li data-check-bound><div><strong>Attraction au nom extrêmement long et non sécable</strong><span>Destination représentative</span></div><span>Groupe séparé</span><p>Une raison collective suffisamment longue pour éprouver la mise en page mobile.</p></li></ul>
      </section>
    </main>

    <main class="invitation-page" data-responsive-root="trip-invitation-preview">
      <a href="#">Retour à l'accueil</a>
      <article class="invitation-card-public surface" data-check-bound>
        <div class="invitation-card-public__intro"><h1>Une aventure européenne avec un titre très long</h1><p>Camille t'invite à préparer ce voyage.</p></div>
        <div class="invitation-card-public__facts"><div><i>◉</i><span>Rôle proposé</span><strong>Participant</strong></div><div><i>▣</i><span>Période approximative</span><strong>2027-07 → 2027-08</strong></div><div><i>♙</i><span>Taille du groupe</span><strong>2 à 5 membres</strong></div></div>
        <div class="invitation-card-public__privacy"><strong>Programme privé</strong><p>Aucun parc, jour, vote ou contrainte n'est révélé.</p></div>
        <div class="invitation-card-public__cta"><button>Me connecter pour continuer</button><p>Tu peux consulter cet aperçu sans compte.</p></div>
      </article>
    </main>
  </div>
  <nav class="test-mobile-navigation" aria-hidden="true">Navigation mobile</nav>`;

const baseStyles = `
  :root { --font-heading: sans-serif; --text-muted: #8a8175; --text-primary: #fff; --app-border: #655847; --app-surface: #17110c; --app-surface-2: #211810; --c-rose: #ff477e; --c-orange: #ff5b2d; --c-sky: #58c9ee; --c-gold: #d6a945; --c-lime: #b8e532; --c-purple: #a978ff; --radius-md: 0.75rem; --radius-lg: 1rem; --shadow-lg: 0 1rem 2rem #0008; }
  * { box-sizing: border-box; }
  html, body { width: 100%; max-width: 100%; margin: 0; overflow-x: clip; }
  body { background: #0d0906; color: var(--text-primary); font: 16px/1.4 sans-serif; }
  button, input, select, textarea, a { max-width: 100%; font: inherit; }
  button, a { overflow-wrap: anywhere; }
  .surface { padding: 1rem; border: 1px solid var(--app-border); border-radius: var(--radius-lg); background: var(--app-surface); }
  .test-mobile-navigation { display: none; }
  @media (max-width: 36rem) { .test-mobile-navigation { position: fixed; z-index: 10; right: 0.75rem; bottom: 0; left: 0.75rem; display: grid; height: 4.75rem; place-items: center; border: 1px solid var(--app-border); border-radius: 1rem 1rem 0 0; background: #120d09; } }
`;

const evaluationScript = `
  (() => {
    const viewportRight = document.documentElement.clientWidth;
    const checked = Array.from(document.querySelectorAll('[data-responsive-root], [data-check-bound], input, select, textarea, button, a'))
      .filter((element) => !element.closest('.wishlist-strip'));
    const violations = [];
    if (document.documentElement.scrollWidth > viewportRight + 1) {
      violations.push({ selector: 'document', reason: 'horizontal-overflow', right: document.documentElement.scrollWidth, viewportRight });
    }
    for (const element of checked) {
      const bounds = element.getBoundingClientRect();
      if (bounds.left < -1 || bounds.right > viewportRight + 1) {
        violations.push({ selector: element.tagName.toLowerCase() + (element.className ? '.' + String(element.className).trim().replaceAll(' ', '.') : ''), reason: 'out-of-bounds', left: bounds.left, right: bounds.right, viewportRight });
      }
    }
    if (viewportRight <= 576) {
      for (const root of document.querySelectorAll('[data-responsive-root]')) {
        const bottomPadding = Number.parseFloat(getComputedStyle(root).paddingBottom);
        if (bottomPadding < 80) {
          violations.push({ selector: root.getAttribute('data-responsive-root'), reason: 'fixed-navigation-overlap', bottomPadding });
        }
      }
    }
    document.querySelector('#responsive-result').textContent = JSON.stringify({ viewportRight, violations });
  })();`;

const reservePort = () => new Promise((resolvePort, rejectPort) => {
  const server = createServer();
  server.once('error', rejectPort);
  server.listen(0, '127.0.0.1', () => {
    const address = server.address();
    const port = typeof address === 'object' && address ? address.port : null;
    server.close((error) => error ? rejectPort(error) : resolvePort(port));
  });
});

const waitForPageTarget = async (port) => {
  const deadline = Date.now() + 30000;
  while (Date.now() < deadline) {
    try {
      const response = await fetch(`http://127.0.0.1:${port}/json/list`);
      const targets = await response.json();
      const page = targets.find((target) => target.type === 'page');
      if (page?.webSocketDebuggerUrl) {
        return page.webSocketDebuggerUrl;
      }
    } catch {
      // Chrome has not opened its DevTools endpoint yet.
    }
    await new Promise((resolveWait) => setTimeout(resolveWait, 100));
  }
  throw new Error('Chrome did not expose a page target within thirty seconds.');
};

const connectDevTools = async (webSocketUrl) => {
  const socket = new WebSocket(webSocketUrl);
  await new Promise((resolveOpen, rejectOpen) => {
    socket.addEventListener('open', resolveOpen, { once: true });
    socket.addEventListener('error', rejectOpen, { once: true });
  });

  let sequence = 0;
  const pending = new Map();
  const eventWaiters = new Map();
  socket.addEventListener('message', (event) => {
    const message = JSON.parse(event.data);
    if (message.id) {
      const request = pending.get(message.id);
      pending.delete(message.id);
      if (message.error) {
        request?.reject(new Error(message.error.message));
      } else {
        request?.resolve(message.result);
      }
      return;
    }
    const waiters = eventWaiters.get(message.method) ?? [];
    eventWaiters.delete(message.method);
    for (const resolveEvent of waiters) {
      resolveEvent(message.params);
    }
  });

  return {
    close: () => socket.close(),
    send: (method, params = {}) => new Promise((resolveRequest, rejectRequest) => {
      const id = ++sequence;
      pending.set(id, { reject: rejectRequest, resolve: resolveRequest });
      socket.send(JSON.stringify({ id, method, params }));
    }),
    waitFor: (method) => new Promise((resolveEvent) => {
      const waiters = eventWaiters.get(method) ?? [];
      waiters.push(resolveEvent);
      eventWaiters.set(method, waiters);
    })
  };
};

const workingDirectory = await mkdtemp(join(tmpdir(), 'trip-responsive-'));
let chromeProcess;
let devTools;

try {
  const htmlPath = join(workingDirectory, 'trip-responsive.html');
  const html = `<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><style>${baseStyles}\n${componentStyles}</style></head><body>${fixtureMarkup}<pre id="responsive-result"></pre><script>${evaluationScript}</script></body></html>`;
  await writeFile(htmlPath, html, 'utf8');
  const port = await reservePort();
  chromeProcess = spawn(chromeExecutable, [
    '--headless=new',
    '--disable-gpu',
    '--disable-dev-shm-usage',
    '--hide-scrollbars',
    '--no-sandbox',
    '--remote-allow-origins=*',
    `--remote-debugging-port=${port}`,
    `--user-data-dir=${join(workingDirectory, 'chrome-profile')}`,
    'about:blank'
  ], { stdio: 'ignore' });
  const pageWebSocketUrl = await waitForPageTarget(port);
  devTools = await connectDevTools(pageWebSocketUrl);
  await devTools.send('Page.enable');
  await devTools.send('Runtime.enable');

  for (const width of viewportWidths) {
    await devTools.send('Emulation.setDeviceMetricsOverride', {
      width,
      height: 1200,
      deviceScaleFactor: 1,
      mobile: false,
      screenWidth: width,
      screenHeight: 1200
    });
    const pageLoaded = devTools.waitFor('Page.loadEventFired');
    await devTools.send('Page.navigate', { url: `${pathToFileURL(htmlPath).href}?width=${width}` });
    await pageLoaded;
    const evaluation = await devTools.send('Runtime.evaluate', {
      expression: "document.querySelector('#responsive-result')?.textContent",
      returnByValue: true
    });
    if (!evaluation.result?.value) {
      throw new Error(`Chrome did not return responsive measurements at ${width}px.`);
    }
    const result = JSON.parse(evaluation.result.value);
    if (result.viewportRight !== width || result.violations.length > 0) {
      throw new Error(`Trip responsive check failed at ${width}px: ${JSON.stringify(result)}`);
    }
  }

  console.log(`Trip pages fit real Chromium viewports: ${viewportWidths.join(', ')}px.`);
} finally {
  devTools?.close();
  if (chromeProcess && chromeProcess.exitCode === null) {
    chromeProcess.kill();
    await Promise.race([
      once(chromeProcess, 'exit'),
      new Promise((resolveWait) => setTimeout(resolveWait, 2000))
    ]);
  }
  await rm(workingDirectory, { force: true, recursive: true, maxRetries: 10, retryDelay: 100 });
}
