# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Копируем csproj и восстанавливаем зависимости
COPY *.csproj ./
RUN dotnet restore

# Копируем весь проект и публикуем
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Порт, на котором будет слушать приложение Render
EXPOSE 10000

# Команда запуска
ENTRYPOINT ["dotnet", "MobileAppBackendNet8.dll"]
