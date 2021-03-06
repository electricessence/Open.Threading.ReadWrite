﻿/*!
 * @author electricessence / https://github.com/electricessence/
 * Some portions of this code are based upon code from Stephen Cleary's Nitro library.
 */

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Threading;

namespace Open.Threading
{

	public static class ReaderWriterLockSlimExensions
	{
		/// <summary>
		/// Extension for checking lock status... Should only be used for debugging.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public static bool IsLockFree(this ReaderWriterLockSlim target)
		{
			if (target is null)
				throw new NullReferenceException();
			Contract.EndContractBlock();

			return target.CurrentReadCount == 0
				&& target.RecursiveReadCount == 0
				&& target.RecursiveUpgradeCount == 0
				&& target.RecursiveWriteCount == 0
				&& target.WaitingReadCount == 0
				&& target.WaitingWriteCount == 0
				&& target.WaitingUpgradeCount == 0
				&& !target.IsReadLockHeld
				&& !target.IsUpgradeableReadLockHeld
				&& !target.IsWriteLockHeld;
		}


		internal static void ValidateMillisecondsTimeout(int? millisecondsTimeout)
		{
			if ((millisecondsTimeout ?? 0) < 0)
				throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), millisecondsTimeout, "Cannot be a negative value.");
		}


		/// <summary>
		/// Simplifies entering a timeout or no timeout based read lock.
		/// </summary>
		/// <returns>True if a lock was acquired, false if not.</returns>
		public static bool EnterReadLock(this ReaderWriterLockSlim target,
			int? millisecondsTimeout, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			if (millisecondsTimeout is null)
				target.EnterReadLock();
			else if (!target.TryEnterReadLock(millisecondsTimeout.Value))
			{
				if (throwsOnTimeout)
					throw new TimeoutException(
						$"Could not acquire a read lock within the timeout specified. (millisecondsTimeout={millisecondsTimeout})");

				return false;
			}

			return true;
		}

		/// <summary>
		/// Simplifies entering a timeout or no timeout based upgradeable read lock.
		/// </summary>
		/// <returns>True if a lock was acquired, false if not.</returns>
		public static bool EnterUpgradeableReadLock(this ReaderWriterLockSlim target,
			int? millisecondsTimeout, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			if (millisecondsTimeout is null)
				target.EnterUpgradeableReadLock();
			else if (!target.TryEnterUpgradeableReadLock(millisecondsTimeout.Value))
			{
				if (throwsOnTimeout)
					throw new TimeoutException(
						$"Could not acquire an upgradeable read lock within the timeout specified. (millisecondsTimeout={millisecondsTimeout})");

				return false;
			}

			return true;
		}

		/// <summary>
		/// Simplifies entering a timeout or no timeout based write lock.
		/// </summary>
		/// <returns>True if a lock was acquired, false if not.</returns>
		public static bool EnterWriteLock(this ReaderWriterLockSlim target,
			int? millisecondsTimeout, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			if (millisecondsTimeout is null)
				target.EnterWriteLock();
			else if (!target.TryEnterWriteLock(millisecondsTimeout.Value))
			{
				if (throwsOnTimeout)
					throw new TimeoutException(
						$"Could not acquire a write lock within the timeout specified. (millisecondsTimeout={millisecondsTimeout})");

				return false;
			}

			return true;
		}

		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing read access.
		/// </summary>
		/// <param name="target">ReaderWriterLockSlim to execute on.</param>
		/// <param name="closure">Action to execute once a lock is acquired.</param>
		/// <param name="millisecondsTimeout">Indicates if and for how long a timeout is used to acquire a lock.</param>
		/// <param name="throwsOnTimeout">If this parameter is true, then if a timeout addValue is reached, an exception is thrown.</param>
		/// <returns>Returns false if a timeout is reached.</returns>
		public static bool Read(this ReaderWriterLockSlim target,
			Action closure,
			int? millisecondsTimeout = null, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			if (closure is null)
				throw new ArgumentNullException(nameof(closure));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			var lockHeld = false;
			try
			{
				lockHeld = target.EnterReadLock(millisecondsTimeout, throwsOnTimeout);
				closure();
			}
			finally
			{
				if (lockHeld)
					target.ExitReadLock();
			}

			return lockHeld;
		}

		/// <summary>
		/// Generates a disposable ReadLock object for encapsulating code within a using(){} statement.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim instance.</param>
		/// <param name="millisecondsTimeout">Optional timeout value.  If timeout is exceeded, an exception is thrown.</param>
		/// <returns>The ReadLock disposable.</returns>
		public static ReadLock ReadLock(this ReaderWriterLockSlim target,
			int? millisecondsTimeout = null)
		{
			return new ReadLock(target, millisecondsTimeout);
		}

		/// <summary>
		/// Generates a disposable WriteLock object for encapsulating code within a using(){} statement.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim instance.</param>
		/// <param name="millisecondsTimeout">Optional timeout value.  If timeout is exceeded, an exception is thrown.</param>
		/// <returns>The WriteLock disposable.</returns>
		public static WriteLock WriteLock(this ReaderWriterLockSlim target,
			int? millisecondsTimeout = null)
		{
			return new WriteLock(target, millisecondsTimeout);
		}

		/// <summary>
		/// Generates a disposable UpgradableReadLock object for encapsulating code within a using(){} statement.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim instance.</param>
		/// <param name="millisecondsTimeout">Optional timeout value.  If timeout is exceeded, an exception is thrown.</param>
		/// <returns>The UpgradableReadLock disposable.</returns>
		public static UpgradableReadLock UpgradableReadLock(this ReaderWriterLockSlim target,
			int? millisecondsTimeout = null)
		{
			return new UpgradableReadLock(target, millisecondsTimeout);
		}

		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing read access.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim to use for locking.</param>
		/// <param name="result">The value from the closure.</param>
		/// <param name="closure">Action to execute once a lock is acquired.</param>
		/// <param name="millisecondsTimeout">Indicates if and for how long a timeout is used to acquire a lock.</param>
		/// <param name="throwsOnTimeout">If this parameter is true, then if a timeout addValue is reached, an exception is thrown.</param>
		/// <returns>Returns false if a timeout is reached.</returns>
		public static bool Read<T>(this ReaderWriterLockSlim target,
			out T result, Func<T> closure,
			int? millisecondsTimeout = null, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			if (closure is null)
				throw new ArgumentNullException(nameof(closure));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			var lockHeld = false;
			try
			{
				lockHeld = target.EnterReadLock(millisecondsTimeout, throwsOnTimeout);
				result = closure();
			}
			finally
			{
				if (lockHeld)
					target.ExitReadLock();
			}

			return lockHeld;
		}


		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing upgradeable read access.
		/// This method allows for entering a write lock within the query, but there can only be one upgraded thread at at time.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim to use for locking.</param>
		/// <param name="closure">Action to execute once a lock is acquired.</param>
		/// <param name="millisecondsTimeout">Indicates if and for how long a timeout is used to acquire a lock.</param>
		/// <param name="throwsOnTimeout">If this parameter is true, then if a timeout addValue is reached, an exception is thrown.</param>
		/// <returns>Returns false if a timeout is reached.</returns>
		public static bool ReadUpgradeable(this ReaderWriterLockSlim target,
			Action closure,
			int? millisecondsTimeout = null, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			if (closure is null)
				throw new ArgumentNullException(nameof(closure));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			var lockHeld = false;
			try
			{
				lockHeld = target.EnterUpgradeableReadLock(millisecondsTimeout, throwsOnTimeout);
				closure();
			}
			finally
			{
				if (lockHeld)
					target.ExitUpgradeableReadLock();
			}

			return lockHeld;
		}

		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing upgradeable read access.
		/// This method allows for entering a write lock within the query, but there can only be one upgraded thread at at time.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim to use for locking.</param>
		/// <param name="result">The value from the closure.</param>
		/// <param name="closure">Action to execute once a lock is acquired.</param>
		/// <param name="millisecondsTimeout">Indicates if and for how long a timeout is used to acquire a lock.</param>
		/// <param name="throwsOnTimeout">If this parameter is true, then if a timeout addValue is reached, an exception is thrown.</param>
		/// <returns>Returns false if a timeout is reached.</returns>
		public static bool ReadUpgradeable<T>(this ReaderWriterLockSlim target,
			out T result, Func<T> closure,
			int? millisecondsTimeout = null, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			if (closure is null)
				throw new ArgumentNullException(nameof(closure));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			var lockHeld = false;
			try
			{
				lockHeld = target.EnterUpgradeableReadLock(millisecondsTimeout, throwsOnTimeout);
				result = closure();
			}
			finally
			{
				if (lockHeld)
					target.ExitUpgradeableReadLock();
			}

			return lockHeld;
		}


		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing write access.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim to use for locking.</param>
		/// <param name="closure">Action to execute once a lock is acquired.</param>
		/// <param name="millisecondsTimeout">Indicates if and for how long a timeout is used to acquire a lock.</param>
		/// <param name="throwsOnTimeout">If this parameter is true, then if a timeout addValue is reached, an exception is thrown.</param>
		/// <returns>Returns false if a timeout is reached.</returns>
		public static bool Write(this ReaderWriterLockSlim target,
			Action closure,
			int? millisecondsTimeout = null, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			if (closure is null)
				throw new ArgumentNullException(nameof(closure));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			var lockHeld = false;
			try
			{
				lockHeld = target.EnterWriteLock(millisecondsTimeout, throwsOnTimeout);
				closure();
			}
			finally
			{
				if (lockHeld)
					target.ExitWriteLock();
			}

			return lockHeld;
		}

		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing write access.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim to use for locking.</param>
		/// <param name="result">The value from the closure.</param>
		/// <param name="closure">Action to execute once a lock is acquired.</param>
		/// <param name="millisecondsTimeout">Indicates if and for how long a timeout is used to acquire a lock.</param>
		/// <param name="throwsOnTimeout">If this parameter is true, then if a timeout addValue is reached, an exception is thrown.</param>
		/// <returns>Returns false if a timeout is reached.</returns>
		public static bool Write<T>(this ReaderWriterLockSlim target,
			out T result, Func<T> closure,
			int? millisecondsTimeout = null, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			if (closure is null)
				throw new ArgumentNullException(nameof(closure));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			var lockHeld = false;
			try
			{
				lockHeld = target.EnterWriteLock(millisecondsTimeout, throwsOnTimeout);
				result = closure();
			}
			finally
			{
				if (lockHeld)
					target.ExitWriteLock();
			}

			return lockHeld;
		}

		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing write access.  Starts by executing the condition without a lock.
		/// Note: Passing a boolean to the condition when a lock is acquired helps if it is important to the cosuming logic to avoid recursive locking.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim to use for locking.</param>
		/// <param name="condition">Takes a bool where false means no lock and true means a write lock.  Returns true if it should execute the query Action.</param>
		/// <param name="closure">Action to execute once a lock is acquired.</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to to wait before timing out.  Null value will wait indefinitely.</param>
		/// <param name="throwsOnTimeout">If true, will throw an timeout exception if the timeout is reached.</param>
		/// <returns>Returns false if a timeout is reached.</returns>
		public static bool WriteConditional(this ReaderWriterLockSlim target,
			Func<bool, bool> condition, Action closure,
			int? millisecondsTimeout = null, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			if (condition is null)
				throw new ArgumentNullException(nameof(condition));
			if (closure is null)
				throw new ArgumentNullException(nameof(closure));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			var lockHeld = true;
			if (condition(false))
			{
				lockHeld = target.Write(() =>
				{
					if (condition(true))
						closure();
				},
				millisecondsTimeout,
				throwsOnTimeout);
			}

			return lockHeld;
		}

		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing write access.  Starts by executing the condition without a lock.
		/// Note: Passing a boolean to the condition when a lock is acquired helps if it is important to the cosuming logic to avoid recursive locking.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim to use for locking.</param>
		/// <param name="result">The value from the closure.</param>
		/// <param name="condition">Takes a bool where false means no lock and true means a write lock.  Returns true if it should execute the query Action.</param>
		/// <param name="closure">Action to execute once a lock is acquired.</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to to wait before timing out.  Null value will wait indefinitely.</param>
		/// <param name="throwsOnTimeout">If true, will throw an timeout exception if the timeout is reached.</param>
		/// <returns>Returns false if a timeout is reached.</returns>
		public static bool WriteConditional<T>(this ReaderWriterLockSlim target,
			ref T result, Func<bool, bool> condition, Func<T> closure,
			int? millisecondsTimeout = null, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			if (condition is null)
				throw new ArgumentNullException(nameof(condition));
			if (closure is null)
				throw new ArgumentNullException(nameof(closure));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			var r = result;
			bool lockHeld = true, written = false;
			if (condition(false))
			{
				lockHeld = target.Write(out written, () =>
				{
					var w = condition(true);
					if (w) r = closure();
					return w;
				},
				millisecondsTimeout,
				throwsOnTimeout);
			}
			if (written)
				result = r;

			return lockHeld;
		}

		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing write access.  Starts by executing the condition with a read lock.  Then if necessary after releasing the read lock, acquires a write lock.
		/// Note: Passing a boolean to the condition when a lock is acquired helps if it is important to the cosuming logic to avoid recursive locking.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim to use for locking.</param>
		/// <param name="condition">Takes a bool where false means a read lock and true means a write lock.  Returns true if it should execute the query Action.</param>
		/// <param name="closure">Action to execute once a lock is acquired.</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to to wait before timing out.  Null value will wait indefinitely.</param>
		/// <param name="throwsOnTimeout">If true, will throw an timeout exception if the timeout is reached.</param>
		/// <returns>Returns false if a timeout is reached.</returns>
		public static bool ReadWriteConditional(this ReaderWriterLockSlim target,
			Func<LockType, bool> condition, Action closure,
			int? millisecondsTimeout = null, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			if (condition is null)
				throw new ArgumentNullException(nameof(condition));
			if (closure is null)
				throw new ArgumentNullException(nameof(closure));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			var c = false;
			var lockHeld = target.Read(() => c = condition(LockType.Read), millisecondsTimeout, throwsOnTimeout);
			if (lockHeld && c)
			{
				lockHeld = target.Write(() =>
				{
					if (condition(LockType.Write))
						closure();
				},
				millisecondsTimeout,
				throwsOnTimeout);

			}

			return lockHeld;
		}

		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing write access.  Starts by executing the condition with a read lock.  Then if necessary after releasing the read lock, acquires a write lock.
		/// Note: Passing a boolean to the condition when a lock is acquired helps if it is important to the cosuming logic to avoid recursive locking.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim to use for locking.</param>
		/// <param name="result">The value from the closure.</param>
		/// <param name="condition">Takes a bool where false means a read lock and true means a write lock.  Returns true if it should execute the query Action.</param>
		/// <param name="closure">Action to execute once a lock is acquired.</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to to wait before timing out.  Null value will wait indefinitely.</param>
		/// <param name="throwsOnTimeout">If true, will throw an timeout exception if the timeout is reached.</param>
		/// <returns>Returns false if a timeout is reached.</returns>
		public static bool ReadWriteConditional<T>(this ReaderWriterLockSlim target,
			ref T result, Func<LockType, bool> condition, Func<T> closure,
			int? millisecondsTimeout = null, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			if (condition is null)
				throw new ArgumentNullException(nameof(condition));
			if (closure is null)
				throw new ArgumentNullException(nameof(closure));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			var r = result;
			bool c = false, written = false;
			var lockHeld = target.Read(() => c = condition(LockType.Read), millisecondsTimeout, throwsOnTimeout);
			if (lockHeld && c)
			{
				lockHeld = target.Write(out written, () =>
				{
					var w = condition(LockType.Write);
					if (w) r = closure();
					return w;
				},
				millisecondsTimeout,
				throwsOnTimeout);

			}
			if (written)
				result = r;


			return lockHeld;
		}

		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing write access.  Starts by executing the condition with an upgradeable read lock before acquiring a write lock.
		/// Note: Passing a boolean to the condition when a lock is acquired helps if it is important to the cosuming logic to avoid recursive locking.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim to use for locking.</param>
		/// <param name="condition">Takes a bool where false means a read lock and true means a write lock.  Returns true if it should execute the query Action.</param>
		/// <param name="closure">Action to execute once a lock is acquired.</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to to wait before timing out.  Null value will wait indefinitely.</param>
		/// <param name="throwsOnTimeout">If true, will throw an timeout exception if the timeout is reached.</param>
		/// <returns>Returns false if a timeout is reached.</returns>
		public static bool ReadUpgradeableWriteConditional(this ReaderWriterLockSlim target,
			Func<bool> condition, Action closure,
			int? millisecondsTimeout = null, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			if (condition is null)
				throw new ArgumentNullException(nameof(condition));
			if (closure is null)
				throw new ArgumentNullException(nameof(closure));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			var writeLocked = true; // Initialize true so that if only only reading it still returns true.
			var readLocked = target.ReadUpgradeable(() =>
			{
				if (condition())
					writeLocked = target.Write(closure, millisecondsTimeout, throwsOnTimeout);
			});

			return readLocked && writeLocked;
		}

		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing write access.  Starts by executing the condition with an upgradeable read lock before acquiring a write lock.
		/// Note: Passing a boolean to the condition when a lock is acquired helps if it is important to the cosuming logic to avoid recursive locking.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim to use for locking.</param>
		/// <param name="result">The value from the closure.</param>
		/// <param name="condition">Takes a bool where false means a read lock and true means a write lock.  Returns true if it should execute the query Action.</param>
		/// <param name="closure">Action to execute once a lock is acquired.</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to to wait before timing out.  Null value will wait indefinitely.</param>
		/// <param name="throwsOnTimeout">If true, will throw an timeout exception if the timeout is reached.</param>
		/// <returns>Returns false if a timeout is reached.</returns>
		public static bool ReadUpgradeableWriteConditional<T>(this ReaderWriterLockSlim target,
			ref T result, Func<bool> condition, Func<T> closure,
			int? millisecondsTimeout = null, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			if (condition is null)
				throw new ArgumentNullException(nameof(condition));
			if (closure is null)
				throw new ArgumentNullException(nameof(closure));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			var r = result;
			var writeLocked = true; // Initialize true so that if only only reading it still returns true.
			var written = false;
			var readLocked = target.ReadUpgradeable(() =>
			{
				if (!condition()) return;
				// out r ensures that it IS written to.
				writeLocked = target.Write(out r, closure, millisecondsTimeout, throwsOnTimeout);
				written = true;
			});

			if (written)
				result = r;

			return readLocked && writeLocked;
		}

		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing write access.  Starts by executing the condition with a read lock.
		/// Then if necessary executes the condition with an upgradeable read lock before acquiring a write lock.
		/// Note: Passing a boolean to the condition when a lock is acquired helps if it is important to the cosuming logic to avoid recursive locking.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim to use for locking.</param>
		/// <param name="condition">Takes a bool where false means a read lock and true means a write lock.  Returns true if it should execute the query Action.</param>
		/// <param name="closure">Action to execute once a lock is acquired.</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to to wait before timing out.  Null value will wait indefinitely.</param>
		/// <param name="throwsOnTimeout">If true, will throw an timeout exception if the timeout is reached.</param>
		/// <returns>Returns false if a timeout is reached.</returns>
		public static bool ReadWriteConditionalOptimized(this ReaderWriterLockSlim target,
			Func<LockType, bool> condition, Action closure,
			int? millisecondsTimeout = null, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			if (condition is null)
				throw new ArgumentNullException(nameof(condition));
			if (closure is null)
				throw new ArgumentNullException(nameof(closure));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			var c = false;
			var lockHeld = target.Read(() => c = condition(LockType.Read), millisecondsTimeout, throwsOnTimeout);
			return lockHeld && (!c || target.ReadUpgradeableWriteConditional(() => condition(LockType.ReadUpgradeable), closure, millisecondsTimeout, throwsOnTimeout));
		}

		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing write access.  Starts by executing the condition with a read lock.
		/// Then if necessary executes the condition with an upgradeable read lock before acquiring a write lock.
		/// Note: Passing a boolean to the condition when a lock is acquired helps if it is important to the cosuming logic to avoid recursive locking.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim to use for locking.</param>
		/// <param name="result">The value from the closure.</param>
		/// <param name="condition">Takes a bool where false means a read lock and true means a write lock.  Returns true if it should execute the query Action.</param>
		/// <param name="closure">Action to execute once a lock is acquired.</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to to wait before timing out.  Null value will wait indefinitely.</param>
		/// <param name="throwsOnTimeout">If true, will throw an timeout exception if the timeout is reached.</param>
		/// <returns>Returns false if a timeout is reached.</returns>
		public static bool ReadWriteConditionalOptimized<T>(this ReaderWriterLockSlim target,
			ref T result, Func<LockType, bool> condition, Func<T> closure,
			int? millisecondsTimeout = null, bool throwsOnTimeout = false)
		{
			if (target is null)
				throw new NullReferenceException();
			if (condition is null)
				throw new ArgumentNullException(nameof(condition));
			if (closure is null)
				throw new ArgumentNullException(nameof(closure));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			var c = false;
			var lockHeld = target.Read(() => c = condition(LockType.Read), millisecondsTimeout, throwsOnTimeout);
			return lockHeld && (!c || target.ReadUpgradeableWriteConditional(ref result, () => condition(LockType.ReadUpgradeable), closure, millisecondsTimeout, throwsOnTimeout));
		}

		/// <summary>
		/// If the getValue delegate returns null, the value is acquired from the createValue delegate.
		/// </summary>
		/// <typeparam name="T">The return type.</typeparam>
		/// <param name="target">The ReaderWriterLockSlim to use for locking.</param>
		/// <param name="getValue">The function to get the value.</param>
		/// <param name="createValue">The create value factory.</param>
		/// <returns>The value acquired.</returns>
		public static T GetOrCreateValue<T>(this ReaderWriterLockSlim target,
			Func<T?> getValue, Func<T> createValue)
			where T : class
		{
			if (target is null)
				throw new NullReferenceException();
			if (getValue is null)
				throw new ArgumentNullException(nameof(getValue));
			if (createValue is null)
				throw new ArgumentNullException(nameof(createValue));
			Contract.EndContractBlock();

			T? result = null;
			target.ReadWriteConditionalOptimized(
				ref result, lockType => (result = getValue()) is null, createValue);

			return result!;
		}

		/// <summary>
		/// If the getValue delegate returns null, the value is acquired from the createValue delegate.
		/// </summary>
		/// <typeparam name="T">The return type.</typeparam>
		/// <param name="target">The ReaderWriterLockSlim to use for locking.</param>
		/// <param name="getValue">The function to get the value.</param>
		/// <param name="createValue">The create value factory.</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to to wait before timing out.  Null value will wait indefinitely.</param>
		/// <param name="throwsOnTimeout">If true, will throw an timeout exception if the timeout is reached.</param>
		/// <returns>The value acquired.</returns>
		public static T? GetOrCreateValue<T>(this ReaderWriterLockSlim target,
			Func<T?> getValue, Func<T> createValue,
			int? millisecondsTimeout, bool throwsOnTimeout = false)
			where T : class
		{
			if (target is null)
				throw new NullReferenceException();
			if (getValue is null)
				throw new ArgumentNullException(nameof(getValue));
			if (createValue is null)
				throw new ArgumentNullException(nameof(createValue));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			T? result = null;
			target.ReadWriteConditionalOptimized(
				ref result, lockType => (result = getValue()) is null, createValue,
				millisecondsTimeout,
				throwsOnTimeout);

			return result;
		}

		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing read access.
		/// </summary>
		/// <param name="target">The ReaderWriterLockSlim to use for locking.</param>
		/// <param name="result">The result if a lock is acquired..</param>
		/// <param name="valueFactory">The value factory.</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to to wait before timing out.  Null value will wait indefinitely.</param>
		/// <returns>Returns false if a timeout is reached.</returns>
		public static bool TryReadValue<T>(this ReaderWriterLockSlim target,
#if NETSTANDARD2_1
			[NotNullWhen(true)]
#endif
			out T result, Func<T> valueFactory,
			int? millisecondsTimeout = null)
		{
			if (target is null)
				throw new NullReferenceException();
			if (valueFactory is null)
				throw new ArgumentNullException(nameof(valueFactory));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			T r = default!;
			var lockHeld = target.Read(() => r = valueFactory(), millisecondsTimeout);
			result = r!;
			return lockHeld;
		}

		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing read access.
		/// </summary>
		/// <returns>Returns false if a timeout is reached.</returns>
		public static bool TryWriteValue<T>(this ReaderWriterLockSlim target,
#if NETSTANDARD2_1
			[NotNullWhen(true)]
#endif
			out T result, Func<T> valueFactory,
			int? millisecondsTimeout = null)
		{
			if (target is null)
				throw new NullReferenceException();
			if (valueFactory is null)
				throw new ArgumentNullException(nameof(valueFactory));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			T r = default!;
			var lockHeld = target.Write(() => r = valueFactory(), millisecondsTimeout);
			result = r!;
			return lockHeld;
		}

		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing read access.
		/// </summary>
		public static T ReadValue<T>(this ReaderWriterLockSlim target,
			Func<T> valueFactory,
			int? millisecondsTimeout = null)
		{
			if (target is null)
				throw new NullReferenceException();
			if (valueFactory is null)
				throw new ArgumentNullException(nameof(valueFactory));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			T result = default!;
			target.Read(() => result = valueFactory(), millisecondsTimeout, true);
			return result!;
		}

		/// <summary>
		/// ReaderWriterLockSlim extension for synchronizing write access.
		/// </summary>
		public static T WriteValue<T>(this ReaderWriterLockSlim target,
			Func<T> valueFactory,
			int? millisecondsTimeout = null)
		{
			if (target is null)
				throw new NullReferenceException();
			if (valueFactory is null)
				throw new ArgumentNullException(nameof(valueFactory));
			ValidateMillisecondsTimeout(millisecondsTimeout);
			Contract.EndContractBlock();

			T result = default!;
			target.Write(() => result = valueFactory(), millisecondsTimeout, true);
			return result!;
		}


	}
}
