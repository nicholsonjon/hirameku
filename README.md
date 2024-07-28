# Introduction 

Hirameku is a cloud-native, vendor-agnostic, serverless application for studying flashcards with support for localization and accessibility. It is intended primarily as a technology demonstrator rather than a fully featured application.

## Platform Support

Hirameku is a [Jamstack](https://jamstack.org/) application that supports any capable host and API-compatible backend (cloud or on-prem). All development is done locally using the following:

- [.NET ~8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [NodeJS ~20](https://nodejs.org/en/download)
- [Astro ~2.5](https://astro.build/)*
- [React >=18](https://react.dev/)
- [TypeScript >=5.0](https://www.typescriptlang.org/)
- [Docker Desktop >=4](https://www.docker.com/products/docker-desktop/) (or Docker Engine)
- [NGINX ~1.25](https://nginx.org/)
- [RabbitMQ ~3.12**](https://www.rabbitmq.com/)
- [Redis ~6.0](https://redis.io/)
- [MongoDB ~4.0](https://www.mongodb.com/)
- [smtp4dev](https://github.com/rnwood/smtp4dev)
- [Seq](https://datalust.co/seq)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)
- [Visual Studio Code](https://code.visualstudio.com/)

> *Since [Astro requires `unsafe-inline`](https://docs.astro.build/en/guides/troubleshooting/#refused-to-execute-inline-script) (thus defeating much of the benefit of CSP), it will be replaced with an alternative. The intent was to use [NextJS](https://nextjs.org/) and [React Bootstrap](https://react-bootstrap.github.io/) with [INLINE_RUNTIME_CHUNK=false](https://create-react-app.dev/docs/advanced-configuration/), but I had no end of problems with that, including mysterious build errors and components inexplicably not loading (but also not producing errors). I was going to switch to [MUI](https://mui.com/), but it requires [server-side rendering to support CSP](https://mui.com/material-ui/guides/content-security-policy/), which was not the original objective. Chakra UI also [requires `unsafe-inline`](https://github.com/chakra-ui/chakra-ui/issues/3294).
> 
> Supporting nonces at all implies server-side rendering, since the nonce has to be unique for every request. That leaves hashes as the only possible solution for inline scripts, but that also requires the deployment process to update the HTTP response headers (or `<meta>` tag). While that's feasible, I haven't found anything that natively supports it. It's pretty frustrating how poorly supported CSP is in the wider React ecosystem. We have this fantastic security tool at our disposal and people are just ignoring it or only supporting it as an afterthought.
>
> [Angular also requires nonces](https://angular.io/guide/security), meaning no static site generation. The [VueJS](https://vuejs.org/) runtime appears to be [CSP-compliant](https://v2.vuejs.org/v2/guide/installation.html#CSP-environments) and [Nuxt](https://nuxt.com/) can handle SSG and [CSP `<meta>` tag generation](https://nuxt-security.vercel.app/documentation/headers/csp#static-site-generation-ssg). [SvelteKit](https://kit.svelte.dev/) also [supports CSP hashes](https://kit.svelte.dev/docs/configuration#csp). I've been wanting to use Svelte because it's so slick, but the lack of jobs for it has made me hesitant. I think this is one of those cases where doing the right thing in terms of security should outweigh other factors. Using a CSP without restoring to `unsafe-inline` should be assumed for apps on the modern Internet. Since I'm loathe to abandon SSG for cost and performance reasons, that means either VueJS or Svelte. I'll try them both out and then port over what little of the client I have to whatever I decide. For now, I've removed the client entirely from the codebase until a decision about the frontend technology has been made.

> **RabbitMQ will be used to implement eventual consistency among the microservices. Locally, it stands in for cloud-equivalent services like [SNS](https://aws.amazon.com/sns/), [Service Bus](https://azure.microsoft.com/en-us/products/service-bus/), and [Pub/Sub](https://cloud.google.com/pubsub/).

AWS, Azure, and Google Cloud are all planned to be supported as cloud hosts, using the following services:

- AWS S3/Azure Storage/Google Cloud Storage
- AWS CloudFront/Azure CDN/Google Cloud CDN
- AWS API Gateway/Azure API Management/Google Cloud API Gateway
- AWS Lambda/Azure Functions/Google Cloud Functions
- AWS SNS/Azure Service Bus/Google Pub/Sub
- AWS ElastiCache/Azure Cache/Memorystore for Redis
- AWS DocumentDB/Azure Cosmos DB/MongoDB Atlas on Google Cloud

AWS will be supported first, with Azure also being given priority. Google Cloud will be a best effort case.

Deployments are planned to be handled by [Pulumi](https://www.pulumi.com/) and the cloud vendors' respective CLI tools.

# Getting Started

To build and run Hirameku locally, you'll need:

- .NET >=8 SDK
- NodeJS >=20
- Docker Desktop >= 4 (technically, you don't _have_ to use Docker for dev, but that's the supported environment)

Visual Studio is the recommend IDE; however, it's not required. Everything can be built, configured, and run from the command line, and you're free to use whatever editor you prefer. The primary author typically uses Visual Studio Community Edition (its licensing terms permit its use for open source projects like Hirameku) for the .NET solution and Visual Studio Code for the web client.

To set up your local dev environment, do the following:

1. Clone the repo
2. Install the [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
3. Install the latest [LTS release of NodeJS](https://nodejs.org/en/download)
4. If using Windows, [install WSL2](https://learn.microsoft.com/en-us/windows/wsl/install)
5. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or if you're on Linux, Docker Engine, if you prefer)
6. Set up your local environment parameters by consulting the [Configuration](#Configuration) section below
7. `docker compose -f docker-compose.yml -f docker-compose.override.yml up`
    - omit `docker-compose.override.yml` if you're happy with the base file

To develop the client, change to the `client` directory and run `npm i` to install the packages. The client directory is included in the `docker-compose.yml` file mentioned above, so you shouldn't have to do anything just to run the app. Simply open the `client` directory in Visual Studio Code to get started. The [astro-build.astro-vscode](https://marketplace.visualstudio.com/items?itemName=astro-build.astro-vscode) and [ms-azuretools.vscode-docker](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-docker) extensions are recommended for development. Use of [ms-vscode-remote.remote-containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) for dev containers is a also a planned future enhancement.

If you didn't modify it by means of the `docker-compose.override.yml` file, the application should be accessible at [http://localhost/](http://localhost/) once your containers are running.

## Configuration

While most elements of the system come with reasonable defaults, things like API keys, usernames, passwords, and other such confidential or authenticating details are intentionally omitted from the config files. You must supply these in override files in order to run the system. Do not modify the base files (i.e. any files that are aren't `.gitignore`d).

### Create `appsettings.Development.json` files

Because the services require configuration that varies by the hosting environment, you will need to specify things as applicable to your workstation when developing. These configuration details should be specified in the `appsettings.Development.json` file for each service. Do not modify the base `appsettings.json` file. All configuration settings may be overriden in `appsettings.Development.json`, even ones already specified in the base files. You can also specify/override configuration settings using environment variables. See [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-6.0) for more information.

For ContactService, a basic `appsettings.Development.json` would look something like this:

```json
{
  "Hirameku": {
    "Email": {
      "EmailerOptions": {
        "FeedbackEmailAddress": "feedback@localhost",
        "RejectRegistrationUrl": "http://localhost/reject-registration",
        "ResetPasswordUrl": "http://localhost/reset-password",
        "Sender": "noreply@localhost",
        "SmtpCredentials": {
          "Password": null,
          "UserName": null
        },
        "SmtpPort": "25",
        "SmtpServer": "smtpserver",
        "UseTls": false,
        "VerifyEmailUrl": "http://localhost/verify-email"
      }
    },
    "Recaptcha": {
      "RecaptchaOptions": {
        "ExpectedHostname": "localhost",
        "SiteSecret": "Overridden by secrets.json"
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Trace",
      "Microsoft": "Debug"
    }
  }
}
```

For IdentityService, you can use the following:

```json
{
  "Hirameku": {
    "Caching": {
      "CacheOptions:ConnectionString": "cache:6379"
    },
    "Common": {
      "Service": {
        "PasswordValidatorOptions:PasswordBlacklistPath": "/etc/hirameku/password-blacklist.txt",
        "SecurityTokenOptions": {
          "Audience": "http://localhost/",
          "Issuer": "http://localhost/",
          "SecretKey": "Overridden by secrets.json"
        }
      }
    },
    "Data": {
      "DatabaseOptions:ConnectionString": "mongodb://database:27017/"
    },
    "Email": {
      "EmailerOptions": {
        "RejectRegistrationUrl": "http://localhost/reject-registration/",
        "ResetPasswordUrl": "http://localhost/reset-password/",
        "Sender": "noreply@localhost",
        "SmtpCredentials": {
          "Password": null,
          "UserName": null
        },
        "SmtpPort": "25",
        "SmtpServer": "smtpserver",
        "UseTls": false,
        "VerifyEmailUrl": "http://localhost/verify-email/"
      }
    },
    "Recaptcha": {
      "RecaptchaOptions": {
        "ExpectedHostname": "localhost",
        "SiteSecret": "Overridden by secrets.json"
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Trace",
      "Microsoft": "Debug"
    }
  }
}
```

### Generate a reCAPTCHA site key

- [Generate a reCAPTCHA site key and secret](https://www.google.com/recaptcha/admin/create)
- Enter the site key into the `Hirameku:Recaptcha:RecaptchaOptions:SiteKey` property of `appsettings.Development.json` in both ContactService and IdentityService
- Enter the secret key into your [Secrets.json file](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-6.0) for both ContactService and IdentityService

### Docker

In the `/docker` directory, create a [docker-compose.override.yml](https://docs.docker.com/compose/extends/) file. Add the following contents:

```yml
services:
  client:
    environment:
      - PUBLIC_RECAPTCHA_V3_SITE_KEY=<your_recaptcha_site_key>
```

Replace the `<your_recaptcha_site_key>` token with the site key you generated.

Additionally, If you run into port conflicts, you can override them in `docker-compose.override.yml`, which is intentionally `.gitignore`d. Follow the link above for more details.

### Useful Docker Scripts

To remove dangling images and volumes, use the following:

- `docker rmi $(docker images -qf dangling=true)`
- `docker volume rm $(docker volume ls -f dangling=true -q)`

To rebuild an individual container:

- `docker compose up -d --no-deps --build <service_name>`

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
6. Add tracing support via OpenTelemetry and Jaeger
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

> The following CSP uses origin whitelisting, which is discouraged due to the ability to bypass such restrictions (e.g. via JSONP interfaces). The Chrome Lighthouse docs have more [information about this vulnerability](https://developer.chrome.com/docs/lighthouse/best-practices/csp-xss/#csp_uses_nonces_or_hashes_to_avoid_allowlist_bypasses).
>
> Google recommends using CSP nonces for its services (Analytics, Recaptcha, etc.), but that's challenging to implement for a statically generated site. Using CSP hashes might be possible, but if Google ever updates the scripts, that'll break the hashes.

```txt
Cross-Origin-Resource-Policy: same-origin
Content Security Policy:
    connect-src 'self' https://api.hirameku.app https://*.google-analytics.com https://*.analytics.google.com https://*.googletagmanager.com;
    default-src 'none';
    frame-src https://www.google.com/recaptcha/ https://recaptcha.google.com/recaptcha/;
    font-src 'self' https://fonts.gstatic.com;
    img-src 'self' data: https://*.google-analytics.com https://*.googletagmanager.com https://www.gravatar.com https://www.gstatic.com/recaptcha/;
    manifest-src 'self';
    script-src 'self' https://*.googletagmanager.com https://www.google.com/recaptcha/ https://www.gstatic.com/recaptcha/;
    style-src 'self';
Referrer-Policy: strict-origin-when-cross-origin
Strict-Transport-Security: max-age=31536000; preload
X-Content-Type-Options: nosniff
X-Frame-Options: deny
X-XSS-Protection: 0
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

- `UserStatus.Suspended` always returns 403
- `UserStatus is not UserStatus.OK` returns 403 when not interacting with the IdentityService operations
- the middleware must port to cloud FaaS platforms (i.e. it cannot not depend on ASP.NET Core Web API as the host)

## Databases

In keeping with microservices architectural best practices, each service has its own database:

- identityservice: `identityDB`
- cardservice: `cardDB`

The collections for each database follow.

### cardDB

- cards
    /Meanings
- decks
- reviews

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
- /resend-verification
- /reset-password
- /sign-in
- /verify-email
