using Bot.Api;
using Bot.Main;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Xunit;

namespace Test.Main
{
    public static class TestProgram
    {
        private static IConfiguration CreateTestConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                {"Discord:Token", "test-token"},
                {"Deployment:Type", "dev"}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public static void CreateServices_HasEnvironment()
        {
            var config = CreateTestConfiguration();
            var sp = Program.RegisterServices(config);
            var env = sp.GetService<IEnvironment>();

            Assert.IsType<ConfigurationEnvironment>(env);
        }

        [Fact]
        public static void CreateServices_HasDateTime()
        {
            var config = CreateTestConfiguration();
            var sp = Program.RegisterServices(config);
            var env = sp.GetService<IDateTime>();

            Assert.IsType<DateTimeStatic>(env);
        }

        [Fact]
        public static void CreateServices_HasTask()
        {
            var config = CreateTestConfiguration();
            var sp = Program.RegisterServices(config);
            var env = sp.GetService<ITask>();

            Assert.IsType<TaskStatic>(env);
        }
    }
}
