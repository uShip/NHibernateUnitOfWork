using System;
using System.Data;
using NHibernate;

namespace uShip.NHibernate.UnitOfWork
{
    public static class SessionFactoryExtensions
    {
        private class Uow<TResult> : UnitOfWork<TResult>
        {
            private readonly Func<ISession, TResult> _doWork;

            public Uow(
                ISessionFactory sessionFactory,
                Func<ISession, TResult> doWork)
                : base(sessionFactory)
            {
                _doWork = doWork;
            }

            public override TResult InnerExecute(ISession session)
            {
                return _doWork(session);
            }
        }

        public static TResult UnitOfWork<TResult>(
            this ISessionFactory sessionFactory,
            IsolationLevel isolationLevel,
            Func<ISession, TResult> func)
        {
            return new Uow<TResult>(sessionFactory, func).Execute(isolationLevel);
        }

        public static TResult UnitOfWork<TResult>(
            this ISessionFactory sessionFactory,
            Func<ISession, TResult> func)
        {
            return new Uow<TResult>(sessionFactory, func).Execute();
        }

        public static void UnitOfWork(
            this ISessionFactory sessionFactory,
            Action<ISession> doWork)
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
            Action<ISession> doWork)
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
