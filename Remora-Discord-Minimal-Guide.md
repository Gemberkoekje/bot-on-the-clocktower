# Remora Discord Implementation (How it works + Minimal command registration)

## How your current Remora setup works

In this project, the flow is:

1. `Program.cs` starts first.
2. It does pod-claim logic (`IPodClaimService`) to ensure one active bot instance.
3. Once claimed, it builds the real host using `MyHostBuilder.CreateHostBuilder(args, podId)`.
4. In `HostBuilder.cs`, Discord + commands are wired:
   - `AddDiscordService(...)` provides the bot token.
   - `AddDiscordCommands(true).AddCommandTree()` enables slash command support.
   - All `CommandGroup` types are discovered by reflection and registered with `WithCommandGroup<T>()`.
5. Command classes (like `InfoCommand`) inherit `CommandGroup`, and methods marked with `[Command("...")]` become slash commands.
6. Gateway responders are registered using `AddResponder<...>()`.

---

## Minimal Remora Discord example (start to finish)

### 1) Create Worker project (optional)

```powershell
dotnet new worker -n ArmaBotMinimal
cd ArmaBotMinimal
```

### 2) Add NuGet packages

In `ArmaBotMinimal.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
	<TargetFramework>net9.0</TargetFramework>
	<Nullable>enable</Nullable>
	<ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
	<PackageReference Include="Remora.Discord.Hosting" Version="*" />
	<PackageReference Include="Remora.Discord.Gateway" Version="*" />
	<PackageReference Include="Remora.Discord.Commands" Version="*" />
	<PackageReference Include="Remora.Commands" Version="*" />
  </ItemGroup>
</Project>
```

### 3) Add configuration

`appsettings.json`:

```json
{
  "Discord": {
	"Token": "PUT_YOUR_BOT_TOKEN_HERE"
  }
}
```

### 4) Add a command group

Create `Commands/PingCommand.cs`:

```csharp
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ArmaBotMinimal.Commands;

public sealed class PingCommand(FeedbackService feedback) : CommandGroup
{
	[Command("ping")]
	[Description("Replies with pong.")]
	public async Task<IResult> HandlePingAsync()
		=> (Result)await feedback.SendContextualAsync("pong");
}
```

### 5) Wire Remora + register command

Replace `Program.cs` with:

```csharp
using ArmaBotMinimal.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Hosting.Extensions;

var host = Host.CreateDefaultBuilder(args)
	.AddDiscordService(services =>
	{
		var config = services.GetRequiredService<IConfiguration>();
		return config["Discord:Token"] ?? throw new InvalidOperationException("Discord token missing.");
	})
	.ConfigureServices((context, services) =>
	{
		services
			.AddDiscordCommands(true)
			.AddCommandTree()
			.WithCommandGroup<PingCommand>();
	})
	.Build();

await host.RunAsync();
```

### 6) Run

```powershell
dotnet run
```

Then use `/ping` in Discord.

---

## Notes

- `AddDiscordCommands(true)` means Remora should register/update application commands.
- If commands don’t appear immediately, re-invite bot with `applications.commands` scope and wait a short while (global commands may take time).
- For faster iteration, you can use guild-scoped command registration patterns later.

---

## Optional: dynamic registration (like your current project)

Your current project scans the assembly for all classes inheriting `CommandGroup` and registers each one at startup. This avoids manually listing `.WithCommandGroup<...>()` for every command class.
