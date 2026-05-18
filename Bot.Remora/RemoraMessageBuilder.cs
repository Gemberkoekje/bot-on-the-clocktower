using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bot.Remora
{
    public class RemoraMessageBuilder : IMessageBuilder
    {
        private readonly List<IEmbed> m_embeds = new();

        public string Content { get; private set; } = string.Empty;

        public IMessageBuilder AddEmbed(IEmbed embed)
        {
            if (embed is not RemoraEmbed)
            {
                throw new InvalidOperationException("Expected an embed that works with Remora");
            }

            m_embeds.Add(embed);
            return this;
        }

        public IMessageBuilder AddEmbeds(IEnumerable<IEmbed> embeds)
        {
            if (!embeds.All(e => e is RemoraEmbed))
            {
                throw new InvalidOperationException("Expected embeds that work with Remora");
            }

            m_embeds.AddRange(embeds);
            return this;
        }

        public IMessageBuilder WithContent(string s)
        {
            Content = s;
            return this;
        }
    }
}
