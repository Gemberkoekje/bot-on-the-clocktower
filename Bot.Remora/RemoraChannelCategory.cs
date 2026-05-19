using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;

namespace Bot.Remora
{
    public class RemoraChannelCategory : IChannelCategory
    {
        public ulong Id { get; }

        public IReadOnlyCollection<IChannel> Channels => m_channels;

        public string Name { get; }

        public bool IsDeleted { get; private set; }

        private readonly List<IChannel> m_channels;
        private IDiscordRestChannelAPI? m_channelApi;

        private readonly Dictionary<ulong, (IBaseChannel.Permissions Allow, IBaseChannel.Permissions Deny)> m_memberOverwrites = new();
        private readonly Dictionary<ulong, (IBaseChannel.Permissions Allow, IBaseChannel.Permissions Deny)> m_roleOverwrites = new();

        public RemoraChannelCategory(ulong id, string name, IEnumerable<IChannel>? channels = null, IDiscordRestChannelAPI? channelApi = null)
        {
            Id = id;
            Name = name;
            m_channels = channels != null ? new List<IChannel>(channels) : new List<IChannel>();
            m_channelApi = channelApi;
        }

        internal void SetChannelApi(IDiscordRestChannelAPI channelApi)
        {
            m_channelApi = channelApi;
        }

        public async Task AddOverwriteAsync(IMember member, IBaseChannel.Permissions allow, IBaseChannel.Permissions deny = IBaseChannel.Permissions.None)
        {
            m_memberOverwrites[member.Id] = (allow, deny);
            if (m_channelApi is null)
            {
                return;
            }

            IResult result = await m_channelApi.EditChannelPermissionsAsync(
                new Snowflake(Id),
                new Snowflake(member.Id),
                new Optional<global::Remora.Discord.API.Abstractions.Objects.IDiscordPermissionSet?>(ToDiscordPermissionSet(allow)),
                new Optional<global::Remora.Discord.API.Abstractions.Objects.IDiscordPermissionSet?>(ToDiscordPermissionSet(deny)),
                new Optional<global::Remora.Discord.API.Abstractions.Objects.PermissionOverwriteType>(global::Remora.Discord.API.Abstractions.Objects.PermissionOverwriteType.Member),
                default,
                default).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to add member permission overwrite.");
        }

        public async Task AddOverwriteAsync(IRole role, IBaseChannel.Permissions allow, IBaseChannel.Permissions deny = IBaseChannel.Permissions.None)
        {
            m_roleOverwrites[role.Id] = (allow, deny);
            if (m_channelApi is null)
            {
                Console.WriteLine($"RemoraChannelCategory: AddOverwriteAsync skipped (no REST API). CategoryId={Id}, CategoryName='{Name}', RoleId={role.Id}, RoleName='{role.Name}', Allow={allow}, Deny={deny}");
                return;
            }

            try
            {
                IResult result = await m_channelApi.EditChannelPermissionsAsync(
                    new Snowflake(Id),
                    new Snowflake(role.Id),
                    new Optional<global::Remora.Discord.API.Abstractions.Objects.IDiscordPermissionSet?>(ToDiscordPermissionSet(allow)),
                    new Optional<global::Remora.Discord.API.Abstractions.Objects.IDiscordPermissionSet?>(ToDiscordPermissionSet(deny)),
                    new Optional<global::Remora.Discord.API.Abstractions.Objects.PermissionOverwriteType>(global::Remora.Discord.API.Abstractions.Objects.PermissionOverwriteType.Role),
                    default,
                    default).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    Console.WriteLine($"RemoraChannelCategory: AddOverwriteAsync succeeded. CategoryId={Id}, CategoryName='{Name}', RoleId={role.Id}, RoleName='{role.Name}', Allow={allow}, Deny={deny}");
                }
                EnsureSuccess(result, "Failed to add role permission overwrite.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RemoraChannelCategory: AddOverwriteAsync failed with exception. CategoryId={Id}, CategoryName='{Name}', RoleId={role.Id}, RoleName='{role.Name}', Exception={ex.GetType().Name}, Message={ex.Message}");
                throw;
            }
        }

        public async Task RemoveOverwriteAsync(IRole role)
        {
            m_roleOverwrites.Remove(role.Id);
            if (m_channelApi is null)
            {
                return;
            }

            IResult result = await m_channelApi.DeleteChannelPermissionAsync(new Snowflake(Id), new Snowflake(role.Id), default, default).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to remove role permission overwrite.");
        }

        public IChannel? GetChannelByName(string name)
        {
            return m_channels.FirstOrDefault(c => c.Name == name);
        }

        public void AddChannel(IChannel channel)
        {
            if (!m_channels.Contains(channel))
            {
                m_channels.Add(channel);
            }
        }

        public void RemoveChannel(IChannel channel)
        {
            m_channels.Remove(channel);
        }

        public async Task DeleteAsync(string? reason = null)
        {
            string reasonText = string.IsNullOrWhiteSpace(reason) ? "<none>" : reason;
            Console.WriteLine($"RemoraChannelCategory: delete requested. TargetType={GetType().FullName ?? GetType().Name}, Id={Id}, Name='{Name}', ChannelCount={m_channels.Count}, HasChannelApi={m_channelApi is not null}, Reason='{reasonText}'.");

            IsDeleted = true;
            if (m_channelApi is null)
            {
                Console.WriteLine($"RemoraChannelCategory: delete completed (no REST API available). TargetType={GetType().FullName ?? GetType().Name}, Id={Id}, Name='{Name}'.");
                return;
            }

            try
            {
                IResult result = await m_channelApi.DeleteChannelAsync(
                    new Snowflake(Id),
                    string.IsNullOrWhiteSpace(reason) ? default : new Optional<string>(reason),
                    default).ConfigureAwait(false);
                EnsureSuccess(result, "Failed to delete category.");
                Console.WriteLine($"RemoraChannelCategory: delete succeeded. TargetType={GetType().FullName ?? GetType().Name}, Id={Id}, Name='{Name}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RemoraChannelCategory: delete failed with exception. CategoryId={Id}, CategoryName='{Name}', Exception={ex.GetType().Name}, Message={ex.Message}");
                throw;
            }
        }

        private static DiscordPermissionSet ToDiscordPermissionSet(IBaseChannel.Permissions permissions)
        {
            ulong rawPermissions = Convert.ToUInt64(permissions);
            return new DiscordPermissionSet(new BigInteger(rawPermissions));
        }

        private static void EnsureSuccess(IResult result, string message)
        {
            if (result.IsSuccess)
            {
                return;
            }

            throw new InvalidOperationException($"{message}: {result.Error}");
        }
    }
}
