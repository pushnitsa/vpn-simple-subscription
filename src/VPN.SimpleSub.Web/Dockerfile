FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

COPY --from=build-env /app/out .

RUN mkdir /appData

VOLUME /appData

EXPOSE 5000

ENTRYPOINT ["dotnet", "VPN.SimpleSub.Web.dll"]