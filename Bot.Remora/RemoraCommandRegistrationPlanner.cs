using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bot.Remora
{
    public sealed class RemoraCommandRegistrationPlanner
    {
        private static readonly ulong[] LegacyDefaultDevGuildIds =
        {
            128585855097896963ul,
            215551375608643586ul,
        };

        public RemoraCommandRegistrationPlan Build(IEnvironment environment)
        {
            string deployType = environment.GetEnvironmentVariable("DEPLOY_TYPE") ?? "none";
            if (!deployType.Equals("dev", StringComparison.Ordinal) && !deployType.Equals("prod", StringComparison.Ordinal))
            {
                throw new InvalidDeployTypeException(deployType);
            }

            List<ulong> devGuildIds = ReadDevGuildIds(environment);

            if (deployType.Equals("dev", StringComparison.Ordinal))
            {
                return new RemoraCommandRegistrationPlan(
                    deployType,
                    registerGlobalCommands: false,
                    clearDevGuildCommands: false,
                    devGuildIds: devGuildIds);
            }

            return new RemoraCommandRegistrationPlan(
                deployType,
                registerGlobalCommands: true,
                clearDevGuildCommands: true,
                devGuildIds: devGuildIds);
        }

        private static List<ulong> ReadDevGuildIds(IEnvironment environment)
        {
            string csv = environment.GetEnvironmentVariable("DISCORD_DEV_GUILD_IDS") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(csv))
            {
                List<ulong> parsedFromCsv = ParseCsvIds(csv);
                if (parsedFromCsv.Count > 0)
                {
                    return parsedFromCsv;
                }
            }

            List<ulong> indexed = ParseIndexedIds(environment);
            if (indexed.Count > 0)
            {
                return indexed;
            }

            return LegacyDefaultDevGuildIds.ToList();
        }

        private static List<ulong> ParseCsvIds(string csv)
        {
            List<ulong> values = new();
            foreach (string token in csv.Split(','))
            {
                string trimmed = token.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                if (ulong.TryParse(trimmed, out ulong guildId))
                {
                    values.Add(guildId);
                }
            }

            return values;
        }

        private static List<ulong> ParseIndexedIds(IEnvironment environment)
        {
            List<ulong> values = new();
            for (int i = 0; i < 16; i++)
            {
                string envName = $"Discord:DevGuildIds:{i}";
                string raw = environment.GetEnvironmentVariable(envName) ?? string.Empty;
                if (raw.Length == 0)
                {
                    continue;
                }

                if (ulong.TryParse(raw, out ulong guildId))
                {
                    values.Add(guildId);
                }
            }

            return values;
        }

        public sealed class InvalidDeployTypeException : Exception
        {
            public string DeployType { get; }

            public InvalidDeployTypeException(string deployType)
                : base($"Bot must be configured with either 'dev' or 'prod' DEPLOY_TYPE. Actual value: '{deployType}'.")
            {
                DeployType = deployType;
            }
        }
    }
}
