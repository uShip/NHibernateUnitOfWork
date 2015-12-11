using System;
using NHibernate;

namespace uShip.NHibnernate.UnitOfWork
{
    /// <summary>
    ///     Register global callbacks that are notified when exceptions occur
    ///     during UnitOfWork calls to <c>InnerExecute()</c>, 
    ///     <c>Transaction.Commit()</c>, and <c>Transaction.Rollback()</c>.  
    ///     These handlers will be called for the remainder of the application 
    ///     lifetime, and cannot be un-registered.
    /// </summary>
    /// <para>
    ///     The exception will still bubble up from the original call, so this is NOT
    ///     a substitute for normal exception handling.
    /// </para>
    public abstract class UnitOfWorkEvents
    {
        /// <summary>
        /// A callback that will be notified of exceptions that occur during
        /// specific parts of the UnitOfWork execution.
        /// </summary>
        /// <param name="sessionFactory">
        /// The session factory from which the Session and Transaction created
        /// by the UnitOfWork originated.  This is made available so that the
        /// connection information can be included in log messages.
        /// </param>
        /// <param name="exc">
        /// the exception that occurred
        /// </param>
        public delegate void OnException(
            ISessionFactory sessionFactory, 
            Exception exc);

        private static event OnException OnExecuteOrCommitException = delegate { };
        private static event OnException OnRollbackException = delegate { };

        /// <summary>
        ///     Register a global callback that will be notified when exceptions occur
        ///     during UnitOfWork calls to <c>InnerExecute()</c> or
        ///     <c>ITransaction.Commit()</c>.  This handler will be called for the
        ///     remainder of the application lifetime, and cannot be un-registered.
        /// </summary>
        /// <para>
        ///     The exception will still bubble up from the original call, so this is NOT
        ///     a substitute for normal exception handling.
        /// </para>
        public static void AddGlobalExecuteOrCommitExceptionHandler(
            OnException handler)
        {
            if (null == handler) throw new ArgumentNullException("handler");
                
            OnExecuteOrCommitException += handler;
        }

        /// <summary>
        /// <para>
        ///     Register a global callback that will be notified when exceptions occur
        ///     during UnitOfWork calls to <c>ITransaction.Rollback()</c>. This handler
        ///     will be called for the remainder of the application lifetime, and cannot
        ///     be un-registered.
        /// </para>
        /// <para>
        ///     The exception will still bubble up from the original call, so this is NOT
        ///     a substitute for normal exception handling.
        /// </para>
        /// </summary>
        public static void AddGlobalRollbackExceptionHandler(
            OnException handler)
        {
            if (null == handler) throw new ArgumentNullException("handler");

            OnRollbackException += handler;
        }

        /// <summary>
        /// This method should only be invoked by 
        /// <c>UnitOfWork(of T).Execute()</c>.  It is called when an exception
        /// occurs either during the <c>InnerExecute()</c> method or during
        /// an automatic <c>Transaction.Commit()</c>.
        /// </summary>
        /// <param name="sessionFactory">
        /// The session factory from which the Session and Transaction created
        /// by the UnitOfWork originated.  This is made available so that the
        /// connection information can be included in log messages.
        /// </param>
        /// <param name="exc">
        /// the exception that occurred
        /// </param>
        internal static void FireOnExecuteOrCommitException(
            ISessionFactory sessionFactory, 
            Exception exc)
        {
            try
            {
                OnExecuteOrCommitException(sessionFactory, exc);
            }
            catch
            {
                // swallow exceptions from handlers so that execution of the
                // UnitOfWork continues unimpeded
            }
        }

        /// <summary>
        /// This method should only be invoked by 
        /// <c>UnitOfWork(of T).Execute()</c>.  It is called when an exception
        /// occurs during an automatic call <c>Transaction.Rollback()</c>.
        /// </summary>
        /// <param name="sessionFactory">
        /// The session factory from which the Session and Transaction created
        /// by the UnitOfWork originated.  This is made available so that the
        /// connection information can be included in log messages.
        /// </param>
        /// <param name="exc">
        /// the exception that occurred
        /// </param>
        internal static void FireOnRollbackException(
            ISessionFactory sessionFactory, 
            Exception exc)
        {
            try
            {
                OnRollbackException(sessionFactory, exc);
            }
            catch
            {
                // swallow exceptions from handlers so that execution of the
                // UnitOfWork continues unimpeded
            }
        }

        /// <summary>
        /// FOR UNIT TESTING ONLY
        /// </summary>
        internal static void RemoveAllHandlers()
        {
            OnExecuteOrCommitException = delegate { };
            OnRollbackException = delegate { };
        }
    }
}
