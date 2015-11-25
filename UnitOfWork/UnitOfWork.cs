using System;
using System.Data;
using NHibernate;

namespace UOW
{
    public abstract class UnitOfWork<TResult>
    {
        private readonly ISessionFactory _sessionFactory;

        protected UnitOfWork(ISessionFactory sessionFactory)
        {
            _sessionFactory = sessionFactory;
        }

        public TResult Execute(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            using (var session = _sessionFactory.OpenSession())
            using (session.BeginTransaction(isolationLevel))
            {
                TResult result;

                try
                {
                    result = InnerExecute(session);
                    CommitIfActive(session.Transaction);
                }
                catch (Exception executeOrCommitExc)
                {
                    try
                    {
                        RollbackIfActive(session.Transaction);
                    }
                    catch (Exception rollbackExc)
                    {
                        // KLUDGE: This sucks: We can't rethrow to preserve
                        // stack traces, and the thrown exception won't look
                        // much like either of the original exceptions.
                        throw new AggregateException(
                            executeOrCommitExc,
                            rollbackExc);
                    }
                    throw;
                }

                return result;
            }
        }

        private static void RollbackIfActive(ITransaction t)
        {
            if (t.IsActive) t.Rollback();
        }

        private static void CommitIfActive(ITransaction t)
        {
            if (t.IsActive) t.Commit();
        }

        public abstract TResult InnerExecute(NHibernate.ISession session);
    }

    public static class SessionFactoryExtensions
    {
        internal class Uow<TResult> : UnitOfWork<TResult>
        {
            private readonly Func<NHibernate.ISession, TResult> _doWork;

            public Uow(
                ISessionFactory sessionFactory,
                Func<NHibernate.ISession, TResult> doWork)
                : base(sessionFactory)
            {
                _doWork = doWork;
            }

            public override TResult InnerExecute(NHibernate.ISession session)
            {
                return _doWork(session);
            }
        }

        public static TResult UnitOfWork<TResult>(
            this ISessionFactory sessionFactory,
            IsolationLevel isolationLevel,
            Func<NHibernate.ISession, TResult> func)
        {
            return new Uow<TResult>(sessionFactory, func).Execute(isolationLevel);
        }

        public static TResult UnitOfWork<TResult>(
            this ISessionFactory sessionFactory,
            Func<NHibernate.ISession, TResult> func)
        {
            return new Uow<TResult>(sessionFactory, func).Execute();
        }

        public static void UnitOfWork(
            this ISessionFactory sessionFactory,
            Action<NHibernate.ISession> doWork)
        {
            new Uow<bool>(
                sessionFactory,
                session =>
                {
                    doWork(session);
                    return true;
                }).Execute();
        }

        public static void UnitOfWork(
            this ISessionFactory sessionFactory,
            IsolationLevel isolationLevel,
            Action<NHibernate.ISession> doWork)
        {
            new Uow<bool>(
                sessionFactory,
                session =>
                {
                    doWork(session);
                    return true;
                }).Execute(isolationLevel);
        }
    }
}
