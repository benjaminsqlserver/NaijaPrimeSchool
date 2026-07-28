# syntax=docker/dockerfile:1

##############################################
# Stage 1: Build & Publish
##############################################
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first so Docker can cache the NuGet restore
# layer whenever source code changes but dependencies don't.
COPY ["NaijaPrimeSchool.slnx", "./"]
COPY ["src/NaijaPrimeSchool.Domain/NaijaPrimeSchool.Domain.csproj", "src/NaijaPrimeSchool.Domain/"]
COPY ["src/NaijaPrimeSchool.Application/NaijaPrimeSchool.Application.csproj", "src/NaijaPrimeSchool.Application/"]
COPY ["src/NaijaPrimeSchool.Infrastructure/NaijaPrimeSchool.Infrastructure.csproj", "src/NaijaPrimeSchool.Infrastructure/"]
COPY ["src/NaijaPrimeSchool.Web.Client/NaijaPrimeSchool.Web.Client.csproj", "src/NaijaPrimeSchool.Web.Client/"]
COPY ["src/NaijaPrimeSchool.Web/NaijaPrimeSchool.Web.csproj", "src/NaijaPrimeSchool.Web/"]

RUN dotnet restore "src/NaijaPrimeSchool.Web/NaijaPrimeSchool.Web.csproj"

# Now copy the rest of the source and publish.
COPY . .

RUN dotnet publish "src/NaijaPrimeSchool.Web/NaijaPrimeSchool.Web.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

##############################################
# Stage 2: Runtime
##############################################
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Run as the built-in non-root user provided by the base image.
USER $APP_UID

# ASP.NET Core in containers listens on 8080 by default in .NET 8+.
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "NaijaPrimeSchool.Web.dll"]
