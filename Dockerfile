# 建置階段 (使用 SDK 映像檔來編譯程式碼)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# 複製專案檔案到容器中
COPY . .
# 還原專案相依性 (NuGet 套件)
RUN dotnet restore
# 發佈 (編譯成最終的執行檔)
RUN dotnet publish -c Release -o /app

# 執行階段 (使用更輕量的 ASP.NET 執行環境映像檔)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
# 從建置階段複製發佈好的檔案
COPY --from=build /app .
EXPOSE 5112
# 定義容器啟動時的執行指令 (使用您的專案名稱)
ENTRYPOINT ["dotnet", "HRSystemAPI.dll"]
