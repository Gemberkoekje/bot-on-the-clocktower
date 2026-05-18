using Bot.Api;

namespace Bot.Remora
{
    public class RemoraColorBuilder : IColorBuilder
    {
        public IColor Build(int rgb) => new RemoraColor(rgb);

        public IColor Build(byte r, byte g, byte b)
        {
            int rgb = (r << 16) | (g << 8) | b;
            return new RemoraColor(rgb);
        }

        public IColor None { get; } = new RemoraColor(0x000000);
        public IColor Black { get; } = new RemoraColor(0x000000);
        public IColor White { get; } = new RemoraColor(0xFFFFFF);
        public IColor Gray { get; } = new RemoraColor(0x808080);
        public IColor DarkGray { get; } = new RemoraColor(0x404040);
        public IColor LightGray { get; } = new RemoraColor(0xC0C0C0);
        public IColor VeryDarkGray { get; } = new RemoraColor(0x202020);
        public IColor Blurple { get; } = new RemoraColor(0x5865F2);
        public IColor Grayple { get; } = new RemoraColor(0x99AAB5);
        public IColor DarkButNotBlack { get; } = new RemoraColor(0x2C2F33);
        public IColor NotQuiteBlack { get; } = new RemoraColor(0x23272A);
        public IColor Red { get; } = new RemoraColor(0xFF0000);
        public IColor DarkRed { get; } = new RemoraColor(0x8B0000);
        public IColor Green { get; } = new RemoraColor(0x00FF00);
        public IColor DarkGreen { get; } = new RemoraColor(0x006400);
        public IColor Blue { get; } = new RemoraColor(0x0000FF);
        public IColor DarkBlue { get; } = new RemoraColor(0x00008B);
        public IColor Yellow { get; } = new RemoraColor(0xFFFF00);
        public IColor Cyan { get; } = new RemoraColor(0x00FFFF);
        public IColor Magenta { get; } = new RemoraColor(0xFF00FF);
        public IColor Teal { get; } = new RemoraColor(0x008080);
        public IColor Aquamarine { get; } = new RemoraColor(0x7FFFD4);
        public IColor Gold { get; } = new RemoraColor(0xFFD700);
        public IColor Goldenrod { get; } = new RemoraColor(0xDAA520);
        public IColor Azure { get; } = new RemoraColor(0x007FFF);
        public IColor Rose { get; } = new RemoraColor(0xFF007F);
        public IColor SpringGreen { get; } = new RemoraColor(0x00FF7F);
        public IColor Chartreuse { get; } = new RemoraColor(0x7FFF00);
        public IColor Orange { get; } = new RemoraColor(0xFFA500);
        public IColor Purple { get; } = new RemoraColor(0x800080);
        public IColor Violet { get; } = new RemoraColor(0xEE82EE);
        public IColor Brown { get; } = new RemoraColor(0xA52A2A);
        public IColor HotPink { get; } = new RemoraColor(0xFF69B4);
        public IColor Lilac { get; } = new RemoraColor(0xC8A2C8);
        public IColor CornflowerBlue { get; } = new RemoraColor(0x6495ED);
        public IColor MidnightBlue { get; } = new RemoraColor(0x191970);
        public IColor Wheat { get; } = new RemoraColor(0xF5DEB3);
        public IColor IndianRed { get; } = new RemoraColor(0xCD5C5C);
        public IColor Turquoise { get; } = new RemoraColor(0x40E0D0);
        public IColor SapGreen { get; } = new RemoraColor(0x507D2A);
        public IColor PhthaloBlue { get; } = new RemoraColor(0x000F89);
        public IColor PhthaloGreen { get; } = new RemoraColor(0x123524);
        public IColor Sienna { get; } = new RemoraColor(0xA0522D);
    }
}
