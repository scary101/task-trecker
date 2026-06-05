FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY steptreck.sln ./
COPY steptreck/steptreck.API.csproj steptreck/
COPY steptrek.Domain/steptreck.Domain.csproj steptrek.Domain/
RUN dotnet restore steptreck/steptreck.API.csproj

COPY . ./
RUN dotnet publish steptreck/steptreck.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "steptreck.API.dll"]
