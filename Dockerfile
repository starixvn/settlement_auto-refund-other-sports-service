FROM hub.nexdev.net/base/dotnet/core/sdk:7.0-alpine AS builder
WORKDIR /src

COPY ./Sources/AutoRefundOtherSports ./AutoRefundOtherSports
COPY ./Sources/Fanex.AutoRefundOtherSports.Service/Fanex.AutoRefundOtherSports.Service.csproj ./Fanex.AutoRefundOtherSports.Service/Fanex.AutoRefundOtherSports.Service.csproj

WORKDIR /src/Fanex.AutoRefundOtherSports.Service
RUN dotnet nuget add source http://nuget.nexdev.net/api/v2
RUN dotnet restore -v n

COPY ./Sources/Fanex.AutoRefundOtherSports.Service .

RUN dotnet publish -r linux-x64 --self-contained false -c Release -o /app 

FROM hub.nexdev.net/base/dotnet/core/aspnet:7.0-alpine AS base

WORKDIR /app
COPY --from=builder /app .

ENTRYPOINT ["dotnet", "Fanex.AutoRefundOtherSports.Service.dll"]
