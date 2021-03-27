FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /build
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS final
WORKDIR /usr/share/dotnet
COPY --from=build /app .
COPY settings.json .
COPY .secrets .

ENTRYPOINT ["dotnet", "AgileBot.dll"]