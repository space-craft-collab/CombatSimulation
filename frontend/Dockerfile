# syntax=docker/dockerfile:1

# The frontend's whole toolchain: Node and npm exist only in these
# images (ADR-0004). One pinned Node version serves development,
# the build and the runtime.
FROM node:24.22.3-trixie-slim AS base
WORKDIR /app

# Development: the sources are bind-mounted (compose.yaml).
FROM base AS dev
USER node
CMD ["npm", "run", "dev"]

# Production build (adapter-node).
FROM base AS build
COPY .npmrc package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

# Production runtime: the built server and its runtime packages.
FROM base AS runtime
ENV NODE_ENV=production
COPY .npmrc package.json package-lock.json ./
RUN npm ci --omit=dev
COPY --from=build /app/build ./build
USER node
EXPOSE 3000
CMD ["node", "build"]
