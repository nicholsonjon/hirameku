# Introduction 

Hirameku is a cloud-native, vendor-agnostic, serverless application for studying flashcards with support for localization and accessibility. It is intended primarily as a technology demonstrator rather than a fully featured application.

## Platform Support

Hirameku is a [Jamstack](https://jamstack.org/) application that supports any capable host and API-compatible backend (cloud or on-prem). All development is done locally using the following:

- .NET >=6
- Latest NodeJS LTS release
- Astro >= 2.5*
- React >= 18
- TypeScript >=5.0
- Docker Desktop >=4
- Redis ~6.0
- MongoDB ~4.0
- smtp4dev
- Visual Studio 2022
- Visual Studio Code

> *Since [Astro requires 'unsafe-inline'](https://docs.astro.build/en/guides/troubleshooting/#refused-to-execute-inline-script) (thus defeating much of the benefit of CSP), its use will be retired. The client will be ported to a React/NextJS statically generated solution.

AWS, Azure, and Google Cloud are all planned to be supported as cloud hosts, using the following services:

- AWS S3/Azure Storage/Google Cloud Storage
- AWS CloudFront/Azure CDN/Google Cloud CDN
- AWS API Gateway/Azure API Management/Google Cloud API Gateway
- AWS Lambda/Azure Functions/Google Cloud Functions
- AWS SQS/Azure Service Bus/Google Pub/Sub
- AWS ElastiCache/Azure Cache/Memorystore for Redis
- AWS DocumentDB/Azure Cosmos DB/MongoDB Atlas on Google Cloud

Deployments will be handled by [Pulumi](https://www.pulumi.com/) and the cloud vendors' respective CLI tools.

# Getting Started

To build and run Hirameku locally, you'll need:

- .NET >=6.x SDK
- NodeJS >=18
- Docker Desktop >= 4 (technically, you don't _have_ to use Docker for dev, but that's the supported environment)

Visual Studio is the recommend IDE; however, it's not required. Everything can be built, configured, and run from the command line, and you're free to use whatever editor you prefer. The primary author typically uses Visual Studio Community Edition (its licensing terms permit its use for open source projects like Hirameku) for the .NET solution and Visual Studio Code for the web client.

To set up your local dev environment, do the following:

1. Clone the repo
2. Install the [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
3. If using Windows, [install WSL2](https://learn.microsoft.com/en-us/windows/wsl/install)
4. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or if you're on Linux, Docker Engine, if you prefer)
5. Enable Swarm mode in Docker (`docker swarm init`), as this is used to manage secrets
6. _Configuration instructions go here_ (TBD)
    - if you run into port conflicts, you can override them in a [docker-compose.override.yml](https://docs.docker.com/compose/extends/) file, which is intentionally `.gitignore`d
7.  Open `Hirameku.sln` and build it (or run `dotnet build` in a terminal)
8. `docker compose -f docker-compose.yml -f docker-compose.override.yml up`
    - omit `docker-compose.override.yml` if you're happy with the base file

To develop the client, you'll also need to install the [latest NodeJS LTS release](https://nodejs.org/). Change to the `client` directory and run `npm i` to install the packages. The client directory is included in the `docker-compose.yml` file mentioned above, so you don't have to do anything just to run the app.

## Useful Docker Scripts

To remove dangling images and volumes, use the following:

- `docker rmi $(docker images -f dangling=true -q)`
- `docker volume rm $(docker volume ls -f dangling=true -q)`

# Build and Test

Building and testing are as simple as `dotnet build` and `dotnet test`. Ensure you are in the root directory of the solution so MSBuild can find `Hirameku.sln`.

# Contribute

This repository is licensed [AGPL-3.0-or-later](https://spdx.org/licenses/AGPL-3.0-or-later.html) and all contributions are required to be licensed under the same. To contribute, clone the repo, make your commits, and submit a pull request. In your pull request comments, you must explicitly acknowledge and consent to the terms of the license for your contribution to be considered. Please also include a short description of your changes.

For contributions to be accepted, they must meet the following requirements:

- Build without errors
- Pass all tests
- Exhibit comprehensive test coverage
- Comply with .NET Analyzers at analysis level `Latest All`
- Comply with the latest v1.2.0-beta of StyleCop.Analzyers (the only exceptions allowed are those found in `stylecop.json`, which your contribution may not modify)
- Comply with the Visual Studio Code Cleanup rules defined in `.editorconfig` (for this reason, you are strongly encouraged to use Visual Studio to author contributions), which your contribution may not modify
- Use of `SuppressMessageAttribute` is permitted for demonstrable false positives or bugs
- Any new projects created as part of a contribution must be configured in accordance with the preceding

Contributions that do not meet these requirements may be summarily rejected.

In general, don't blaze a trail. Take the time to observe, learn, and follow the conventions and patterns established within the codebase and follow those. If you object to the way something's currently being done, open an issue for discussion before submitting your pull request.

## Rules for Success

1. Always do your best
2. Your best isn't good enough
3. Get better

# Notes

This section is used to keep running notes about the application. Most of these pertain to outstanding features.

## Enhancements

1. Add `registrationDate` to the `users` collection in the `identityDB` database
2. When a user logs in, validate whether their current password still meets password validity requirements. If it doesn't, allow the sign-in, but set their `UserStatus` to `PasswordChangeRequired`.
3. When saving a `PersistentToken`, include the date/time, user agent, IP, and geolocation information so this can later be relayed to the user on their dashboard to decide whether they want to delete one or more tokens
4. With the preceding as a prerequisite, provide users the ability to delete `PersistentToken`s
5. Integrate Polly into all API interactions (currently only implemented for HttpClient--also need it for Mongo, Redis, SMTP, and queues)
6. Add tracing support via OpenTelemetry and Jaeger (instead of logging traces via NLog)
    - see https://devblogs.microsoft.com/dotnet/observability-asp-net-core-apps/
    - use [Activities](https://github.com/dotnet/runtime/blob/4f9ae42d861fcb4be2fcd5d3d55d5f227d30e723/src/libraries/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md#overview) instead of CorrelationManager.ActivityId
    - correlate NLog messages using [NLog.DiagnosticSource](https://github.com/NLog/NLog.DiagnosticSource)
7. Add metrics collection, aggregation, and visualization via Prometheus and Grafana
8. (Once CardService is implemented) Event-integrate deletion of user accounts across IdentityService and CardService
9. (Once CardService is implemented) Provide a function whereby a user may request their account data (everything that isn't security-critical)
    - This will need to be an asynchronous backend job that creates an archive that is stored in a location only the user's account may access

> Consideration was given to [debouncing config file changes](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/change-tokens?view=aspnetcore-6.0#monitor-for-configuration-changes), but due to the nature of how the services will be deployed, it's unlikely we'd ever have to deal with a situation in which only the config files change (i.e. any config changes would result in a redeploy of the service)
> This also applies to `CommonServiceModule.GetPasswordBlacklist()`, which would use a `MemoryCache` to store the blacklisted passwords that is refreshed whenever the file changes on disk

## Protection Measures

The application doesn't use cookies. Instead, local storage is used for authentication tokens in order to prevent CSRFs.

`api.hirameku.app`
---

Access-Control-Allow-Origin: https://www.hirameku.app

`www.hirameku.app`
---

Security-related HTTP headers:

```
Cross-Origin-Resource-Policy: same-origin
Content Security Policy:
    connect-src 'self' https://*.api.hirameku.app https://*.google-analytics.com https://*.analytics.google.com https://*.googletagmanager.com;
    default-src 'self';
    frame-src: https://www.google.com/recaptcha/ https://recaptcha.google.com/recaptcha/ https://www.recaptcha.net/recaptcha/;
    font-src 'self' https://fonts.gstatic.com;
    img-src 'self' https://*.google-analytics.com https://*.googletagmanager.com https://www.gravatar.com;
    report-to csp-endpoint;
    require-trusted-types-for 'script';
    script-src 'self' https://*.google.com https://*.googletagmanager.com https://www.gstatic.com/recaptcha/ https://www.recaptcha.net/recaptcha/;
    trusted-types;
    upgrade-insecure-requests;
Report-To: {
    "group": "csp-endpoint",
    "max_age": 7776000,
    "endpoints": [
        "url": "https://api.hirameku.app/csp/reports"
    ]
}
Referrer-Policy: strict-origin-when-cross-origin
Strict-Transport-Security: max-age=31536000; preload
X-Content-Type-Options: nosniff
X-Frame-Options: sameorigin
```

Google recommends using CSP nonces for Recaptcha, but that's challenging to implement for a statically generated site. Using CSP hashes might be possible, but if Google ever updates the script, that'll break the hash. Recaptcha also works with `strict-dynamic`, which might be the best option.

### CSP Reports

```javascript
{
    "csp-report": {
        "blocked-uri": "",
        "column-number": 0,
        "disposition": "",
        "document-uri": "",
        "effective-directive": "enforce|report",
        "line-number": 0,
        "referrer": "",
        "script-sample": "",
        "source-file": "",
        "status-code": 0,
        "vioated-directive": ""
    }
}
```

## Authentication

Use JWT bearer tokens for authentication at the request level.

- `iss` (issuer)
- `sub` (subject of the claims - i.e. the user)
- `aud` (audience - i.e. the rp/sp - the app that wants the token)
- `exp` (expiration - time at which the token becomes invalid)
- `nbf` (not before - time at which the token becomes valid)
- `iat` (issued at - time the token was issued)
- `jti` (JWT ID - unique identifier)
- `https://www.hirameku.app/user_id` (UserId - the id of the user in the Hirameku Identity database)

## Authorization

Implement middleware to check UserStatus claim on authenticated requests
    - UserStatus.Suspended always returns 403
    - `UserStatus is not UserStatus.OK` returns 403 when not interacting with the IdentityService operations
    - the middleware should port to cloud FaaS platforms

## Databases

In keeping with microservices architectural best practices, each service has its own database:

- identityservice: `identityDB`
- cardservice: `cardDB`
- contactservice: `cspReportDB`

The collections for each database follow.

### cardDB

- cards
    /Meanings
- decks
- reviews

### cspReportDB

- cspReports

### identityDB

- authenticationEvents
- users
    /passwordHash
    /persistentTokens
- verifications

### authenticationEvents

An `authenticationEvent` document consists of the following properties:

- _id
- accept
- authenticationResult
- contentEncoding
- contentLanguage
- hash*
- remoteIP
- userAgent
- user_id

*The `hash` and `user_id` properties are indexed

### cards

A `card` document consists of the following properties:

- _id
- creationDate
- expression*
- meanings/
    - example*
    - hint*
    - text*
- notes*
- reading*
- tags*

*The `expression`, `meanings/example`, `meanings/hint`, `meanings/text`, `notes`, `reading`, and `tags` properties are text indexed (to support searching)

### cspReports

A `cspReport` document consists of the following properties:

- blockedUri
- columnNumber
- disposition
- documentUri
- effectiveDirective
- lineNumber
- referrer
- scriptSample
- sourceFile
- statusCode
- vioatedDirective

### decks

A `deck` document consists of the following properties:

- _id
- cards
- creationDate
- name
- user_id*

*The `user_id` property is indexed

### reviews

A `review` document consists of the following properties:

- _id
- card_id*
- disposition
- interval
- reviewDate
- user_id*

*The `card_id` and `user_id` properties are indexed

### users

A `user` document consists of the following properties:

- _id
- emailAddress*
- name
- passwordHash/
    expirationDate
    hash
    lastChangeDate
    salt
    version
- persistentTokens/
    clientId*
    expirationDate
    hash
- userName*
- userStatus

*The `emailAddress`, `persistentTokens/clientId`, and `userName` properties have unique indexes

### verifications

- _id
- creationDate
- emailAddress*
- expirationDate
- salt
- type
- user_id*

*The `emailAddress` and `user_id` properties are indexed

## Services

The ASP.NET Core WebAPI services include minimal Swagger documentation when run locally.

## Client

The client is implemented as a statically generated site using Astro with React components.

### Pages/Routes

- /
- /about
- /contact
- /forgot-password
- /privacy
- /register
- /resend-verification-email
- /reset-password
- /sign-in
- /verify-email
