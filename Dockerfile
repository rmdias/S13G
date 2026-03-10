# Stage 1 — build and publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first to maximise layer cache on restore
COPY S13G.sln ./
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Application/Application.csproj src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Api/Api.csproj src/Api/

RUN dotnet restore src/Api/Api.csproj

# Copy the rest of the source and publish
COPY src/ src/
RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish --no-restore

# Stage 2 — runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Render routes external traffic to port 10000 by default
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "Api.dll"]
