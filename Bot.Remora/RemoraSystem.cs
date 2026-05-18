using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bot.Remora
{
    public class RemoraSystem : IBotSystem
    {
        public IBotClient CreateClient(IServiceProvider serviceProvider)
        {
            return new RemoraClient(serviceProvider);
        }

        public IBotWebhookBuilder CreateWebhookBuilder() => new RemoraWebhookBuilder();

        public IInteractionResponseBuilder CreateInteractionResponseBuilder() => new RemoraInteractionResponseBuilder();

        public IEmbedBuilder CreateEmbedBuilder() => new RemoraEmbedBuilder();

        public IMessageBuilder CreateMessageBuilder() => new RemoraMessageBuilder();

        public IColorBuilder ColorBuilder { get; } = new RemoraColorBuilder();

        public IBotComponent CreateButton(string customId, string label, IBotSystem.ButtonType type = IBotSystem.ButtonType.Primary, bool disabled = false, string? emoji = null)
            => RemoraComponent.Button(customId, label, type, disabled, emoji ?? string.Empty);

        public IBotComponent CreateSelectMenu(string customId, string placeholder, IEnumerable<IBotSystem.SelectMenuOption> options, bool disabled = false, int minOptions = 1, int maxOptions = 1)
        {
            var optionList = options.ToList();
            return RemoraComponent.SelectMenu(customId, placeholder, optionList, disabled, minOptions, maxOptions);
        }

        public IBotComponent CreateTextInput(string customId, string label, string? placeholder = null, string? value = null, bool required = true)
            => RemoraComponent.TextInput(customId, label, placeholder ?? string.Empty, value ?? string.Empty, required);
    }
}
