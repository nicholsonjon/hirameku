# Hirameku is a cloud-native, vendor-agnostic, serverless application for
# studying flashcards with support for localization and accessibility.
# Copyright (C) 2023 Jon Nicholson
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU Affero General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU Affero General Public License for more details.
#
# You should have received a copy of the GNU Affero General Public License
# along with this program.  If not, see <https://www.gnu.org/licenses/>.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["IdentityService/src/IdentityService.csproj", "IdentityService/src/"]
COPY ["Authentication/src/Authentication.csproj", "Authentication/src/"]
COPY ["Caching/src/Caching.csproj", "Caching/src/"]
COPY ["Common/Common/src/Common.csproj", "Common/Common/src/"]
COPY ["Common/Common.Service/src/Common.Service.csproj", "Common/Common.Service/src/"]
COPY ["Data/src/Data.csproj", "Data/src/"]
COPY ["Email/src/Email.csproj", "Email/src/"]
COPY ["Recaptcha/src/Recaptcha.csproj", "Recaptcha/src/"]
COPY ["Registration/src/Registration.csproj", "Registration/src/"]
COPY ["User/src/User.csproj", "User/src/"]
COPY ["Directory.Build.props", "/src/"]
RUN dotnet restore "IdentityService/src/IdentityService.csproj"
COPY . .
COPY ["IdentityService/src/password-blacklist.txt", "/etc/hirameku/password-blacklist.txt"]
WORKDIR "/src/IdentityService/src"
RUN dotnet build "IdentityService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IdentityService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hirameku.IdentityService.dll"]
