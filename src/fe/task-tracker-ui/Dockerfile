FROM node:24-alpine3.21 AS build

RUN apk update && apk upgrade --no-cache

WORKDIR /app

COPY src/fe/task-tracker-ui/package.json ./
COPY src/fe/task-tracker-ui/package-lock.json ./

RUN npm install

COPY src/fe/task-tracker-ui/. ./

COPY . .


ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL_ENV=$VITE_API_BASE_URL

ARG VITE_BASE_API_KEY
ENV VITE_BASE_API_KEY_ENV=$VITE_BASE_API_KEY

RUN echo "VITE_BASE_API_KEY=${VITE_BASE_API_KEY_ENV}" > .env.production && \
    echo "VITE_API_BASE_URL=${VITE_API_BASE_URL_ENV}" >> .env.production && \
    npm run build && \
    rm .env.production # Clean up

FROM nginx:1.28.0-alpine-slim AS final

RUN apk update && apk upgrade --no-cache

COPY --from=build /app/dist /usr/share/nginx/html
COPY src/fe/task-tracker-ui/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]