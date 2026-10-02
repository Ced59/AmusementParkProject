import express from 'express';
import type { Express } from 'express';
import { basename } from 'node:path';

export function registerPublicAgentDiscoveryRoutes(server: Express, browserDirectory: string): void {
  server.get(['/llms.txt', '/ai-catalog.json', '/agent-guide.md'], express.static(browserDirectory, {
    index: false,
    fallthrough: false,
    setHeaders(response, filePath): void {
      response.setHeader('Cache-Control', 'no-cache, max-age=0, must-revalidate');
      response.setHeader('X-Robots-Tag', 'noindex');
      const fileName: string = basename(filePath);
      if (fileName === 'ai-catalog.json') {
        response.setHeader('Content-Type', 'application/ai-catalog+json; charset=utf-8');
      } else if (fileName === 'agent-guide.md') {
        response.setHeader('Content-Type', 'text/markdown; profile="urn:air:agent-skills"; charset=utf-8');
      } else {
        response.setHeader('Content-Type', 'text/plain; charset=utf-8');
      }
    }
  }));
}
