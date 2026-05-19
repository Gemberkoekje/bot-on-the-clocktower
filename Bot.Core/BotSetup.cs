using Bot.Api;
using Bot.Api.Database;
using Bot.Core.Interaction;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using static Bot.Api.IBaseChannel;

namespace Bot.Core
{
    public class BotSetup : IBotSetup
    {
        public IEnumerable<string> DefaultExtraDayChannels => new[] { "Dark Alley", "Library", "Graveyard", "Pie Shop" };

        private readonly ITownDatabase m_townDb;
        private readonly ITownResolver m_townResolver;
        private readonly IBotSystem m_botSystem;
        private readonly ICommandMetricDatabase m_commandMetricsDatabase;
        private readonly IDateTime m_dateTime;
        private readonly IGuildInteractionWrapper m_interactionWrapper;

        public BotSetup(
            ITownDatabase townDb,
            ITownResolver townResolver,
            IBotSystem botSystem,
            ICommandMetricDatabase commandMetricsDatabase,
            IDateTime dateTime,
            IGuildInteractionWrapper interactionWrapper)
        {
            m_townDb = townDb;
            m_townResolver = townResolver;
            m_botSystem = botSystem;
            m_commandMetricsDatabase = commandMetricsDatabase;
            m_dateTime = dateTime;
            m_interactionWrapper = interactionWrapper;
        }

        public Task AddTownAsync(IBotInteractionContext ctx, 
            IChannel controlChan, 
            IChannel townSquare, 
            IChannelCategory dayCat,
            IChannelCategory? nightCat,
            IRole stRole,
            IRole villagerRole,
            IChannel? chatChan) =>
            m_interactionWrapper.WrapInteractionAsync($"Adding town **townName**...", ctx,
                l => PerformAddTown(l, ctx, controlChan, townSquare, dayCat, nightCat, stRole, villagerRole, chatChan));

        public Task ModifyTownAsync(IBotInteractionContext ctx,
            IChannel? chatChannel,
            IChannelCategory? nightCat) =>
            m_interactionWrapper.WrapInteractionAsync($"Modifying town...", ctx,
                l => PerformModifyTown(l, ctx, chatChannel, nightCat));

        public Task CreateTownAsync(IBotInteractionContext ctx, 
            string townName, 
            IRole? guildPlayerRole, 
            IRole? guildStRole, 
            bool useNight) => 
            m_interactionWrapper.WrapInteractionAsync($"Creating town **{townName}**...", ctx, 
                l => PerformCreateTown(l, ctx, townName, guildPlayerRole, guildStRole, useNight));
        public Task TownInfoAsync(IBotInteractionContext ctx) => 
            m_interactionWrapper.WrapInteractionAsync($"Looking up town...", ctx, 
                l => PerformTownInfo(l, ctx));
        public Task DestroyTownAsync(IBotInteractionContext ctx, string townName) =>
            m_interactionWrapper.WrapInteractionAsync($"Destroying channels and roles for town **{townName}**...", ctx,
                l => PerformDestroyTown(l, ctx, townName));

        public Task RemoveTownAsync(IBotInteractionContext ctx, IChannelCategory? dayCat)
        {
            string msg = $"Removing town...";
            if(dayCat != null)
                msg = $"Removing town {dayCat.Name}...";

            return m_interactionWrapper.WrapInteractionAsync(msg, ctx,
                l => PerformRemoveTown(l, ctx, dayCat));
        }

        private async Task<InteractionResult> PerformModifyTown(IProcessLogger _, IBotInteractionContext ctx,
            IChannel? chatChannel,
            IChannelCategory? nightCat)
        {
            var townRecord = await m_townDb.GetTownRecordAsync(ctx.Guild.Id, ctx.Channel.Id);

            if (townRecord != null)
            {
                var town = await m_townResolver.ResolveTownAsync(townRecord);
                if (town != null)
                {

                    if (chatChannel != null)
                        town.ChatChannel = chatChannel;
                    if (nightCat != null)
                        town.NightCategory = nightCat;

                    await m_townDb.UpdateTownAsync(town);

                    var embed = EmbedFromTown(town, townRecord.Timestamp, townRecord.AuthorName ?? "unknown");
                    return InteractionResult.FromMessageAndEmbeds($"Updated town **{town.DayCategory?.Name ?? "unknown"}**!", embed);
                }
            }

            return InteractionResult.FromMessage("Couldn't find a valid town to modify associated with this channel. Are you in the control channel for the town to modify?");
        }

            private async Task<InteractionResult> PerformAddTown(IProcessLogger _, IBotInteractionContext ctx, 
            IChannel controlChan,
            IChannel townSquare,
            IChannelCategory dayCat,
            IChannelCategory? nightCat,
            IRole stRole,
            IRole villagerRole,
            IChannel? chatChan)
        {

            Town newTown = new()
            {
                Guild = ctx.Guild,
                ControlChannel = controlChan,
                TownSquare = townSquare,
                DayCategory = dayCat,
                NightCategory = nightCat,
                StorytellerRole = stRole,
                VillagerRole = villagerRole,
                ChatChannel = chatChan
            };

            await AddTown(newTown, ctx.Member);

            var embed = EmbedFromTown(newTown, m_dateTime.Now, ctx.Member.DisplayName);
            return InteractionResult.FromMessageAndEmbeds($"Town **{dayCat.Name}** added!", embed);
        }


        private Task<bool> RemoveTown(ITownRecord townRec)
        {
            return m_townDb.DeleteTownAsync(TownKey.FromTownRecord(townRec));
        }

        private async Task<InteractionResult> PerformRemoveTown(IProcessLogger _, IBotInteractionContext ctx, IChannelCategory? dayCat)
        {
            bool categoryMode = (dayCat != null);
            string townName = "unknown";
            var guild = ctx.Guild;
            bool success = false;
            ITownRecord? townRec = dayCat == null ? 
                await m_townDb.GetTownRecordAsync(guild.Id, ctx.Channel.Id) :
                await m_townDb.GetTownRecordByNameAsync(guild.Id, dayCat.Name);

            if (townRec != null)
            {
                if(townRec.DayCategory != null)
                    townName = townRec.DayCategory;
                success = await RemoveTown(townRec);
            }

            if (success)
            {
                var embed = m_botSystem.CreateEmbedBuilder();
                embed.WithTitle($"{guild.Name} // {townName}");
                embed.WithDescription($"This town is no longer registered.");
                embed.WithColor(m_botSystem.ColorBuilder.DarkRed);

                return InteractionResult.FromMessageAndEmbeds("", embed.Build());
            }
            else
            {
                string msg = "Couldn't find a town controlled by this channel to remove!";
                if (categoryMode)
                    msg = $"Couldn't find a town named **{townName}** on this server to remove!";
                return InteractionResult.FromMessage(msg);
            }
        }

        private async Task<InteractionResult> PerformDestroyTown(IProcessLogger _, IBotInteractionContext ctx, string townName)
        {
            var guild = ctx.Guild;

            var tdesc = new TownDescription();
            tdesc.PopulateFromTownName(townName, guild, ctx.Member, true);

            var botRole = guild.BotRole;
            Console.WriteLine($"BotSetup: PerformDestroyTown starting. TownName={townName}, GuildId={guild.Id}, GuildName={guild.Name}, BotRole={botRole?.Name ?? "NOT FOUND"}, BotRoleId={botRole?.Id ?? 0}");
            Console.WriteLine($"BotSetup: TownDescription populated. DayCategoryName={tdesc.DayCategoryName}, NightCategoryName={tdesc.NightCategoryName}, ControlChannelName={tdesc.ControlChannelName}, ChatChannelName={tdesc.ChatChannelName}");

            List<string> notDeletedUnfamiliar = new();
            List<string> notDeleted = new();

            bool success = true;

            // Night Category
            var nightCat = guild.GetCategoryByName(tdesc.NightCategoryName!);
            if(nightCat != null)
            {
                Console.WriteLine($"BotSetup: Night category found. Name={nightCat.Name}, Id={nightCat.Id}, ChannelCount={nightCat.Channels.Count}");
                var toDestroy = new List<IChannel>();
                foreach(var channel in nightCat.Channels)
                {
                    if(channel.IsVoice && channel.Name == IBotSetup.DefaultCottageName)
                    {
                        toDestroy.Add(channel);
                    }
                }

                foreach(var channel in toDestroy)
                {
                    Console.WriteLine($"BotSetup: Deleting night channel. Name={channel.Name}, Id={channel.Id}");
                    await channel.DeleteAsync();
                }

                if(nightCat.Channels.Count == 0)
                {
                    Console.WriteLine($"BotSetup: Deleting night category. Name={nightCat.Name}, Id={nightCat.Id}");
                    await nightCat.DeleteAsync();
                }
                else
                {
                    foreach(var channel in nightCat.Channels)
                    {
                        notDeletedUnfamiliar.Add($"**{nightCat.Name}** / **{channel.Name}**");
                    }
                    notDeleted.Add($"**{nightCat.Name}** (category)");
                    success = false;
                }
            }
            else
            {
                Console.WriteLine($"BotSetup: Night category not found. Expected name={tdesc.NightCategoryName}");
            }

            var dayCat = guild.GetCategoryByName(tdesc.DayCategoryName);
            if(dayCat != null)
            {
                Console.WriteLine($"BotSetup: Day category found. Name={dayCat.Name}, Id={dayCat.Id}, ChannelCount={dayCat.Channels.Count}");
                foreach (var ch in dayCat.Channels)
                {
                    Console.WriteLine($"BotSetup:   Channel in day category: Name={ch.Name}, Id={ch.Id}, IsText={ch.IsText}, IsVoice={ch.IsVoice}");
                }

                var chatChan = dayCat.GetChannelByName(ChannelHelper.MakeTextChannelName(tdesc.ChatChannelName));
                if(chatChan != null && chatChan.IsText)
                {
                    Console.WriteLine($"BotSetup: Found chat channel to delete. Name={chatChan.Name}, Id={chatChan.Id}, IsText={chatChan.IsText}. BotRole={botRole?.Name ?? "NOT FOUND"}, BotRoleId={botRole?.Id ?? 0}");
                    await chatChan.DeleteAsync();
                    Console.WriteLine($"BotSetup: Chat channel deletion completed.");
                }
                else
                {
                    if (chatChan == null)
                        Console.WriteLine($"BotSetup: Chat channel not found. Expected name={ChannelHelper.MakeTextChannelName(tdesc.ChatChannelName)}");
                    else
                        Console.WriteLine($"BotSetup: Chat channel found but IsText={chatChan.IsText}, skipping delete.");
                }

                var townSquare = dayCat.GetChannelByName(tdesc.TownSquareName);
                if(townSquare != null && townSquare.IsVoice)
                {
                    Console.WriteLine($"BotSetup: Deleting town square. Name={townSquare.Name}, Id={townSquare.Id}");
                    await townSquare.DeleteAsync();
                }

                foreach(string s in DefaultExtraDayChannels)
                {
                    var chan = dayCat.GetChannelByName(s);
                    if(chan != null && chan.IsVoice)
                    {
                        Console.WriteLine($"BotSetup: Deleting extra day channel. Name={chan.Name}, Id={chan.Id}");
                        await chan.DeleteAsync();
                    }
                }

                var controlChan = dayCat.GetChannelByName(ChannelHelper.MakeTextChannelName(tdesc.ControlChannelName));
                if(controlChan != null && controlChan.IsText)
                {
                    if(success && dayCat.Channels.Count == 1)
                    {
                        Console.WriteLine($"BotSetup: Deleting control channel. Name={controlChan.Name}, Id={controlChan.Id}");
                        await controlChan.DeleteAsync();
                    }
                    else
                    {
                        notDeleted.Add($"**{tdesc.DayCategoryName}** / **{tdesc.ControlChannelName}**");
                        success = false;
                    }
                }

                if(dayCat.Channels.Count == 0)
                {
                    Console.WriteLine($"BotSetup: Deleting day category. Name={dayCat.Name}, Id={dayCat.Id}");
                    await dayCat.DeleteAsync();
                }
                else
                {
                    notDeleted.Add($"**{dayCat.Name}** (category)");
                    success = false;
                }
            }
            else
            {
                Console.WriteLine($"BotSetup: Day category not found. Expected name={tdesc.DayCategoryName}");
                return InteractionResult.FromMessage($"Couldn't find a town named **{townName}** on this server!");
            }

            // Don't try to remove roles if we had an issue with something else - we might need them to see channels that didn't delete!
            if(success)
            {
                var gameVillager = guild.GetRoleByName(tdesc.VillagerRoleName);
                if(gameVillager != null)
                {
                    Console.WriteLine($"BotSetup: Deleting villager role. Name={gameVillager.Name}, Id={gameVillager.Id}");
                    await gameVillager.DeleteAsync();
                }
                else
                {
                    notDeleted.Add($"**{tdesc.VillagerRoleName}** (role)");
                }

                var gameStoryteller = guild.GetRoleByName(tdesc.StorytellerRoleName);
                if( gameStoryteller != null)
                {
                    Console.WriteLine($"BotSetup: Deleting storyteller role. Name={gameStoryteller.Name}, Id={gameStoryteller.Id}");
                    await gameStoryteller.DeleteAsync();
                }
                else
                {
                    notDeleted.Add($"**{tdesc.StorytellerRoleName}** (role)");
                }
            }

            var message = $"Town **{townName}** has been destroyed.";
            if(!success)
            {
                message = $"I destroyed what I knew about for Town **{townName}**.";
                if(notDeletedUnfamiliar.Count > 0)
                {
                    message += "\n\nI did not destroy some things I was unfamiliar with:";
                    foreach(var s in notDeletedUnfamiliar)
                    {
                        message += "\n * " + s;
                    }
                    message += "\n\nYou can destroy them manually and run this command again.\n";
                }
                if(notDeleted.Count > 0)
                {
                    message += "\nI did not destroy these things yet, just in case you still need them:";
                    foreach(var s in notDeleted)
                    {
                        message += "\n * " + s;
                    }
                }
            }

            if(success)
            {
                var townRec = await m_townDb.GetTownRecordByNameAsync(guild.Id, townName);
                if (townRec != null)
                {
                    bool removeSuccess = await RemoveTown(townRec);
                    if (removeSuccess)
                        message += $"\n\nTown **{townName}** is no longer registered.";
                }
            }

            return InteractionResult.FromMessage(message);
        }
        
        private IEmbed EmbedFromTown(ITown town, DateTime timestamp, string authorName)
        {
            var u = "Unknown";

            var embed = m_botSystem.CreateEmbedBuilder();
            embed.WithTitle($"{town.Guild?.Name ?? u} // {town.DayCategory?.Name ?? u}")
                .WithDescription($"Created {timestamp} by {authorName}")
                .WithColor(m_botSystem.ColorBuilder.DarkRed);
            embed.AddField("Control Channel", town.ControlChannel?.Name ?? u);
            embed.AddField("Town Square", town.TownSquare?.Name ?? u);
            if (town.ChatChannel != null) embed.AddField("Chat Channel", town.ChatChannel.Name);
            embed.AddField("Day Category", town.DayCategory?.Name ?? u);
            if (town.NightCategory != null) embed.AddField("Night Category", town.NightCategory.Name);
            embed.AddField("Storyteller Role", town.StorytellerRole?.Name ?? u);
            embed.AddField("Villager Role", town.VillagerRole?.Name ?? u);
            return embed.Build();
        }

        private async Task<InteractionResult> PerformTownInfo(IProcessLogger _, IBotInteractionContext ctx)
        {
            var townRecord = await m_townDb.GetTownRecordAsync(ctx.Guild.Id, ctx.Channel.Id);

            if (townRecord != null)
            {
                var town = await m_townResolver.ResolveTownAsync(townRecord);

                if (town != null)
                {
                    var embed = EmbedFromTown(town, townRecord.Timestamp, townRecord.AuthorName ?? "unknown");

                    return InteractionResult.FromMessageAndEmbeds("Town found!", embed);
                }
            }

            return InteractionResult.FromMessage("Couldn't find an active town for this server & channel!");
        }

        private async Task<InteractionResult> PerformCreateTown(IProcessLogger _, IBotInteractionContext ctx, string townName, IRole? guildPlayerRole, IRole? guildStRole, bool useNight)
        {
            TownDescription tdesc = new();
            tdesc.PopulateFromTownName(townName, ctx.Guild, ctx.Member, useNight);

            var newTown = await CreateTown(tdesc, ctx.Member, guildStRole, guildPlayerRole);

            await m_commandMetricsDatabase.RecordCommand("createtown", m_dateTime.Now);

            var embed = EmbedFromTown(newTown, m_dateTime.Now, ctx.Member.DisplayName ?? "unknown");

            return InteractionResult.FromMessageAndEmbeds($"Created new town **{townName}**!", embed);
        }

        public Task AddTown(ITown town, IMember author)
        {
            return m_townDb.AddTownAsync(town, author);
        }

        public async Task<Town> CreateTown(TownDescription townDesc, IMember author, IRole? guildStRole = null, IRole? guildPlayerRole = null)
        {
            IGuild guild = townDesc.Guild;

            Serilog.Log.Information(
                "CreateTown: starting for guild {GuildName} ({GuildId}); guildPlayerRolePresent={GuildPlayerRolePresent}, guildStRolePresent={GuildStRolePresent}, dayCategory='{DayCategory}', nightCategory='{NightCategory}', controlChannel='{ControlChannel}', townSquare='{TownSquare}', chatChannel='{ChatChannel}'",
                guild.Name,
                guild.Id,
                guildPlayerRole != null,
                guildStRole != null,
                townDesc.DayCategoryName,
                townDesc.NightCategoryName,
                townDesc.ControlChannelName,
                townDesc.TownSquareName,
                townDesc.ChatChannelName);

            Town newTown = new();
            newTown.Guild = guild;

            // Get bot role
            var botRole = guild.BotRole;
            if (botRole == null)
                throw new CreateTownException($"Could not find bot role!");
            var everyoneRole = guild.EveryoneRole;

            Console.WriteLine($"BotSetup: CreateTown acquired bot role. BotRoleName={botRole.Name}, BotRoleId={botRole.Id}, EveryoneRoleName={everyoneRole.Name}, EveryoneRoleId={everyoneRole.Id}");

            // Make sure things aren't null
            townDesc.FallbackToDefaults();

            // First create the roles for this town
            newTown.StorytellerRole = await RoleHelper.GetOrCreateRole(guild, townDesc.StorytellerRoleName, Color.Magenta);
            if (newTown.StorytellerRole == null)
                throw new CreateTownException($"Could not find or create Storyteller role '{townDesc.StorytellerRoleName}'");

            newTown.VillagerRole = await RoleHelper.GetOrCreateRole(guild, townDesc.VillagerRoleName, Color.Orange);
            if (newTown.VillagerRole == null)
                throw new CreateTownException($"Could not find or create Villager role '{townDesc.VillagerRoleName}'");


            // Create Day Category and set up visibility
            newTown.DayCategory = await ChannelHelper.GetOrCreateCategory(guild, townDesc.DayCategoryName!);
            if (newTown.DayCategory == null)
                throw new CreateTownException($"Could not find or create day category '{townDesc.DayCategoryName}'");
            await newTown.DayCategory.AddOverwriteAsync(newTown.VillagerRole, Permissions.AccessChannels);
            Serilog.Log.Information("CreateTown: day category overwrite applied for {CategoryName} ({CategoryId}) -> {RoleName}: allow={Allow}, deny={Deny}", newTown.DayCategory.Name, newTown.DayCategory.Id, newTown.VillagerRole.Name, Permissions.AccessChannels, Permissions.None);
            await newTown.DayCategory.AddOverwriteAsync(botRole, Permissions.AccessChannels | Permissions.MoveMembers | Permissions.ManageChannels);
            Serilog.Log.Information("CreateTown: day category overwrite applied for {CategoryName} ({CategoryId}) -> {RoleName}: allow={Allow}, deny={Deny}", newTown.DayCategory.Name, newTown.DayCategory.Id, botRole.Name, Permissions.AccessChannels | Permissions.MoveMembers | Permissions.ManageChannels, Permissions.None);
            await newTown.DayCategory.AddOverwriteAsync(newTown.StorytellerRole, Permissions.MoveMembers);
            Serilog.Log.Information("CreateTown: day category overwrite applied for {CategoryName} ({CategoryId}) -> {RoleName}: allow={Allow}, deny={Deny}", newTown.DayCategory.Name, newTown.DayCategory.Id, newTown.StorytellerRole.Name, Permissions.MoveMembers, Permissions.None);

            newTown.ControlChannel = await ChannelHelper.GetOrCreateTextChannel(guild, newTown.DayCategory, townDesc.ControlChannelName!);
            if (newTown.ControlChannel == null)
                throw new CreateTownException($"Could not find or create control channel '{townDesc.ControlChannelName}'");
            await newTown.ControlChannel.AddOverwriteAsync(botRole, Permissions.AccessChannels | Permissions.ManageChannels);
            Serilog.Log.Information("CreateTown: control channel overwrite applied for {ChannelName} ({ChannelId}) -> {RoleName}: allow={Allow}, deny={Deny}", newTown.ControlChannel.Name, newTown.ControlChannel.Id, botRole.Name, Permissions.AccessChannels | Permissions.ManageChannels, Permissions.None);
            await newTown.ControlChannel.RemoveOverwriteAsync(newTown.VillagerRole);
            Serilog.Log.Information("CreateTown: control channel overwrite removed for {ChannelName} ({ChannelId}) -> {RoleName}", newTown.ControlChannel.Name, newTown.ControlChannel.Id, newTown.VillagerRole.Name);

            if (guildStRole != null)
            {
                await newTown.ControlChannel.AddOverwriteAsync(guildStRole, Permissions.AccessChannels);
                Serilog.Log.Information("CreateTown: control channel overwrite applied for {ChannelName} ({ChannelId}) -> {RoleName}: allow={Allow}, deny={Deny}", newTown.ControlChannel.Name, newTown.ControlChannel.Id, guildStRole.Name, Permissions.AccessChannels, Permissions.None);
                await newTown.ControlChannel.AddOverwriteAsync(everyoneRole, allow: Permissions.None, deny: Permissions.AccessChannels);
                Serilog.Log.Information("CreateTown: control channel overwrite applied for {ChannelName} ({ChannelId}) -> {RoleName}: allow={Allow}, deny={Deny}", newTown.ControlChannel.Name, newTown.ControlChannel.Id, everyoneRole.Name, Permissions.None, Permissions.AccessChannels);
            }

            newTown.TownSquare = await ChannelHelper.GetOrCreateVoiceChannel(guild, newTown.DayCategory, townDesc.TownSquareName!);
            if (newTown.TownSquare == null)
                throw new CreateTownException($"Could not find or create town square '{townDesc.TownSquareName}'");

            if (guildPlayerRole != null)
            {
                await newTown.TownSquare.AddOverwriteAsync(guildPlayerRole, Permissions.AccessChannels);
                Serilog.Log.Information("CreateTown: town square overwrite applied for {ChannelName} ({ChannelId}) -> {RoleName}: allow={Allow}, deny={Deny}", newTown.TownSquare.Name, newTown.TownSquare.Id, guildPlayerRole.Name, Permissions.AccessChannels, Permissions.None);
                await newTown.DayCategory.AddOverwriteAsync(everyoneRole, allow: Permissions.None, deny: Permissions.AccessChannels);
                Serilog.Log.Information("CreateTown: day category overwrite applied for {CategoryName} ({CategoryId}) -> {RoleName}: allow={Allow}, deny={Deny}", newTown.DayCategory.Name, newTown.DayCategory.Id, everyoneRole.Name, Permissions.None, Permissions.AccessChannels);
            }

            // Chat channel is optional
            if (townDesc.ChatChannelName != null)
            {
                newTown.ChatChannel = await ChannelHelper.GetOrCreateTextChannel(guild, newTown.DayCategory, townDesc.ChatChannelName);
                if (newTown.ChatChannel == null)
                    throw new CreateTownException($"Could not find or create chat channel '{townDesc.ChatChannelName}'");

                Console.WriteLine($"BotSetup: About to apply chat channel overwrite. ChannelName={newTown.ChatChannel.Name}, ChannelId={newTown.ChatChannel.Id}, BotRoleName={botRole.Name}, BotRoleId={botRole.Id}, Allow={Permissions.AccessChannels | Permissions.ManageChannels}");
                await newTown.ChatChannel.AddOverwriteAsync(botRole, Permissions.AccessChannels | Permissions.ManageChannels);
                Console.WriteLine($"BotSetup: Chat channel overwrite applied successfully. ChannelName={newTown.ChatChannel.Name}, ChannelId={newTown.ChatChannel.Id}");
                Serilog.Log.Information("CreateTown: chat channel overwrite applied for {ChannelName} ({ChannelId}) -> {RoleName}: allow={Allow}, deny={Deny}", newTown.ChatChannel.Name, newTown.ChatChannel.Id, botRole.Name, Permissions.AccessChannels | Permissions.ManageChannels, Permissions.None);

                if (guildPlayerRole == null)
                {
                    await newTown.ChatChannel.AddOverwriteAsync(everyoneRole, allow: Permissions.None, deny: Permissions.AccessChannels);
                    Serilog.Log.Information("CreateTown: chat channel overwrite applied for {ChannelName} ({ChannelId}) -> {RoleName}: allow={Allow}, deny={Deny}", newTown.ChatChannel.Name, newTown.ChatChannel.Id, everyoneRole.Name, Permissions.None, Permissions.AccessChannels);
                }
            }

            foreach (var chanName in DefaultExtraDayChannels)
            {
                var newChan = await ChannelHelper.GetOrCreateVoiceChannel(guild, newTown.DayCategory, chanName);
                if (newChan == null)
                    throw new CreateTownException($"Could not find or create extra day channel '{chanName}'");

                if (guildPlayerRole == null)
                {
                    await newChan.AddOverwriteAsync(everyoneRole, allow: Permissions.None, deny: Permissions.AccessChannels);
                    Serilog.Log.Information("CreateTown: extra day channel overwrite applied for {ChannelName} ({ChannelId}) -> {RoleName}: allow={Allow}, deny={Deny}", newChan.Name, newChan.Id, everyoneRole.Name, Permissions.None, Permissions.AccessChannels);
                }
            }

            // Night category is optional
            if (townDesc.NightCategoryName != null)
            {
                newTown.NightCategory = await ChannelHelper.GetOrCreateCategory(guild, townDesc.NightCategoryName);
                if (newTown.NightCategory == null)
                    throw new CreateTownException($"Could not find or create night category '{townDesc.NightCategoryName}'");

                await newTown.NightCategory.AddOverwriteAsync(newTown.StorytellerRole, Permissions.AccessChannels | Permissions.MoveMembers);
                Serilog.Log.Information("CreateTown: night category overwrite applied for {CategoryName} ({CategoryId}) -> {RoleName}: allow={Allow}, deny={Deny}", newTown.NightCategory.Name, newTown.NightCategory.Id, newTown.StorytellerRole.Name, Permissions.AccessChannels | Permissions.MoveMembers, Permissions.None);
                await newTown.NightCategory.AddOverwriteAsync(botRole, Permissions.AccessChannels | Permissions.MoveMembers | Permissions.ManageChannels);
                Serilog.Log.Information("CreateTown: night category overwrite applied for {CategoryName} ({CategoryId}) -> {RoleName}: allow={Allow}, deny={Deny}", newTown.NightCategory.Name, newTown.NightCategory.Id, botRole.Name, Permissions.AccessChannels | Permissions.MoveMembers | Permissions.ManageChannels, Permissions.None);
                await newTown.NightCategory.AddOverwriteAsync(everyoneRole, allow: Permissions.None, deny: Permissions.AccessChannels);
                Serilog.Log.Information("CreateTown: night category overwrite applied for {CategoryName} ({CategoryId}) -> {RoleName}: allow={Allow}, deny={Deny}", newTown.NightCategory.Name, newTown.NightCategory.Id, everyoneRole.Name, Permissions.None, Permissions.AccessChannels);

                for (int i = 0; i < IBotSetup.NumCottages; i++)
                {
                    await guild.CreateVoiceChannelAsync(IBotSetup.DefaultCottageName, newTown.NightCategory);
                    Serilog.Log.Information("CreateTown: created night cottage channel {ChannelName} in category {CategoryName} ({CategoryId})", IBotSetup.DefaultCottageName, newTown.NightCategory.Name, newTown.NightCategory.Id);
                }
            }

            await AddTown(newTown, author);

            return newTown;
        }

    }

    public class CreateTownException : Exception
    {
        public CreateTownException(string message)
            : base(message)
        {
        }
    }
}
