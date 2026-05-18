# 01-prerequisites: Verify SDK and toolchain readiness

Confirm that the .NET 10 SDK is installed and that any `global.json` files in the repository are compatible with the target SDK. This is a non-destructive verification step that must pass before any project file changes are made.

The repository is currently on net6.0. Any `global.json` pinning an SDK version older than the .NET 10 range will need to be updated so the toolchain resolves correctly after the TFM bump.

**Done when**: .NET 10 SDK is confirmed installed; any `global.json` in the repo is updated to allow .NET 10 SDK resolution; no build or restore failures caused by toolchain mismatch.
