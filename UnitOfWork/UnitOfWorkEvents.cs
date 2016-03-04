using System;
using NHibernate;

namespace uShip.NHibernate.UnitOfWork
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

        // ReSharper disable InconsistentNaming - use non-standard naming to keep internal method names nice
        private static event OnException _onExecuteOrCommitException = delegate { };
        private static event OnException _onRollbackException = delegate { };
        // ReSharper restore InconsistentNaming

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
                
            _onExecuteOrCommitException += handler;
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

            _onRollbackException += handler;
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
        internal static void OnExecuteOrCommitException(
            ISessionFactory sessionFactory, 
            Exception exc)
        {
            try
            {
                _onExecuteOrCommitException(sessionFactory, exc);
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
        internal static void OnRollbackException(
            ISessionFactory sessionFactory, 
            Exception exc)
        {
            try
            {
                _onRollbackException(sessionFactory, exc);
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
            _onExecuteOrCommitException = delegate { };
            _onRollbackException = delegate { };
        }
    }
}
