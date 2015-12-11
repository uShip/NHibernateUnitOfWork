using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using NHibernate;
using NHibernate.Transform;

namespace uShip.NHibnernate.UnitOfWork
{
    public static class SessionSqlStoredProcedureExtensions
    {
        //public static SqlStoredProcedure SqlStoredProcedure(
        //    this ISession session,
        //    StoredProcedure storedProcedure)
        //{
        //    return session.SqlStoredProcedure(storedProcedure.DisplayName);
        //}

        public static SqlStoredProcedure SqlStoredProcedure(
            this ISession session,
            string storedProcedureName)
        {
            return new SqlStoredProcedure(session, storedProcedureName);
        }
    }

    public class SqlStoredProcedure
    {
        private readonly ISession _session;
        private readonly string _storedProcedureName;
        private readonly Dictionary<string, object> _parameters;

        public SqlStoredProcedure(
            ISession session,
            string storedProcedureName)
        {
            if (null == session) throw new ArgumentNullException("session");
            EnsureSafeSqlIdentifier(storedProcedureName);

            _session = session;
            _storedProcedureName = storedProcedureName;
            _parameters = new Dictionary<string, object>();
        }

        public SqlStoredProcedure SetParameter(string name, object value)
        {
            EnsureSafeSqlIdentifier(name);
            _parameters.Add(name, value);
            return this;
        }

        /// <summary>
        ///     The stored procedure will actually run when this method is 
        ///     called.
        /// </summary>
        /// <typeparam name="TBean">
        ///     The <see cref="Transformers.AliasToBean"/> transformer is used 
        ///     to return instance of this type.
        /// </typeparam>
        /// <returns>
        ///     one instance of the <c>TBean</c> type for each row that results 
        ///     from running the stored procedure
        /// </returns>
        public IList<TBean> ListResult<TBean>()
        {
            return StoredProc(
                    _session,
                    _storedProcedureName,
                    _parameters)
                .SetResultTransformer(Transformers.AliasToBean<TBean>())
                .List<TBean>();
        }

        /// <summary>
        ///     The stored procedure will actually run when this method is 
        ///     called.
        /// </summary>
        /// <typeparam name="TBean">
        ///     The <see cref="Transformers.AliasToBean"/> transformer is used 
        ///     to return an instance of this type.
        /// </typeparam>
        /// <returns>
        ///     one instance of the <c>TBean</c> type for the single row that 
        ///     results from running the stored procedure
        /// </returns>
        public TBean UniqueResult<TBean>()
        {
            return StoredProc(
                    _session,
                    _storedProcedureName,
                    _parameters)
                .SetResultTransformer(Transformers.AliasToBean<TBean>())
                .UniqueResult<TBean>();
        }

        private static ISQLQuery StoredProc(
            ISession session,
            string procedureName,
            IReadOnlyDictionary<string, object> parameters)
        {
            if (null == session) throw new ArgumentNullException("session");
            if (string.IsNullOrWhiteSpace(procedureName)) throw new ArgumentException("procedureName");
            if (null == parameters) throw new ArgumentNullException("parameters");

            var query = session.CreateSQLQuery(StoredProcCommandText(
                procedureName,
                parameters.Keys));

            foreach (var p in parameters)
            {
                query.SetParameter(p.Key, p.Value);
            }

            return query;
        }

        /// <summary> EXPOSED FOR UNIT TESTING ONLY. </summary>
        internal static string StoredProcCommandText(
            string procedureName,
            IEnumerable<string> parameterNames)
        {
            if (string.IsNullOrWhiteSpace(procedureName)) throw new ArgumentException("procedureName");
            if (null == parameterNames) throw new ArgumentNullException("parameterNames");

            var sb = new StringBuilder();
            EnsureSafeSqlIdentifier(procedureName);
            sb.AppendFormat("EXEC {0}", procedureName);

            var isFirstParameter = true;
            foreach (var paramName in parameterNames)
            {
                if (!isFirstParameter) sb.Append(',');
                sb.AppendFormat(" :{0}", paramName);
                isFirstParameter = false;
            }

            return sb.ToString();
        }

        /// <summary>
        /// <para>
        ///     This is a subset of the actual T-SQL identifier rules from
        ///     https://technet.microsoft.com/en-us/library/aa223962(v=sql.80).aspx.
        /// </para>
        /// <para>
        ///     The first character must be one of the following:
        ///     
        ///         - An English letter
        ///         - The underscore (_)
        ///         - "at" sign (@)
        ///         - number sign (#)
        /// </para>
        /// <para>
        ///     Subsequent characters may be:
        ///     
        ///         - Letters as defined in the Unicode Standard 2.0
        ///         - Decimal (Basic Latin) numbers
        ///         - The "at" sign (@)
        ///         - dollar sign ($)
        ///         - number sign (#)
        ///         - underscore (_)
        /// </para>
        /// <para>
        ///     Although the identifier must not be a Transact-SQL reserved 
        ///     word, this check does not filter out reserved words.  Using
        ///     a reserved word will result in a SQL error at runtime.
        /// </para> 
        /// </summary>
        private static readonly Regex SafeStoredProcParamName = new Regex(
            @"\A  [_\@\#a-z]  [\.\@\$\#\w\\]*  \Z",
            RegexOptions.IgnoreCase
            | RegexOptions.IgnorePatternWhitespace);

        /// <summary> EXPOSED FOR UNIT TESTING ONLY. </summary>
        [SuppressMessage("ReSharper", "UnusedParameter.Local",
            Justification = "This method is a reusable precondition check.")]
        internal static void EnsureSafeSqlIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)
                || !SafeStoredProcParamName.IsMatch(identifier))
            {
                throw new ArgumentException(string.Format(
                    "The stored procedure parameter name '{0}' is unsafe; " +
                    "safe names contain only letters, digits, and the " +
                    "underscore character.",
                    identifier));
            }
        }
    }
}
