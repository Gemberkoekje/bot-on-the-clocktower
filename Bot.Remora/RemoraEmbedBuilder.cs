using Bot.Api;
using System.Collections.Generic;

namespace Bot.Remora
{
    public class RemoraEmbedBuilder : IEmbedBuilder
    {
        private readonly List<(string Name, string Value, bool Inline)> m_fields = new();

        public IEmbed Build()
        {
            return new RemoraEmbed();
        }

        public IEmbedBuilder WithTitle(string title) => this;

        public IEmbedBuilder WithDescription(string description) => this;

        public IEmbedBuilder WithThumbnail(string url, int height = 0, int width = 0) => this;

        public IEmbedBuilder WithAuthor(string? name = null, string? url = null, string? iconUrl = null) => this;

        public IEmbedBuilder WithFooter(string? text = null, string? iconUrl = null) => this;

        public IEmbedBuilder WithColor(IColor color) => this;

        public IEmbedBuilder WithImageUrl(string url) => this;

        public IEmbedBuilder AddField(string name, string value, bool inline = false)
        {
            m_fields.Add((name, value, inline));
            return this;
        }
    }
}
