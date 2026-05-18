using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bot.Remora
{
    public class RemoraInteractionResponseBuilder : IInteractionResponseBuilder
    {
        private readonly List<IBotComponent> m_components = new();
        private readonly List<IEmbed> m_embeds = new();

        public string Title { get; private set; } = string.Empty;

        public string CustomId { get; private set; } = string.Empty;

        public string Content { get; private set; } = string.Empty;

        public IInteractionResponseBuilder WithTitle(string title)
        {
            Title = title;
            return this;
        }

        public IInteractionResponseBuilder WithCustomId(string customId)
        {
            CustomId = customId;
            return this;
        }

        public IInteractionResponseBuilder WithContent(string content)
        {
            Content = content;
            return this;
        }

        public IInteractionResponseBuilder AddComponents(params IBotComponent[] components)
        {
            if (!components.All(c => c is RemoraComponent))
            {
                throw new InvalidOperationException("Expected to be passed only components of Remora types");
            }

            m_components.AddRange(components);
            return this;
        }

        public IInteractionResponseBuilder AddEmbeds(IEnumerable<IEmbed> embeds)
        {
            if (!embeds.All(e => e is RemoraEmbed))
            {
                throw new InvalidOperationException("Expected to be passed only embeds of Remora types");
            }

            m_embeds.AddRange(embeds);
            return this;
        }
    }
}
