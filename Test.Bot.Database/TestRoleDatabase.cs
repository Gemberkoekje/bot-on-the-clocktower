using Bot.Database;
using Marten;
using Moq;
using Xunit;

namespace Test.Bot.Database
{
    public class TestRoleDatabase
    {
        [Fact]
        public void Ctor_WithDocumentStore_Works()
        {
            var mockStore = new Mock<IDocumentStore>(MockBehavior.Strict);

            var db = new LookupRoleDatabase(mockStore.Object);

            Assert.NotNull(db);
        }
    }
}
