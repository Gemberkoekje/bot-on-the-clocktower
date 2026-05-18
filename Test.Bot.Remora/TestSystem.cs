using Bot.Api;
using Bot.Remora;
using Xunit;

namespace Test.Bot.Remora
{
    public class TestSystem
    {
        [Fact]
        public void ConstructSystem_NoExceptions()
        {
            _ = new RemoraSystem();
        }

        [Fact]
        public void System_ImplementsSystemInterface()
        {
            Assert.True(typeof(IBotSystem).IsAssignableFrom(typeof(RemoraSystem)));
        }
    }
}
