using Bot.Api;

namespace Bot.Remora
{
    public class RemoraColor : IColor
    {
        public int Rgb { get; }

        public RemoraColor(int rgb)
        {
            Rgb = rgb;
        }
    }
}
