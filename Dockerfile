# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj y restaurar dependencias
COPY *.sln .
COPY AgendaSalud.Postino.EmailService/*.csproj ./AgendaSalud.Postino.EmailService/
RUN dotnet restore

# Copiar el resto del código y compilar
COPY . .
WORKDIR /src/AgendaSalud.Postino.EmailService
RUN dotnet publish -c Release -o /app

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "AgendaSalud.Postino.EmailService.dll"]
