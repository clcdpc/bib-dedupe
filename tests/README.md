# Testing the Bib Dedupe solution

This repository contains an MSTest v2 suite under `tests/Clc.BibDedupe.Web.Tests` that exercises the web application.

## Prerequisites

1. Install the .NET SDK (8.0 or later). In restricted environments you can use the official install script:
   ```bash
   curl -L https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
   bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "$HOME/dotnet"
   export PATH="$HOME/dotnet:$PATH"
   ```
2. Restore dependencies (handled automatically by `dotnet test`).

## Running the Tests

Execute the solution-level test command:

```bash
dotnet test src/Clc.BibDedupe.sln
```

## Recent Test Run

The latest validation run (executed during this change) reported:

```
Passed!  - Failed:     0, Passed:    35, Skipped:     0, Total:    35
```

