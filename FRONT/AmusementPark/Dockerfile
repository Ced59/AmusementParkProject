# Étape de construction
FROM node:20 as build

WORKDIR /app
COPY package.json package-lock.json ./
RUN npm install
COPY . ./
RUN npm run build --prod

# Étape de déploiement
FROM nginx:alpine

COPY --from=build /app/dist/amusement-park /usr/share/nginx/html
EXPOSE 80
EXPOSE 443  

CMD ["nginx", "-g", "daemon off;"]



