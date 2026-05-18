using Bot.Api;
using System.Collections.Generic;

namespace Bot.Remora
{
    public class RemoraComponent : IBotComponent
    {
        public enum ComponentKind
        {
            Empty = 0,
            Button = 1,
            SelectMenu = 2,
            TextInput = 3,
        }

        public string CustomId { get; }

        public string Label { get; }

        public bool Disabled { get; }

        public ComponentKind Kind { get; }

        public string Placeholder { get; }

        public IReadOnlyCollection<IBotSystem.SelectMenuOption> SelectOptions { get; }

        public int MinOptions { get; }

        public int MaxOptions { get; }

        public IBotSystem.ButtonType ButtonType { get; }

        public string Emoji { get; }

        public bool Required { get; }

        public string Value { get; }

        public RemoraComponent(
            string customId,
            string label,
            bool disabled,
            ComponentKind kind,
            string placeholder,
            IReadOnlyCollection<IBotSystem.SelectMenuOption>? selectOptions,
            int minOptions,
            int maxOptions,
            IBotSystem.ButtonType buttonType,
            string emoji,
            bool required,
            string value)
        {
            CustomId = customId;
            Label = label;
            Disabled = disabled;
            Kind = kind;
            Placeholder = placeholder;
            SelectOptions = selectOptions ?? new List<IBotSystem.SelectMenuOption>();
            MinOptions = minOptions;
            MaxOptions = maxOptions;
            ButtonType = buttonType;
            Emoji = emoji;
            Required = required;
            Value = value;
        }

        public static RemoraComponent Button(string customId, string label, IBotSystem.ButtonType type, bool disabled, string emoji)
            => new(customId, label, disabled, ComponentKind.Button, string.Empty, null, 0, 0, type, emoji, false, string.Empty);

        public static RemoraComponent SelectMenu(string customId, string placeholder, IReadOnlyCollection<IBotSystem.SelectMenuOption> options, bool disabled, int minOptions, int maxOptions)
            => new(customId, placeholder, disabled, ComponentKind.SelectMenu, placeholder, options, minOptions, maxOptions, IBotSystem.ButtonType.Primary, string.Empty, false, string.Empty);

        public static RemoraComponent TextInput(string customId, string label, string placeholder, string value, bool required)
            => new(customId, label, false, ComponentKind.TextInput, placeholder, null, 0, 0, IBotSystem.ButtonType.Primary, string.Empty, required, value);
    }
}
