FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_ENVIRONMENT=Docker
WORKDIR /src

COPY steptreck.sln ./
COPY steptreck.Web/steptreck.Web.csproj steptreck.Web/
COPY steptrek.Domain/steptreck.Domain.csproj steptrek.Domain/
RUN dotnet restore steptreck.Web/steptreck.Web.csproj

COPY . ./
RUN dotnet publish steptreck.Web/steptreck.Web.csproj -c Release -o /app/publish -p:EnvironmentName=${BUILD_ENVIRONMENT}

FROM nginx:alpine
COPY steptreck.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 80
