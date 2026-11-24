FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["JobAutoApplyNET.csproj", "./"]
RUN dotnet restore "JobAutoApplyNET.csproj"
COPY . .
RUN dotnet publish "JobAutoApplyNET.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV PORT=5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "JobAutoApplyNET.dll"]
