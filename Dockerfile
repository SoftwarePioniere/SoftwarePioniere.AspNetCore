FROM microsoft/dotnet:2.1.300-sdk-alpine AS restore
ARG CONFIGURATION=Release
WORKDIR /proj
COPY nuget.config.build.tmp ./nuget.config
COPY Directory.Build.* ./
COPY *.sln ./
COPY src/SoftwarePioniere.AspNetCore/*.csproj ./src/SoftwarePioniere.AspNetCore/
COPY src/SoftwarePioniere.AspNetCore.SampleApp/*.csproj ./src/SoftwarePioniere.AspNetCore.SampleApp/
RUN dotnet restore SoftwarePioniere.AspNetCore.sln

FROM restore as src
COPY . .

FROM src AS buildsln
ARG CONFIGURATION=Release
ARG NUGETVERSIONV2=99.99.99
ARG ASSEMBLYSEMVER=99.99.99.99
WORKDIR /proj/src/
RUN dotnet build /proj/SoftwarePioniere.AspNetCore.sln -c $CONFIGURATION --no-restore /p:NuGetVersionV2=$NUGETVERSIONV2 /p:AssemblySemVer=$ASSEMBLYSEMVER

FROM buildsln as pack
ARG CONFIGURATION=Release
ARG NUGETVERSIONV2=99.99.99
ARG ASSEMBLYSEMVER=99.99.99.99
RUN dotnet pack /proj/SoftwarePioniere.AspNetCore.sln -c $CONFIGURATION --no-restore --no-build /p:NuGetVersionV2=$NUGETVERSIONV2 /p:AssemblySemVer=$ASSEMBLYSEMVER -o /proj/packages
WORKDIR /proj/packages/