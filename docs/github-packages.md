# Private GitHub Packages

Package ID: `SmartEnum`  
Feed: `https://nuget.pkg.github.com/deividsdukalskis/index.json`  
Repository: `https://github.com/deividsdukalskis/SmartEnum`

## Publish a version

1. Commit and push the package metadata, this documentation, and
   `.github/workflows/publish-nuget.yml` to the repository's default branch.
2. Open **Actions → Publish NuGet → Run workflow** on GitHub.
3. Select the intended branch and enter a new version, starting with `0.1.0`.
4. The workflow runs `Verify.ps1`, builds the release package, and publishes it.

The workflow uses the automatically supplied `GITHUB_TOKEN` with `packages:write`;
you do not need to create a publishing secret. It publishes only to GitHub
Packages. New packages default to private. After the first publish, confirm
**Package settings → Visibility: Private** and check the inherited repository
access permissions. Existing package visibility is not changed by publishing.

Each release needs a new version. Duplicate versions fail instead of silently
reusing an older package. Publishing is manual; normal pushes do not publish.

## Install in another project

Create a GitHub **personal access token (classic)** with `read:packages` under
**Settings → Developer settings → Personal access tokens → Tokens (classic)**.
The token's account must have permission to read the package. Use the token only
on your computer or in a secret store; do not put it in source control or chat.

Add this `NuGet.Config` beside the consuming solution. It contains no credentials:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github-smartenum" value="https://nuget.pkg.github.com/deividsdukalskis/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="github-smartenum">
      <package pattern="SmartEnum" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

The exact package-name mapping routes `SmartEnum` to your private feed; other
packages use nuget.org. Merge these entries with any existing company feeds.

In PowerShell 7, authenticate for the current terminal session using a hidden
prompt, then install the package:

```powershell
$packageToken = Read-Host 'GitHub classic token (read:packages)' -MaskInput
[Environment]::SetEnvironmentVariable(
    'NuGetPackageSourceCredentials_github-smartenum',
    "Username=deividsdukalskis;Password=$packageToken;ValidAuthenticationTypes=Basic",
    'Process')
Remove-Variable packageToken

dotnet add path/to/YourProject.csproj package SmartEnum --version 0.1.0
```

The credential environment variable must match the source name exactly. These
credentials last only for this terminal and processes launched from it. Visual
Studio launched separately needs its own feed authentication. Once installed,
the analyzer and generator run during builds; the `SmartEnum` namespace stays
the same. Use `PrivateAssets="all"` on the package reference if you pack your
consumer library and do not want to propagate this build-time dependency.

## Restore from another repository's GitHub Actions

Under the package's **Manage Actions access**, grant the consuming repository
read access. Its workflow can then use `GITHUB_TOKEN` with `packages:read`:

```yaml
permissions:
  contents: read
  packages: read

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v7
      - uses: actions/setup-dotnet@v6
        with:
          dotnet-version: '10.0.401'
      - name: Restore
        env:
          NuGetPackageSourceCredentials_github-smartenum: Username=${{ github.actor }};Password=${{ secrets.GITHUB_TOKEN }};ValidAuthenticationTypes=Basic
        run: dotnet restore
```

Without that repository grant, use a classic `read:packages` token saved as a
GitHub Actions secret instead. Consumer compiler hosts require Roslyn 5.6 or
newer; the publishing workflow uses the tested .NET SDK 10.0.401.

References: [GitHub NuGet registry](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry),
[NuGet feed authentication](https://learn.microsoft.com/en-us/nuget/consume-packages/consuming-packages-authenticated-feeds).
