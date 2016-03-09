using System;
using NUnit.Framework;

namespace S203.NewRelic.RabbitMq.Tests
{
    [TestFixture]
    class EsbAgentTests
    {
        [Test]
        public void Ctor_Null_Argument_Name()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var agent = new EsbAgent(null, "", 0, "");
            });
        }

        [Test]
        public void Ctor_Null_Argument_Host()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var agent = new EsbAgent("name", null, 0, "");
            });
        }

        [Test]
        public void Ctor_Null_Argument_Instance()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var agent = new EsbAgent("name", "host", 0, null);
            });
        }

        [Test]
        public void Ctor_Version()
        {
            var agent = new EsbAgent("name", "host", 0, "instance");
            Assert.IsNotNull(agent.Version);
        }

        [Test]
        public void Ctor_Guid()
        {
            var agent = new EsbAgent("name", "host", 0, "instance");
            Assert.IsNotNull(agent.Guid);
            Assert.IsTrue(agent.Guid == "com.203sol.newrelic.neuronesb");
        }

        [Test]
        public void Ctor_Name()
        {
            var agent = new EsbAgent("name", "host", 0, "instance");
            Assert.IsNotNull(agent.GetAgentName());
            Assert.IsTrue(agent.GetAgentName() == "name");
        }
    }
}
