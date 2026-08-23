# Build context is this directory.
#   docker build -t healthwatcherui .
FROM node:22-alpine AS build
WORKDIR /src

# Install against package.json alone so this layer caches across source edits.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:alpine AS runtime
COPY --from=build /src/dist/healthwatcher-ui/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
