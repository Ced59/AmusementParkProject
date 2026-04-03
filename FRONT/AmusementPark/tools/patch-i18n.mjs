import fs from 'node:fs';
import path from 'node:path';

const root = process.cwd();
const i18nDir = path.join(root, 'src', 'assets', 'i18n');

const langs = ['en', 'fr', 'es', 'de', 'it', 'nl', 'pl', 'pt'];

const T = {
  en: {
    homeSubtitle: 'Find theme parks and plan your next trip.',
    filterCountry: 'Filter by country',
    noResults: 'No parks found.',
    topbar: {
      selectLanguage: 'Select language',
      loginOrRegister: 'Login / Register',
      language: 'Language',
      login: 'Login'
    },
    parkList: {
      title: 'Parks',
      subtitle: 'Browse all parks.',
      backHome: 'Back to home'
    },
    actions: { logout: 'Logout' },
    registerErrors: {
      emailSummary: 'Invalid email',
      email: 'Please enter a valid email address.',
      mismatchSummary: 'Password mismatch',
      mismatch: 'Passwords do not match.'
    },
    admin: {
      loading: 'Loading…',
      location: 'Location',
      visibleOn: 'Visible',
      visibleOff: 'Hidden',
      logos: {
        sectionTitle: 'Logos',
        currentTitle: 'Current logos',
        noCurrent: 'No logos yet.',
        uploadTitle: 'Add / replace logos',
        descriptionPlaceholder: 'Description (optional)',
        uploadButton: 'Upload',
        historyTitle: 'History',
        empty: 'No previous logo.',
        noDescription: 'No description'
      }
    }
  },
  fr: {
    homeSubtitle: 'Trouvez des parcs d’attractions et préparez votre prochaine visite.',
    filterCountry: 'Filtrer par pays',
    noResults: 'Aucun parc trouvé.',
    topbar: {
      selectLanguage: 'Choisir la langue',
      loginOrRegister: 'Connexion / Inscription',
      language: 'Langue',
      login: 'Connexion'
    },
    parkList: {
      title: 'Parcs',
      subtitle: 'Parcourez tous les parcs.',
      backHome: 'Retour à l\'accueil'
    },
    actions: { logout: 'Déconnexion' },
    registerErrors: {
      emailSummary: 'Email invalide',
      email: 'Veuillez saisir une adresse email valide.',
      mismatchSummary: 'Confirmation du mot de passe',
      mismatch: 'Les mots de passe ne correspondent pas.'
    },
    admin: {
      loading: 'Chargement…',
      location: 'Localisation',
      visibleOn: 'Visible',
      visibleOff: 'Masqué',
      logos: {
        sectionTitle: 'Logos',
        currentTitle: 'Logos actuels',
        noCurrent: 'Aucun logo pour l\'instant.',
        uploadTitle: 'Ajouter / remplacer des logos',
        descriptionPlaceholder: 'Description (optionnelle)',
        uploadButton: 'Importer',
        historyTitle: 'Historique',
        empty: 'Aucun logo précédent.',
        noDescription: 'Sans description'
      }
    }
  },
  es: {
    homeSubtitle: 'Encuentra parques temáticos y planifica tu próxima visita.',
    filterCountry: 'Filtrar por país',
    noResults: 'No se encontraron parques.',
    topbar: {
      selectLanguage: 'Elegir idioma',
      loginOrRegister: 'Iniciar sesión / Registrarse',
      language: 'Idioma',
      login: 'Iniciar sesión'
    },
    parkList: {
      title: 'Parques',
      subtitle: 'Explora todos los parques.',
      backHome: 'Volver al inicio'
    },
    actions: { logout: 'Cerrar sesión' },
    registerErrors: {
      emailSummary: 'Correo no válido',
      email: 'Introduce una dirección de correo válida.',
      mismatchSummary: 'Las contraseñas no coinciden',
      mismatch: 'Las contraseñas no coinciden.'
    },
    admin: {
      loading: 'Cargando…',
      location: 'Ubicación',
      visibleOn: 'Visible',
      visibleOff: 'Oculto',
      logos: {
        sectionTitle: 'Logos',
        currentTitle: 'Logos actuales',
        noCurrent: 'Aún no hay logos.',
        uploadTitle: 'Añadir / reemplazar logos',
        descriptionPlaceholder: 'Descripción (opcional)',
        uploadButton: 'Subir',
        historyTitle: 'Historial',
        empty: 'No hay logos anteriores.',
        noDescription: 'Sin descripción'
      }
    }
  },
  de: {
    homeSubtitle: 'Finde Freizeitparks und plane deinen nächsten Trip.',
    filterCountry: 'Nach Land filtern',
    noResults: 'Keine Parks gefunden.',
    topbar: {
      selectLanguage: 'Sprache wählen',
      loginOrRegister: 'Anmelden / Registrieren',
      language: 'Sprache',
      login: 'Anmelden'
    },
    parkList: {
      title: 'Parks',
      subtitle: 'Alle Parks durchsuchen.',
      backHome: 'Zur Startseite'
    },
    actions: { logout: 'Abmelden' },
    registerErrors: {
      emailSummary: 'Ungültige E-Mail',
      email: 'Bitte gib eine gültige E-Mail-Adresse ein.',
      mismatchSummary: 'Passwörter stimmen nicht überein',
      mismatch: 'Die Passwörter stimmen nicht überein.'
    },
    admin: {
      loading: 'Laden…',
      location: 'Ort',
      visibleOn: 'Sichtbar',
      visibleOff: 'Ausgeblendet',
      logos: {
        sectionTitle: 'Logos',
        currentTitle: 'Aktuelle Logos',
        noCurrent: 'Noch keine Logos.',
        uploadTitle: 'Logos hinzufügen / ersetzen',
        descriptionPlaceholder: 'Beschreibung (optional)',
        uploadButton: 'Hochladen',
        historyTitle: 'Verlauf',
        empty: 'Keine früheren Logos.',
        noDescription: 'Keine Beschreibung'
      }
    }
  },
  it: {
    homeSubtitle: 'Trova parchi a tema e pianifica la tua prossima visita.',
    filterCountry: 'Filtra per paese',
    noResults: 'Nessun parco trovato.',
    topbar: {
      selectLanguage: 'Scegli lingua',
      loginOrRegister: 'Accedi / Registrati',
      language: 'Lingua',
      login: 'Accedi'
    },
    parkList: {
      title: 'Parchi',
      subtitle: 'Sfoglia tutti i parchi.',
      backHome: 'Torna alla home'
    },
    actions: { logout: 'Disconnetti' },
    registerErrors: {
      emailSummary: 'Email non valida',
      email: 'Inserisci un indirizzo email valido.',
      mismatchSummary: 'Le password non coincidono',
      mismatch: 'Le password non coincidono.'
    },
    admin: {
      loading: 'Caricamento…',
      location: 'Località',
      visibleOn: 'Visibile',
      visibleOff: 'Nascosto',
      logos: {
        sectionTitle: 'Loghi',
        currentTitle: 'Loghi attuali',
        noCurrent: 'Nessun logo per ora.',
        uploadTitle: 'Aggiungi / sostituisci loghi',
        descriptionPlaceholder: 'Descrizione (opzionale)',
        uploadButton: 'Carica',
        historyTitle: 'Cronologia',
        empty: 'Nessun logo precedente.',
        noDescription: 'Nessuna descrizione'
      }
    }
  },
  nl: {
    homeSubtitle: 'Vind pretparken en plan je volgende uitstap.',
    filterCountry: 'Filter op land',
    noResults: 'Geen parken gevonden.',
    topbar: {
      selectLanguage: 'Taal kiezen',
      loginOrRegister: 'Inloggen / Registreren',
      language: 'Taal',
      login: 'Inloggen'
    },
    parkList: {
      title: 'Parken',
      subtitle: 'Bekijk alle parken.',
      backHome: 'Terug naar start'
    },
    actions: { logout: 'Uitloggen' },
    registerErrors: {
      emailSummary: 'Ongeldig e-mailadres',
      email: 'Voer een geldig e-mailadres in.',
      mismatchSummary: 'Wachtwoorden komen niet overeen',
      mismatch: 'De wachtwoorden komen niet overeen.'
    },
    admin: {
      loading: 'Laden…',
      location: 'Locatie',
      visibleOn: 'Zichtbaar',
      visibleOff: 'Verborgen',
      logos: {
        sectionTitle: 'Logo\'s',
        currentTitle: 'Huidige logo\'s',
        noCurrent: 'Nog geen logo\'s.',
        uploadTitle: 'Logo\'s toevoegen / vervangen',
        descriptionPlaceholder: 'Beschrijving (optioneel)',
        uploadButton: 'Uploaden',
        historyTitle: 'Geschiedenis',
        empty: 'Geen eerdere logo\'s.',
        noDescription: 'Geen beschrijving'
      }
    }
  },
  pl: {
    homeSubtitle: 'Znajdź parki rozrywki i zaplanuj następną wizytę.',
    filterCountry: 'Filtruj według kraju',
    noResults: 'Nie znaleziono parków.',
    topbar: {
      selectLanguage: 'Wybierz język',
      loginOrRegister: 'Zaloguj / Zarejestruj',
      language: 'Język',
      login: 'Zaloguj'
    },
    parkList: {
      title: 'Parki',
      subtitle: 'Przeglądaj wszystkie parki.',
      backHome: 'Powrót do strony głównej'
    },
    actions: { logout: 'Wyloguj' },
    registerErrors: {
      emailSummary: 'Nieprawidłowy e-mail',
      email: 'Wpisz poprawny adres e-mail.',
      mismatchSummary: 'Hasła nie pasują',
      mismatch: 'Hasła nie są takie same.'
    },
    admin: {
      loading: 'Ładowanie…',
      location: 'Lokalizacja',
      visibleOn: 'Widoczne',
      visibleOff: 'Ukryte',
      logos: {
        sectionTitle: 'Logotypy',
        currentTitle: 'Aktualne logotypy',
        noCurrent: 'Brak logotypów.',
        uploadTitle: 'Dodaj / zamień logotypy',
        descriptionPlaceholder: 'Opis (opcjonalnie)',
        uploadButton: 'Prześlij',
        historyTitle: 'Historia',
        empty: 'Brak wcześniejszych logotypów.',
        noDescription: 'Brak opisu'
      }
    }
  },
  pt: {
    homeSubtitle: 'Encontre parques temáticos e planeie a sua próxima visita.',
    filterCountry: 'Filtrar por país',
    noResults: 'Nenhum parque encontrado.',
    topbar: {
      selectLanguage: 'Escolher idioma',
      loginOrRegister: 'Entrar / Registar',
      language: 'Idioma',
      login: 'Entrar'
    },
    parkList: {
      title: 'Parques',
      subtitle: 'Explore todos os parques.',
      backHome: 'Voltar à página inicial'
    },
    actions: { logout: 'Terminar sessão' },
    registerErrors: {
      emailSummary: 'Email inválido',
      email: 'Introduza um endereço de email válido.',
      mismatchSummary: 'As palavras‑passe não coincidem',
      mismatch: 'As palavras‑passe não coincidem.'
    },
    admin: {
      loading: 'A carregar…',
      location: 'Localização',
      visibleOn: 'Visível',
      visibleOff: 'Oculto',
      logos: {
        sectionTitle: 'Logotipos',
        currentTitle: 'Logotipos atuais',
        noCurrent: 'Ainda não há logotipos.',
        uploadTitle: 'Adicionar / substituir logotipos',
        descriptionPlaceholder: 'Descrição (opcional)',
        uploadButton: 'Carregar',
        historyTitle: 'Histórico',
        empty: 'Sem logotipos anteriores.',
        noDescription: 'Sem descrição'
      }
    }
  }
};

function ensureObj(obj, key) {
  if (!obj[key] || typeof obj[key] !== 'object') obj[key] = {};
  return obj[key];
}

function ensureStr(obj, key, value) {
  if (obj[key] === undefined || obj[key] === null || obj[key] === '') {
    obj[key] = value;
  }
}

for (const lang of langs) {
  const file = path.join(i18nDir, `${lang}.json`);
  const raw = fs.readFileSync(file, 'utf8');
  const json = JSON.parse(raw);

  const tr = T[lang];

  // HOME
  const home = ensureObj(json, 'home');
  if (!home.title) home.title = home.home_title ?? home.title ?? (lang === 'fr' ? 'Accueil' : 'Home');
  if (!home.searchPlaceholder) home.searchPlaceholder = home.placeholder_search ?? home.searchPlaceholder ?? '';
  ensureStr(home, 'subtitle', tr.homeSubtitle);
  ensureStr(home, 'filterCountry', tr.filterCountry);
  // map existing message if any
  ensureStr(home, 'noResults', home.no_results ?? tr.noResults);

  // TOPBAR
  const topbar = ensureObj(json, 'topbar');
  ensureStr(topbar, 'selectLanguage', topbar.choose_language ?? tr.topbar.selectLanguage);
  ensureStr(topbar, 'loginOrRegister', topbar.login_header ?? tr.topbar.loginOrRegister);
  ensureStr(topbar, 'language', tr.topbar.language);
  ensureStr(topbar, 'login', tr.topbar.login);

  // PARK LIST
  const parkList = ensureObj(json, 'parkList');
  ensureStr(parkList, 'title', tr.parkList.title);
  ensureStr(parkList, 'subtitle', tr.parkList.subtitle);
  ensureStr(parkList, 'backHome', tr.parkList.backHome);

  // ACTIONS
  const actions = ensureObj(json, 'actions');
  ensureStr(actions, 'logout', tr.actions.logout);

  // AUTH / REGISTER
  const auth = ensureObj(json, 'auth');
  const register = ensureObj(auth, 'register');
  const errors = ensureObj(register, 'errors');

  // move/copy previous mismatch keys if they exist
  if (register.password_mismatch && !errors.password_mismatch) {
    errors.password_mismatch = register.password_mismatch;
  }
  if (register.password_mismatch_summary && !errors.password_mismatch_summary) {
    errors.password_mismatch_summary = register.password_mismatch_summary;
  }

  // add missing error keys
  ensureStr(errors, 'email_error_summary', tr.registerErrors.emailSummary);
  ensureStr(errors, 'email_error', tr.registerErrors.email);
  ensureStr(errors, 'password_mismatch_summary', tr.registerErrors.mismatchSummary);
  ensureStr(errors, 'password_mismatch', tr.registerErrors.mismatch);

  // Fix minor typo in FR register button if present
  if (lang === 'fr' && register.btn_text === "S'enregister") {
    register.btn_text = "S'enregistrer";
  }

  // ADMIN
  const admin = ensureObj(json, 'admin');
  ensureStr(admin, 'loading', tr.admin.loading);
  const parks = ensureObj(admin, 'parks');
  ensureStr(parks, 'location', tr.admin.location);
  ensureStr(parks, 'visibleOn', tr.admin.visibleOn);
  ensureStr(parks, 'visibleOff', tr.admin.visibleOff);

  const logos = ensureObj(parks, 'logos');
  for (const [k, v] of Object.entries(tr.admin.logos)) {
    ensureStr(logos, k, v);
  }

  // Write
  fs.writeFileSync(file, JSON.stringify(json, null, 2) + '\n', 'utf8');
}

console.log('i18n patched.');
