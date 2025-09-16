# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj y restaurar dependencias
COPY AgendaSalud.Postino.EmailService.csproj ./
RUN dotnet restore AgendaSalud.Postino.EmailService.csproj

# Copiar todo el código y compilar
COPY . .
RUN dotnet publish AgendaSalud.Postino.EmailService.csproj -c Release -o /app

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "AgendaSalud.Postino.EmailService.dll"]
