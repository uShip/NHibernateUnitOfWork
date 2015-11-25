using NUnit.Framework;

namespace UnitOfWorkTests
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [SetUp]
        public void OneTimeSetUp()
        {
            DatabaseSchema.Create();
        }

        [TearDown]
        public void OneTimeTearDown()
        {
        }
    }
}
