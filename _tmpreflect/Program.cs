using System; using Remora.Discord.API.Abstractions.Gateway.Commands; class P { static void Main() { foreach (var n in Enum.GetNames<GatewayIntents>()) Console.WriteLine(n); } }
