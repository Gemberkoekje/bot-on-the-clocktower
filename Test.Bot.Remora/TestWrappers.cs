using Bot.Api;
using Bot.Remora;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Xunit;

namespace Test.Bot.Remora
{
    public class TestWrappers
    {
        [Fact]
        public static async Task Channel_SendMessage_WithRemoraBuilder_UsesBuilderContent()
        {
            RemoraChannel channel = new(1, "general");
            RemoraMessageBuilder builder = new();
            builder.WithContent("content from builder");

            IMessage message = await channel.SendMessageAsync(builder);

            RemoraMessage typed = Assert.IsType<RemoraMessage>(message);
            Assert.Equal("content from builder", typed.Content);
        }

        [Fact]
        public static async Task Channel_SendMessage_WithWrongEmbedType_Throws()
        {
            RemoraChannel channel = new(1, "general");

            await Assert.ThrowsAsync<InvalidOperationException>(() => channel.SendMessageAsync(new FakeEmbed()));
        }

        [Fact]
        public static void System_CreateButton_ProducesButtonComponent()
        {
            RemoraSystem system = new();

            IBotComponent component = system.CreateButton("c", "label", IBotSystem.ButtonType.Danger, true, "x");

            RemoraComponent typed = Assert.IsType<RemoraComponent>(component);
            Assert.Equal(RemoraComponent.ComponentKind.Button, typed.Kind);
            Assert.Equal(IBotSystem.ButtonType.Danger, typed.ButtonType);
            Assert.True(typed.Disabled);
        }

        [Fact]
        public static void System_CreateSelectMenu_ProducesSelectComponent()
        {
            RemoraSystem system = new();
            var options = new[] { new IBotSystem.SelectMenuOption("label", "value", "desc") };

            IBotComponent component = system.CreateSelectMenu("menu", "pick", options, false, 1, 1);

            RemoraComponent typed = Assert.IsType<RemoraComponent>(component);
            Assert.Equal(RemoraComponent.ComponentKind.SelectMenu, typed.Kind);
            Assert.Single(typed.SelectOptions);
        }

        [Fact]
        public static async Task Guild_RoleAndChannelLookups_Work()
        {
            RemoraRole everyone = new(0, "everyone");
            RemoraRole storyteller = new(10, "storyteller", true);
            RemoraChannel control = new(20, "control");
            RemoraChannelCategory day = new(30, "day");
            RemoraGuild guild = new(100, "guild", new[] { everyone, storyteller }, null, new[] { control }, new[] { day });

            Assert.Same(storyteller, guild.GetRoleByName("storyteller"));
            Assert.Same(control, guild.GetChannel(20));
            Assert.Same(day, guild.GetChannelCategory(30));
            Assert.Same(storyteller, guild.BotRole);

            IRole? created = await guild.CreateRoleAsync("villager", Color.Blue);
            Assert.NotNull(created);
            Assert.Same(created, guild.GetRoleByName("villager"));
        }

        [Fact]
        public static async Task InteractionContext_HappyPath_StoresBuildersAndDeferredState()
        {
            RemoraGuild guild = new(1, "guild");
            RemoraChannel channel = new(2, "general");
            RemoraMember member = new(3, "member");
            RemoraInteractionContext context = new(guild, channel, member, "component-id", new[] { "x", "y" });

            RemoraWebhookBuilder webhook = new();
            RemoraInteractionResponseBuilder response = new();

            await context.DeferInteractionResponse();
            await context.EditResponseAsync(webhook);
            await context.UpdateOriginalMessageAsync(response);
            await context.ShowModalAsync(response);

            Assert.True(context.IsDeferred);
            Assert.Same(webhook, context.LastWebhookBuilder);
            Assert.Same(response, context.LastInteractionResponseBuilder);
            Assert.Equal("component-id", context.ComponentCustomId);
            Assert.Equal(new[] { "x", "y" }, context.ComponentValues);
        }

        [Fact]
        public static async Task InteractionContext_RejectsMismatchedBuilders()
        {
            RemoraGuild guild = new(1, "guild");
            RemoraChannel channel = new(2, "general");
            RemoraMember member = new(3, "member");
            RemoraInteractionContext context = new(guild, channel, member);

            await Assert.ThrowsAsync<InvalidOperationException>(() => context.EditResponseAsync(new FakeWebhookBuilder()));
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.UpdateOriginalMessageAsync(new FakeInteractionResponseBuilder()));
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.ShowModalAsync(new FakeInteractionResponseBuilder()));
        }

        private sealed class FakeEmbed : IEmbed
        {
        }

        private sealed class FakeWebhookBuilder : IBotWebhookBuilder
        {
            public IBotWebhookBuilder AddComponents(params IBotComponent[] components) => this;
            public IBotWebhookBuilder AddEmbeds(IEnumerable<IEmbed> embeds) => this;
            public IBotWebhookBuilder WithContent(string content) => this;
        }

        private sealed class FakeInteractionResponseBuilder : IInteractionResponseBuilder
        {
            public IInteractionResponseBuilder AddComponents(params IBotComponent[] components) => this;
            public IInteractionResponseBuilder AddEmbeds(IEnumerable<IEmbed> embeds) => this;
            public IInteractionResponseBuilder WithContent(string content) => this;
            public IInteractionResponseBuilder WithCustomId(string customId) => this;
            public IInteractionResponseBuilder WithTitle(string title) => this;
        }
    }
}
