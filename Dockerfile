# Stage 1: build the React frontend.
# Pinned to the build host's architecture: the output is static files, so when
# cross-building for a Pi with buildx this stage runs natively instead of under
# QEMU emulation. Building on the Pi itself is unaffected.
FROM --platform=$BUILDPLATFORM node:22-alpine AS frontend
WORKDIR /src
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# Stage 2: build the .NET API
FROM mcr.microsoft.com/dotnet/sdk:10.0.301 AS backend
WORKDIR /src
COPY global.json .
COPY backend/OpenWealth.Api/OpenWealth.Api.csproj OpenWealth.Api/
RUN dotnet restore OpenWealth.Api/OpenWealth.Api.csproj
COPY backend/OpenWealth.Api/ OpenWealth.Api/
RUN dotnet publish OpenWealth.Api/OpenWealth.Api.csproj -c Release -o /app

# Stage 3: runtime — API serves the built frontend
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=backend /app .
COPY --from=frontend /src/dist ./wwwroot
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__Default="Data Source=/data/openwealth.db"
VOLUME /data
EXPOSE 8080
ENTRYPOINT ["dotnet", "OpenWealth.Api.dll"]
