FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["PremierLottoApi/PremierLottoApi.csproj", "PremierLottoApi/"]
RUN dotnet restore "PremierLottoApi/PremierLottoApi.csproj"
COPY . .
WORKDIR "/src/PremierLottoApi"
RUN dotnet build "PremierLottoApi.csproj" -c Release -o /app/build
RUN dotnet publish "PremierLottoApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PremierLottoApi.dll"]