# Build context is ./frontend/dailymart-ui (see docker-compose.yml).

FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npx ng build --configuration production

FROM nginx:alpine AS runtime

# The "application" builder puts the static browser bundle under dist/<project>/browser, not dist/<project>
# directly - see angular.json (no explicit outputPath override).
COPY --from=build /app/dist/dailymart-ui/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
