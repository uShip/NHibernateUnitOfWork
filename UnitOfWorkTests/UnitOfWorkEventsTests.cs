using System;
using System.Data;
using NHibernate;
using NSubstitute;
using NUnit.Framework;
using uShip.NHibnernate.UnitOfWork;

namespace UnitOfWorkTests
{
    [TestFixture]
    public class UnitOfWorkEventsTests
    {
        [SetUp]
        public void SetUp()
        {
            UnitOfWorkEvents.RemoveAllHandlers();
        }

        [TearDown]
        public void TearDown()
        {
            UnitOfWorkEvents.RemoveAllHandlers();
        }

        private class Handler
        {
            public int CallCount { get; private set; }
            public bool WasCalled { get { return CallCount > 0; } }
            public ISessionFactory LastSessionFactory { get; private set; }
            public Exception LastException { get; private set; }
            public Handler() { CallCount = 0; }

            public void OnException(ISessionFactory sessionFactory, Exception exc)
            {
                LastSessionFactory = sessionFactory;
                LastException = exc;
                CallCount++;
            }
        }

        [Test]
        public void AddGlobalExecuteOrCommitExceptionHandler_should_throw_when_null_handler_is_registered()
        {
            Assert.Throws<ArgumentNullException>(() =>
                UnitOfWorkEvents.AddGlobalExecuteOrCommitExceptionHandler(null));
        }

        [Test]
        public void AddGlobalRollbackExceptionHandler_should_throw_when_null_handler_is_registered()
        {
            Assert.Throws<ArgumentNullException>(() =>
                UnitOfWorkEvents.AddGlobalRollbackExceptionHandler(null));
        }

        [Test]
        public void FireOnExecuteOrCommitException_should_convey_SessionFactory_and_Exception_faithfully()
        {
            var handler = new Handler();
            UnitOfWorkEvents.AddGlobalExecuteOrCommitExceptionHandler(handler.OnException);
            var sessionFactory = Substitute.For<ISessionFactory>();
            var exception = new Exception();

            UnitOfWorkEvents.OnExecuteOrCommitException(sessionFactory, exception);

            Assert.AreEqual(1, handler.CallCount);
            Assert.AreSame(sessionFactory, handler.LastSessionFactory);
            Assert.AreSame(exception, handler.LastException);
        }

        [Test]
        public void FireOnExecuteOrCommitException_should_invoke_multiple_handlers()
        {
            var handlerA = new Handler();
            UnitOfWorkEvents.AddGlobalExecuteOrCommitExceptionHandler(handlerA.OnException);

            var handlerB = new Handler();
            UnitOfWorkEvents.AddGlobalExecuteOrCommitExceptionHandler(handlerB.OnException);

            UnitOfWorkEvents.OnExecuteOrCommitException(
                Substitute.For<ISessionFactory>(),
                new Exception());

            Assert.IsTrue(handlerA.WasCalled);
            Assert.IsTrue(handlerB.WasCalled);
        }

        [Test]
        public void FireOnExecuteOrCommitException_should_swallow_handler_exceptions()
        {
            UnitOfWorkEvents.AddGlobalExecuteOrCommitExceptionHandler(
                (sf, exc) => { throw new NullReferenceException(); });
            var sessionFactory = Substitute.For<ISessionFactory>();
            var exception = new Exception();

            Assert.DoesNotThrow(() =>
                UnitOfWorkEvents.OnExecuteOrCommitException(sessionFactory, exception));
        }

        [Test]
        public void FireOnRollbackException_should_convey_SessionFactory_and_Exception_faithfully()
        {
            var handler = new Handler();
            UnitOfWorkEvents.AddGlobalRollbackExceptionHandler(handler.OnException);
            var sessionFactory = Substitute.For<ISessionFactory>();
            var exception = new Exception();

            UnitOfWorkEvents.OnRollbackException(sessionFactory, exception);

            Assert.AreEqual(1, handler.CallCount);
            Assert.AreSame(sessionFactory, handler.LastSessionFactory);
            Assert.AreSame(exception, handler.LastException);
        }

        [Test]
        public void FireOnRollbackException_should_invoke_multiple_handlers()
        {
            var handlerA = new Handler();
            UnitOfWorkEvents.AddGlobalRollbackExceptionHandler(handlerA.OnException);

            var handlerB = new Handler();
            UnitOfWorkEvents.AddGlobalRollbackExceptionHandler(handlerB.OnException);

            UnitOfWorkEvents.OnRollbackException(
                Substitute.For<ISessionFactory>(),
                new Exception());

            Assert.IsTrue(handlerA.WasCalled);
            Assert.IsTrue(handlerB.WasCalled);
        }

        [Test]
        public void FireOnRollbackException_should_swallow_handler_exceptions()
        {
            UnitOfWorkEvents.AddGlobalRollbackExceptionHandler(
                (sf, exc) => { throw new NullReferenceException(); });
            var sessionFactory = Substitute.For<ISessionFactory>();
            var exception = new Exception();
            
            Assert.DoesNotThrow(() => 
                UnitOfWorkEvents.OnRollbackException(sessionFactory, exception));
        }

        [Test]
        public void UnitOfWork_Execute_should_invoke_event_handlers()
        {
            // Arrange
            var sessionFactory = Substitute.For<ISessionFactory>();
            var session = Substitute.For<ISession>();
            var trans = Substitute.For<ITransaction>();

            sessionFactory.OpenSession().Returns(session);

            session.BeginTransaction(default(IsolationLevel))
                .ReturnsForAnyArgs(trans);
            session.Transaction.Returns(trans);

            trans.IsActive.Returns(true);
            trans.WasRolledBack.Returns(false);
            var exc2 = new UnitOfWorkTests.MidCommitException();
            trans.When(x => x.Rollback()).Do(x => { throw exc2; });

            var onExecuteOrCommitExceptionCount = 0;
            UnitOfWorkEvents.AddGlobalExecuteOrCommitExceptionHandler(
                (sf, exc) => onExecuteOrCommitExceptionCount++);
            var onRollbackExceptionCount = 0;
            UnitOfWorkEvents.AddGlobalRollbackExceptionHandler(
                (sf, exc) => onRollbackExceptionCount++);

            var exc1 = new UnitOfWorkTests.BusinessException();

            // Act
            try
            {
                sessionFactory.UnitOfWork(s =>
                {
                    throw exc1;
                });
            }
            catch (AggregateException)
            {
                // let it go; we're only concerned with the logging callbacks
            }

            Assert.AreEqual(1, onExecuteOrCommitExceptionCount);
            Assert.AreEqual(1, onRollbackExceptionCount);
        }         
    }
}
