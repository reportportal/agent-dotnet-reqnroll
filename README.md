[![CI](https://github.com/reportportal/agent-dotnet-reqnroll/actions/workflows/ci.yml/badge.svg)](https://github.com/reportportal/agent-dotnet-reqnroll/actions/workflows/ci.yml)

# Installation
Install **ReportPortal.Reqnroll** NuGet package into your project with scenarios.

[![NuGet version](https://badge.fury.io/nu/reportportal.reqnroll.svg)](https://badge.fury.io/nu/reportportal.reqnroll)

> PS> Install-Package ReportPortal.Reqnroll

# Configuration
Add `ReportPortal.json` file into tests project with `Copy to Output Directory = Copy if newer` property.

Example of config file:
```json
{
  "$schema": "https://raw.githubusercontent.com/reportportal/agent-dotnet-reqnroll/master/src/ReportPortal.ReqnrollPlugin/ReportPortal.config.schema",
  "enabled": true,
  "server": {
    "url": "https://rp.epam.com",
    "project": "default_project",
    "apiKey": "7853c7a9-7f27-43ea-835a-cab01355fd17"
  },
  "launch": {
    "name": "Reqnroll Demo Launch",
    "description": "this is description",
    "debugMode": true,
    "attributes": [ "t1", "os:win10" ]
  }
}
```

Discover [more](https://github.com/reportportal/commons-net/blob/master/docs/Configuration.md) about configuration.


# Troubleshooting
All http error messages goes to `ReportPortal.*.log` file.

# Integrate logger framework
- [NLog](https://github.com/reportportal/logger-net-nlog)
- [log4net](https://github.com/reportportal/logger-net-log4net)
- [Serilog](https://github.com/reportportal/logger-net-serilog)
- [System.Diagnostics.TraceListener](https://github.com/reportportal/logger-net-tracelistener)

And [how](https://github.com/reportportal/commons-net/blob/master/docs/Logging.md) you can improve your logging experience with attachments or nested steps.


# Useful extensions
- [SourceBack](https://github.com/nvborisenko/reportportal-extensions-sourceback) adds piece of test code where test was failed
- [Insider](https://github.com/nvborisenko/reportportal-extensions-insider) brings more reporting capabilities without coding like methods invocation as nested steps


# License
ReportPortal is licensed under [Apache 2.0](https://github.com/reportportal/agent-dotnet-reqnroll/blob/master/LICENSE)

We use Google Analytics for sending anonymous usage information as library's name/version and the agent's name/version when starting launch. This information might help us to improve integration with ReportPortal. Used by the ReportPortal team only and not for sharing with 3rd parties. You are able to [turn off](https://github.com/reportportal/commons-net/blob/master/docs/Configuration.md#analytics) it if needed.
