using System;
using NUnit.Framework;

namespace S203.NewRelic.RabbitMq.Tests
{
    [TestFixture]
    class RabbitAgentTests
    {
        [Test]
        public void Ctor_Null_Argument_Name()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var agent = new RabbitAgent(null, "", 0, "", "");
            });
        }

        [Test]
        public void Ctor_Null_Argument_Host()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var agent = new RabbitAgent("name", null, 0, "", "");
            });
        }

        [Test]
        public void Ctor_Null_Argument_Username()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var agent = new RabbitAgent("name", "host", 0, null, "");
            });
        }

        [Test]
        public void Ctor_Null_Argument_Password()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var agent = new RabbitAgent("name", "host", 0, "", null);
            });
        }

        [Test]
        public void Ctor_Version()
        {
            var agent = new RabbitAgent("name", "host", 0, "username", "password");
            Assert.IsNotNull(agent.Version);
        }

        [Test]
        public void Ctor_Guid()
        {
            var agent = new RabbitAgent("name", "host", 0, "username", "password");
            Assert.IsNotNull(agent.Guid);
            Assert.IsTrue(agent.Guid == "com.203sol.newrelic.rabbitmq");
        }

        [Test]
        public void Ctor_Name()
        {
            var agent = new RabbitAgent("name", "host", 0, "username", "password");
            Assert.IsNotNull(agent.GetAgentName());
            Assert.IsTrue(agent.GetAgentName() == "name");
        }
    }
}
