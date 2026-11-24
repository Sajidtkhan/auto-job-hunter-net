FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["JobAutoApplyNET.csproj", "./"]
RUN dotnet restore "JobAutoApplyNET.csproj"
COPY . .
RUN dotnet publish "JobAutoApplyNET.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "JobAutoApplyNET.dll"]
