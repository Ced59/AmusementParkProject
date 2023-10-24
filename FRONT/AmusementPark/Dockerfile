# Étape de construction
FROM node:20 as build

# Commentaires pour expliquer ce que fait chaque étape
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm install
COPY . ./
RUN npm run build --prod

# Définir une variable d'environnement pour l'URL de l'API
ENV API_URL http://api:5000

# Étape de déploiement
FROM nginx:alpine

# Copier les fichiers de l'application Angular dans le conteneur Nginx
COPY --from=build /app/dist/amusement-park /usr/share/nginx/html

# Exposer le port 80
EXPOSE 80

# Démarrer Nginx avec l'option `daemon off` pour garder le conteneur en cours d'exécution
CMD ["nginx", "-g", "daemon off;"]

