---
name: public-park-discovery
description: Find published amusement park pages and cite their localized public information using AMUSEMENT-PARKS.fun.
---

# Public park discovery

Use this guide when a user asks to find an amusement park or read its published information.

## Search

Open the public directory at `https://amusement-parks.fun/{language}/parks`. Supported language codes are `en`, `fr`, `de`, `nl`, `it`, `es`, `pt` and `pl`.

In a browser supporting WebMCP, public pages register the read-only `search_public_parks` tool. Its only parameter is `query`, a park name or location containing 2 to 120 characters. It returns at most ten public park names, localized page paths, cities and country codes. Resolve relative page paths against `https://amusement-parks.fun`. The tool uses the current page language, makes an anonymous request and does not access an account or modify data. If WebMCP is unavailable, use the visible directory search.

## Read and cite

Open a returned park page and read its visible published content. Use its canonical URL in citations. Follow the links it actually provides to attractions, photographs, opening hours, prices or published historical material.

Report unavailable information as unavailable. Park information may include permanently closed entities, so preserve the status shown on the page. Distinguish historical facts from current visitor information. Verify current operating details with the park's official sources when the user's request requires current information.

Only public reading and search are provided by this guide. Account actions, reviews, ratings, registrations, subscriptions and administration require separate user instructions and are not exposed by the tool.
