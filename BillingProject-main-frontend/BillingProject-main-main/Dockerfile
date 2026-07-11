# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY backend/BillingSystem.csproj ./backend/
RUN dotnet restore backend/BillingSystem.csproj

# Copy the remaining backend files and build
COPY backend/ ./backend/
WORKDIR /app/backend
RUN dotnet publish -c Release -o /app/out

# Stage 2: Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Render exposes the port dynamically via the PORT environment variable.
# ASP.NET Core 8 respects ASPNETCORE_HTTP_PORTS to bind to ports.
# We set it to 8080 by default (Render automatically detects this).
ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080
ENTRYPOINT ["dotnet", "BillingSystem.dll"]
