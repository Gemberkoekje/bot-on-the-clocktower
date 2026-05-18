# 02-cpm-setup: Set up Central Package Management

Create `Directory.Packages.props` at the repository root and migrate all `PackageReference` version attributes out of the individual `.csproj` files. This makes package versions consistent and centrally managed across all 11 projects before the version bump happens.

All projects are already SDK-style and on the same ecosystem, making this the right moment to introduce CPM cleanly. The current packages span 13 distinct entries across the solution. After this task, `.csproj` files should contain `<PackageReference Include="..." />` entries without `Version` attributes, and all versions are declared in `Directory.Packages.props`.

**Done when**: `Directory.Packages.props` exists at the repo root with all package versions declared; all `.csproj` files have `PackageReference` entries without `Version` attributes; `dotnet restore` succeeds and the solution builds on net6.0 (no regression).
