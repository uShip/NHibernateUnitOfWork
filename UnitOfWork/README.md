h1. NHibernate UnitOfWork

h2. Build and NuGet

![NHibernateUnitOfWork continuous integration status](https://ci.appveyor.com/api/projects/status/github/EdVinyard/NHibernateUnitOfWork?branch=master&svg=true)

[NuGet package](https://www.nuget.org/packages/UOW/)

h2. What It Is

NHibernate UnitOfWork is an automatic session and transaction management tool for short-lived database interactions.  It's not a substitute for longer lived session and transactions to take advantage of NHibernate caching, and database transaction management that better matches your business transactions.  For example, we use [request-scoped sessions and transactions via StructureMap Nested Containers](http://structuremap.github.io/the-container/nested-containers/) with both ASP.NET Web API and message consumer applications to those ends.

h2. When to Use It

Prefer it if...
- you need to bypass the first-level cache in order to access to up-to-date information that may have been changed by another process or server, or...
- your change must be written to the database immediately, especially when...
- you are resolving a race condition between threads, processes, or servers.

Avoid it if...
- caching of DB queries within a longer-lived Session would benefit the work you are doing, over the course of the entire request, or...
- your changes should not be committed to the database if a later piece of the business transaction fails.

h2. How It Works

The SessionFactoryExtensions.UnitOfWork extension methods offer automatic NHibernate Session and Transaction management, including:

- guaranteed Session close and disposal
- guaranteed Transaction close and disposal
- automatic Transaction commit/rollback

h2. How to Use It

# use dependency injection to get an instance of ISessionFactory
# call `_sessionFactory.UnitOfWork()` a new session and new transaction are opened
# in the delegate you pass into `UnitOfWork()`, you may query, insert, update and delete
# all of your changes will be commited (finalized) in the database no later than the end of the delegate
# catch exceptions that originate inside your delegate, if you can sanely recover from them
# If an exception is thrown by your delegate, the transaction will be rolled back.  Otherwise it will be committed.  If you'd like to choose whether to commit or rollback based on other factors, call `session.Transaction.Commit()` or `.Rollback()` in the delegate.  Both the Transaction and Session will always be closed and disposed automatically

h3. Example: Read the latest information from the database

In the following code, a UnitOfWork is used to perform data access, and the UnitOfWork returns a value into the calling scope.  If the data queried has been modified by another process or server, a stale copy of the data may be cached in the ambient (Request or Thread scoped) Session.  In this case, we need to be absolutely sure of the most up-to-date value stored in the database.

	public class UserNameFinder {
	    private readonly ISessionFactory _sessionFactory;
	 
	    public UserNameFinder(ISessionFactory sessionFactory) {
	        _sessionFactory = sessionFactory;
	    }
	 
	    public string GetUserName(int userGeneratedId) {
	        return _sessionFactory.UnitOfWork(session => {
				return session.Query<User>()
	                .FirstOrDefault(x => x.GeneratedID == userGeneratedId)
	                .IfNotNull(x => x.UserName);
			}
		}
	}

For contrast, the equivalent code, written without the convenience of UnitOfWork, follows.

	public string GetUserName(int userGeneratedId) {
		using (var session = _sessionFactory.OpenSession())
		using (var trans = session.BeginTransaction())
		{
		    try
		    {
		        var username = session.Query<User>()
	                .FirstOrDefault(x => x.GeneratedID == userGeneratedId)
	                .IfNotNull(x => x.UserName);
		        trans.Commit();
				return username;
		    }
		    catch (Exception originalExc)
		    {
		        try
		        {
		            trans.Rollback();
	                throw;
		        }
		        catch (Exception rollbackExc)
		        {
		            throw new AggregateException(originalExc, rollbackExc);
		        }
		    }
		}
	}

h3. Example: Modify data in the database immediately

In the following code, a UnitOfWork is used to modify data, and the UnitOfWork does not return any value at all.

	public class UserPhoneNumberModifier {
	    private readonly ISessionFactory _sessionFactory;

	    public UserPhoneNumberModifier(ISessionFactory sessionFactory) {
	        _sessionFactory = sessionFactory;
	    }
	 
	    public void ChangePhoneNumber(int userId, string newPhoneNumber) {
			try {
		    	_sessionFactory.UnitOfWork(session => {
					var user = session.Get<User>(userId);
		            if (user != null) {
		                user.PhoneNumber = newPhoneNumber; // may throw
						session.Update(user);
		            }
					// session.Transaction.Commit() will happen automatically
					// unless an exception has already occurred
				});
			} catch (InvalidPhoneNumberException exc) {
				throw new ArgumentException("invalid phone number", exc);
			}
		}
	}

h3. Example: Manually commit or rollback

In the following code, the UnitOfWork delegate determines whether to commit or rollback, without relying on the auto-commit/rollback functionality.

	public class UserModifier {
	    private readonly ISessionFactory _sessionFactory;

	    public UserModifier(ISessionFactory sessionFactory) {
	        _sessionFactory = sessionFactory;
	    }
	 
	    public void ChangePhoneNumber(
	        int userId, 
	        string newPhoneNumber) 
		{
	    	_sessionFactory.UnitOfWork(session => {
				var user = session.Get<User>(userId);
	            if (user != null) {
	                user.PhoneNumber = newPhoneNumber;
					session.Update(user);
	            }
	 
				if (user.Suspended) {
					session.Transaction.Rollback();
				} else {
					session.Transaction.Commit();
				}
			}
		}
	}
