using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bot.Remora
{
    public class RemoraWebhookBuilder : IBotWebhookBuilder
    {
        private readonly List<IBotComponent> m_components = new();
        private readonly List<IEmbed> m_embeds = new();

        public string Content { get; private set; } = string.Empty;

        public IBotWebhookBuilder WithContent(string content)
        {
            Content = content;
            return this;
        }

        public IBotWebhookBuilder AddComponents(params IBotComponent[] components)
        {
            if (!components.All(c => c is RemoraComponent))
            {
                throw new InvalidOperationException("Expected to be passed only components of Remora types");
            }

            m_components.AddRange(components);
            return this;
        }

        public IBotWebhookBuilder AddEmbeds(IEnumerable<IEmbed> embeds)
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
