﻿using System;
using System.Data;
using NHibernate;

namespace uShip.NHibnernate.UnitOfWork
{
    /// <summary>
    /// <para>
    ///     Prefer the
    ///     <see cref="uShip.NHibnernate.UnitOfWork.SessionFactoryExtensions"/>
    ///     extension methods to deriving from this class directly, unless you 
    ///     have needs those methods cannot accomodate.
    /// </para>
    /// <para>
    ///     Provides automatic database Session and Transaction management for 
    ///     short-lived business transactions.  
    /// </para>
    /// </summary>
    /// <typeparam name="TResult">
    ///     returned from Execute()
    /// </typeparam>
    public abstract class UnitOfWork<TResult>
    {
        /// <summary>
        /// Each time <see cref="Execute"/> is invoked, a new Session and
        /// Transaction will be created using this SessionFactory.
        /// </summary>
        private readonly ISessionFactory _sessionFactory;

        /// <summary>
        /// Each time <see cref="Execute"/> is invoked, a new Session and
        /// Transaction will be created using the supplied SessionFactory.
        /// </summary>
        protected UnitOfWork(ISessionFactory sessionFactory)
        {
            _sessionFactory = sessionFactory;
        }

        /// <summary>
        /// automatic session and transaction management around the business
        /// transaction represented by <see cref="InnerExecute"/>.
        /// </summary>
        /// 
        /// <param name="isolationLevel">
        /// the isolation level of the database transaction; consult SQL Server
        /// Database documentation for further information
        /// </param>
        /// 
        /// <returns>
        /// the return value of <see cref="InnerExecute"/>
        /// </returns>
        /// 
        /// <seealso>
        /// <a href="https://technet.microsoft.com/en-us/library/ms189122(v=sql.105).aspx">
        /// SQL Server Isolation Levels in the Database Engine</a>
        /// </seealso>
        public TResult Execute(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            using (var session = _sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction(isolationLevel))
            {
                TResult result;

                try
                {
                    result = InnerExecute(session);
                    CommitIfActive(session.Transaction);
                }
                catch (Exception executeOrCommitExc)
                {
                    UnitOfWorkEvents.FireOnExecuteOrCommitException(_sessionFactory, executeOrCommitExc);
                    try
                    {
                        RollbackIfActive(session.Transaction);
                    }
                    catch (Exception rollbackException)
                    {
                        // KLUDGE: This sucks: We can't rethrow to preserve
                        // stack traces, and the thrown exception won't look
                        // much like either of the original exceptions.
                        var aggregateException = new AggregateException(
                            executeOrCommitExc,
                            rollbackException);
                        UnitOfWorkEvents.FireOnRollbackException(_sessionFactory, rollbackException);
                        throw aggregateException;
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

        /// <summary>
        /// The body of the business transaction represented by a concrete
        /// subclass of UnitOfWork.
        /// </summary>
        /// 
        /// <param name="session">
        /// A newly created <see cref="ISession"/> instance, created from
        /// the <see cref="ISessionFactory"/> supplied to the UnitOfWork
        /// constructor.
        /// </param>
        /// 
        /// <returns>
        /// the result of the business transaction, if any
        /// </returns>
        public abstract TResult InnerExecute(ISession session);
    }
}
