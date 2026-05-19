using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;

namespace Bot.Remora
{
    public sealed class RemoraCommandRegistrar : IRemoraCommandRegistrar
    {
        private readonly IDiscordRestApplicationAPI m_applicationApi;
        private readonly IDiscordRestGuildAPI? m_guildApi;

        public RemoraCommandRegistrar(IDiscordRestApplicationAPI applicationApi, IDiscordRestGuildAPI? guildApi = null)
        {
            m_applicationApi = applicationApi;
            m_guildApi = guildApi;
        }

        public async Task RegisterGlobalCommandsAsync(IReadOnlyCollection<IRemoraSlashCommand> commands)
        {
            Snowflake applicationId = await GetApplicationIdAsync();
            IReadOnlyList<IBulkApplicationCommandData> payload = ToBulkCommands(commands);
            string commandList = FormatCommandList(commands);

            Console.WriteLine($"RemoraCommandRegistrar: Starting global slash command registration. ApplicationId={applicationId.Value}, CommandCount={payload.Count}, Commands=[{commandList}].");
            IResult result = await m_applicationApi.BulkOverwriteGlobalApplicationCommandsAsync(applicationId, payload, CancellationToken.None);
            EnsureSuccess(result, "Failed to register global slash commands.", payload);

            Console.WriteLine($"RemoraCommandRegistrar: Completed global slash command registration. ApplicationId={applicationId.Value}, CommandCount={payload.Count}, Commands=[{commandList}].");
        }

        public async Task RegisterGuildCommandsAsync(IReadOnlyCollection<ulong> guildIds, IReadOnlyCollection<IRemoraSlashCommand> commands)
        {
            Snowflake applicationId = await GetApplicationIdAsync();
            IReadOnlyList<IBulkApplicationCommandData> payload = ToBulkCommands(commands);
            string commandList = FormatCommandList(commands);

            Console.WriteLine($"RemoraCommandRegistrar: Starting guild slash command registration batch. ApplicationId={applicationId.Value}, GuildCount={guildIds.Count}, CommandCount={payload.Count}, Commands=[{commandList}].");

            int guildIndex = 0;
            foreach (ulong guildId in guildIds)
            {
                guildIndex++;
                string guildDisplay = await GetGuildDisplayAsync(guildId);
                Console.WriteLine($"RemoraCommandRegistrar: Registering slash commands for guild {guildDisplay} ({guildIndex}/{guildIds.Count}). ApplicationId={applicationId.Value}, CommandCount={payload.Count}.");
                IResult result = await m_applicationApi.BulkOverwriteGuildApplicationCommandsAsync(applicationId, new Snowflake(guildId), payload, CancellationToken.None);
                EnsureSuccess(result, $"Failed to register slash commands for guild {guildDisplay}.", payload);
                Console.WriteLine($"RemoraCommandRegistrar: Successfully registered slash commands for guild {guildDisplay} ({guildIndex}/{guildIds.Count}). ApplicationId={applicationId.Value}, CommandCount={payload.Count}.");
            }

            Console.WriteLine($"RemoraCommandRegistrar: Completed guild slash command registration batch. ApplicationId={applicationId.Value}, GuildCount={guildIds.Count}, CommandCount={payload.Count}.");
        }

        public async Task ClearGuildCommandsAsync(IReadOnlyCollection<ulong> guildIds)
        {
            Snowflake applicationId = await GetApplicationIdAsync();
            IReadOnlyList<IBulkApplicationCommandData> empty = Array.Empty<IBulkApplicationCommandData>();

            Console.WriteLine($"RemoraCommandRegistrar: Starting guild slash command clear batch. ApplicationId={applicationId.Value}, GuildCount={guildIds.Count}.");

            int guildIndex = 0;
            foreach (ulong guildId in guildIds)
            {
                guildIndex++;
                string guildDisplay = await GetGuildDisplayAsync(guildId);
                Console.WriteLine($"RemoraCommandRegistrar: Clearing slash commands for guild {guildDisplay} ({guildIndex}/{guildIds.Count}). ApplicationId={applicationId.Value}.");
                IResult result = await m_applicationApi.BulkOverwriteGuildApplicationCommandsAsync(applicationId, new Snowflake(guildId), empty, CancellationToken.None);
                EnsureSuccess(result, $"Failed to clear slash commands for guild {guildDisplay}.", empty);
                Console.WriteLine($"RemoraCommandRegistrar: Successfully cleared slash commands for guild {guildDisplay} ({guildIndex}/{guildIds.Count}). ApplicationId={applicationId.Value}.");
            }

            Console.WriteLine($"RemoraCommandRegistrar: Completed guild slash command clear batch. ApplicationId={applicationId.Value}, GuildCount={guildIds.Count}.");
        }

        private async Task<Snowflake> GetApplicationIdAsync()
        {
            Result<IApplication> appResult = await m_applicationApi.GetCurrentApplicationAsync(CancellationToken.None);
            EnsureSuccess(appResult, "Failed to fetch current Discord application details.", formBody: null);
            return appResult.Entity.ID;
        }

        private async Task<string> GetGuildDisplayAsync(ulong guildId)
        {
            if (m_guildApi is null)
            {
                return $"{guildId} (name unavailable: guild API not available)";
            }

            Result<IGuild> guildResult = await m_guildApi.GetGuildAsync(new Snowflake(guildId), default, CancellationToken.None);
            if (!guildResult.IsSuccess)
            {
                return $"{guildId} (name unavailable: {guildResult.Error})";
            }

            return $"{guildId} ('{guildResult.Entity.Name}')";
        }

        private static IReadOnlyList<IBulkApplicationCommandData> ToBulkCommands(IReadOnlyCollection<IRemoraSlashCommand> commands)
        {
            return commands
                .Select(ToBulkCommand)
                .Cast<IBulkApplicationCommandData>()
                .ToArray();
        }

        private static BulkApplicationCommandData ToBulkCommand(IRemoraSlashCommand command)
        {
            IReadOnlyList<IApplicationCommandOption> options = command.Parameters
                .Select(ToOption)
                .Cast<IApplicationCommandOption>()
                .ToArray();

            Optional<IReadOnlyList<IApplicationCommandOption>> optionValue = options.Count > 0
                ? new Optional<IReadOnlyList<IApplicationCommandOption>>(options)
                : default;

            return new BulkApplicationCommandData(
                NormalizeDiscordName(command.Name),
                NormalizeDiscordDescription(command.Description),
                default,
                optionValue,
                new Optional<ApplicationCommandType>(ApplicationCommandType.ChatInput),
                default,
                default,
                new DiscordPermissionSet(Array.Empty<DiscordPermission>()),
                default,
                default,
                default,
                default);
        }

        private static ApplicationCommandOption ToOption(RemoraSlashCommandParameter parameter)
        {
            return new ApplicationCommandOption(
                MapParameterType(parameter.ParameterType),
                NormalizeDiscordName(parameter.Name),
                NormalizeDiscordDescription(parameter.Description),
                default,
                new Optional<bool>(parameter.IsRequired));
        }

        private static ApplicationCommandOptionType MapParameterType(RemoraSlashCommandParameterType parameterType)
        {
            return parameterType switch
            {
                RemoraSlashCommandParameterType.String => ApplicationCommandOptionType.String,
                RemoraSlashCommandParameterType.Boolean => ApplicationCommandOptionType.Boolean,
                RemoraSlashCommandParameterType.Integer => ApplicationCommandOptionType.Integer,
                RemoraSlashCommandParameterType.User => ApplicationCommandOptionType.User,
                RemoraSlashCommandParameterType.Role => ApplicationCommandOptionType.Role,
                RemoraSlashCommandParameterType.Channel => ApplicationCommandOptionType.Channel,
                _ => ApplicationCommandOptionType.String,
            };
        }

        private static string FormatCommandList(IReadOnlyCollection<IRemoraSlashCommand> commands)
        {
            return string.Join(", ", commands
                .Select(c => NormalizeDiscordName(c.Name))
                .OrderBy(n => n, StringComparer.Ordinal));
        }

        private static string NormalizeDiscordName(string value)
        {
            return value.ToLowerInvariant();
        }

        private static string NormalizeDiscordDescription(string value)
        {
            const int maxLength = 100;

            string cleaned = (value ?? string.Empty).Trim();
            if (cleaned.Length == 0)
            {
                return "No description";
            }

            if (cleaned.Length <= maxLength)
            {
                return cleaned;
            }

            return $"{cleaned[..(maxLength - 3)]}...";
        }

        private static void EnsureSuccess(IResult result, string message, object? formBody)
        {
            if (result.IsSuccess)
            {
                return;
            }

            string errorText = result.Error?.ToString() ?? "<null>";
            string errorJson = SerializeForLog(result.Error);
            string fieldErrors = ExtractDiscordFieldErrors(result.Error);
            string formBodyJson = SerializeForLog(formBody);

            Console.Error.WriteLine($"RemoraCommandRegistrar: Discord command registration failed. Message={message}");
            Console.Error.WriteLine($"RemoraCommandRegistrar: Discord error text: {errorText}");
            Console.Error.WriteLine($"RemoraCommandRegistrar: Discord error json: {errorJson}");
            Console.Error.WriteLine($"RemoraCommandRegistrar: Discord field-level validation errors: {fieldErrors}");
            Console.Error.WriteLine($"RemoraCommandRegistrar: Discord form body payload: {formBodyJson}");

            throw new InvalidOperationException($"{message} {errorText}");
        }

        private static string ExtractDiscordFieldErrors(object? resultError)
        {
            if (resultError is null)
            {
                return "<no error object>";
            }

            object? restError = GetPropertyValue(resultError, "Error") ?? resultError;
            object? errors = GetPropertyValue(restError, "Errors");

            if (errors is null)
            {
                return "<no field-level validation details available>";
            }

            return SerializeForLog(errors);
        }

        private static object? GetPropertyValue(object target, string propertyName)
        {
            var property = target.GetType().GetProperty(propertyName);
            if (property is null)
            {
                return null;
            }

            return property.GetValue(target);
        }

        private static string SerializeForLog(object? value)
        {
            if (value is null)
            {
                return "null";
            }

            try
            {
                object expanded = ExpandForLog(value, depth: 0);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                };
                return JsonSerializer.Serialize(expanded, options);
            }
            catch
            {
                return value.ToString() ?? "<unserializable>";
            }
        }

        private static object ExpandForLog(object value, int depth)
        {
            const int MaxDepth = 16;
            if (depth >= MaxDepth)
            {
                return "<max-depth>";
            }

            Type type = value.GetType();

            if (value is string || type.IsPrimitive || value is decimal || value is DateTime || value is DateTimeOffset || value is Guid || value is Enum)
            {
                return value;
            }

            if (value is Snowflake snowflake)
            {
                return snowflake.Value;
            }

            if (value is IEnumerable<IBulkApplicationCommandData> bulkCommands)
            {
                return bulkCommands.Select(cmd => new
                {
                    cmd.Name,
                    cmd.Description,
                    Type = cmd.Type.ToString(),
                    DefaultMemberPermissions = cmd.DefaultMemberPermissions.ToString(),
                    DMPermission = cmd.DMPermission.ToString(),
                    Options = cmd.Options.HasValue
                        ? cmd.Options.Value.Select(o => new
                        {
                            o.Name,
                            o.Description,
                            Type = o.Type.ToString(),
                            Required = SerializeForLog(o.IsRequired),
                            ChannelTypes = SerializeForLog(o.ChannelTypes),
                        }).ToArray()
                        : Array.Empty<object>(),
                }).ToArray();
            }

            if (value is IDictionary dictionary)
            {
                Dictionary<string, object?> mapped = new();
                foreach (DictionaryEntry entry in dictionary)
                {
                    string key = entry.Key?.ToString() ?? "<null>";
                    mapped[key] = entry.Value is null ? null : ExpandForLog(entry.Value, depth + 1);
                }
                return mapped;
            }

            if (value is IEnumerable enumerable)
            {
                List<object?> list = new();
                foreach (object? item in enumerable)
                {
                    list.Add(item is null ? null : ExpandForLog(item, depth + 1));
                }
                return list;
            }

            Dictionary<string, object?> props = new();
            foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                object? propValue;
                try
                {
                    propValue = prop.GetValue(value);
                }
                catch
                {
                    continue;
                }

                props[prop.Name] = propValue is null ? null : ExpandForLog(propValue, depth + 1);
            }

            if (props.Count == 0)
            {
                return value.ToString() ?? type.FullName ?? "<unknown>";
            }

            return props;
        }
    }
}
