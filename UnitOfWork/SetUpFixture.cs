using NUnit.Framework;

namespace UOW
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
