using System;
using System.Data;
using NHibernate;

namespace uShip.NHibnernate.UnitOfWork
{
    public static class SessionFactoryExtensions
    {
        private class Uow<TResult> : UnitOfWork<TResult>
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
