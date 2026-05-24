# syntax=docker/dockerfile:1

FROM node:24-alpine AS build
WORKDIR /app

ARG ANGULAR_CONFIGURATION=production

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build -- --configuration ${ANGULAR_CONFIGURATION}

FROM node:24-alpine AS runtime
WORKDIR /app

ENV NODE_ENV=production \
    PORT=4000 \
    SSR_API_INTERNAL_URL=http://api:8080 \
    SSR_CSP_ENABLED=true \
    SSR_CSP_REPORT_ONLY=true \
    SSR_CSP_REPORT_URI=/api/security/csp-report

COPY package.json package-lock.json ./
RUN npm ci --omit=dev && npm cache clean --force

COPY --from=build --chown=node:node /app/dist ./dist

USER node
EXPOSE 4000

CMD ["node", "dist/amusement-park/server/server.mjs"]
