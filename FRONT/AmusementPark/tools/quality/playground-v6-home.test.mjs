import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

const projectRoot = resolve(import.meta.dirname, '../..');
const template = readFileSync(
  resolve(projectRoot, 'src/app/features/public/home/pages/home-view.component.html'),
  'utf8',
);
const styles = readFileSync(
  resolve(projectRoot, 'src/app/features/public/home/pages/home.component.scss'),
  'utf8',
);

test('the home hero keeps API statistics, two CTAs and four dynamic park slots', () => {
  for (const count of ['parksCount', 'attractionsCount', 'countriesCount']) {
    assert.match(template, new RegExp(`formatStatValue\\(homeStats\\(\\)\\?\\.${count}\\)`));
  }

  assert.match(template, /home\.ctaBrowseParks/);
  assert.match(template, /home\.ctaLearnMore/);
  assert.match(template, /@for \(park of heroFeaturedParks\(\)/);
  assert.match(template, /getHeroCardClass\(index\)/);
  assert.match(template, /\[imageId\]="park\.logoImageId"/);
  assert.match(template, /\[loading\]="index === 0 \? 'eager' : 'lazy'"/);
  assert.match(template, /\[fetchPriority\]="index === 0 \? 'high' : 'low'"/);
  assert.doesNotMatch(template, /Le Fleury|Pendolino|38 lieux/i);
});

test('the complete production search contract remains in the home template', () => {
  for (const contract of [
    '[searchTerm]="searchTerm()"',
    '[filters]="searchFilters()"',
    '(searchTermChange)="onSearchInput($event)"',
    '(searchKeyDown)="onSearchKeyDown($event)"',
    '(searchClicked)="onSearchSubmit()"',
    '(filterChanged)="onFilterChanged($event)"',
    '(clearClicked)="onClearSearch()"',
    '(click)="onAutocompleteSelected(autocompleteSuggestion)"',
    '(click)="onSuggestionSelected(suggestionCard.title)"',
    '[rowsPerPageOptions]="[5, 10, 20]"',
    '(pageChanged)="onPageChange($event)"',
  ]) {
    assert.ok(template.includes(contract), `${contract} must remain wired`);
  }

  assert.match(template, /<app-ui-search-result-card/);
  assert.match(template, /<app-pagination/);
});

test('featured, latest and sharing blocks keep their deferred production composition', () => {
  assert.match(template, /@for \(park of featuredParks\(\)/);
  assert.match(template, /@defer \(on viewport; hydrate on viewport; prefetch on idle\)/);
  assert.match(template, /<app-home-latest-content>/);
  assert.match(template, /<app-public-share-panel/);
});

test('the Playground home uses the editorial split without decorative effects', () => {
  assert.match(styles, /grid-template-columns:\s*minmax\(0, 1\.08fr\) minmax\(0, 0\.92fr\)/);
  assert.match(styles, /\.home-hero__spotlight[\s\S]*?border-left:\s*3px solid var\(--app-heading\)/);
  assert.match(styles, /\.home-hero-card--1[\s\S]*?border-top:\s*5px solid var\(--c-orange\)/);
  assert.match(styles, /@media \(max-width:\s*960px\)[\s\S]*?grid-template-columns:\s*1fr/);
  assert.match(styles, /@media \(max-width:\s*680px\)/);
  assert.doesNotMatch(styles, /(?:linear|radial|conic)-gradient\(/);
  assert.doesNotMatch(styles, /animation\s*:/);
  assert.doesNotMatch(styles, /backdrop-filter\s*:/);
});

test('the home title and primary image use the supplied V6 dimensions rather than the previous oversized envelope', () => {
  assert.match(styles, /font-size:\s*clamp\(39px, 5\.3vw, 74px\)/);
  assert.match(styles, /line-height:\s*1\.065/);
  assert.match(styles, /gap:\s*clamp\(18px, 3vw, 38px\)/);
  assert.match(styles, /\.home-hero-card--1 \.home-hero-card__media\s*\{[^}]*height:\s*205px/);
  assert.match(styles, /font-size:\s*clamp\(38px, 9vw, 53px\)/);
  assert.match(styles, /\.home-hero__title-line--outline\s*\{[^}]*color:\s*var\(--app-heading\);[^}]*-webkit-text-stroke:\s*0/);
});
