using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActionRetry
{
    public class Retry
    {
        #region Enum
        public enum Backoff
        {
            Linear,
            Quadratic,
            Cubic
        }
        #endregion

        #region Delegates
        public delegate bool ToRetry();

        public delegate Task<bool> ToRetryASync();
        #endregion

        #region EventArgs

        public class AttemptStartEventArgs : EventArgs
        {
            public readonly int Attempt;

            public AttemptStartEventArgs(int attempt) 
                => Attempt = attempt;
        }

        #endregion

        #region Events

        public event EventHandler<AttemptStartEventArgs>
            AttemptStart,
            AttemptFailure;

        public event UnhandledExceptionEventHandler
            IgnoredException;

        #endregion

        #region Variable Declaration

        private readonly ToRetry toRetry;
        private readonly ToRetryASync toRetryASync;
        private readonly Backoff backoff;

        public readonly int
            Attempts,
            InitialDelay;

        public readonly bool
            IgnoreExceptions,
            IsASync;

        public readonly HashSet<Type>
            ExceptionWhitelist,
            ExceptionBlacklist;

        internal bool? wasSuccessful;

        #endregion

        #region Methods

        #region Constructors
        public Retry(
            ToRetry toRetry,
            int attempts = 5,
            int initialDelay = 50,
            bool ignoreExceptions = false,
            HashSet<Type> exceptionWhitelist = default(HashSet<Type>),
            HashSet<Type> exceptionBlacklist = default(HashSet<Type>),
            Backoff backoff = Backoff.Linear
            )
            : this(attempts, initialDelay, ignoreExceptions, exceptionWhitelist, exceptionBlacklist, backoff)
        {
            this.toRetry = toRetry;
            IsASync = false;
        }

        public Retry(
            ToRetryASync toRetryASync,
            int attempts = 5,
            int initialDelay = 50,
            bool ignoreExceptions = false,
            HashSet<Type> exceptionWhitelist = default(HashSet<Type>),
            HashSet<Type> exceptionBlacklist = default(HashSet<Type>),
            Backoff backoff = Backoff.Linear
            )
            : this(attempts, initialDelay, ignoreExceptions, exceptionWhitelist, exceptionBlacklist, backoff)
        {
            this.toRetryASync = toRetryASync;
            IsASync = true;
        }

        private Retry(
            int attempts,
            int initialDelay,
            bool ignoreExceptions,
            HashSet<Type> exceptionWhitelist,
            HashSet<Type> exceptionBlacklist,
            Backoff backoff
            )
        {
            Attempts = attempts;
            InitialDelay = initialDelay;
            IgnoreExceptions = ignoreExceptions;
            ExceptionWhitelist = exceptionWhitelist;
            ExceptionBlacklist = exceptionBlacklist;
            this.backoff = backoff;
        }
        #endregion

        #region Public Methods

        public bool Begin()
        {
            if (IsASync)
                throw new ArgumentException($"{nameof(Retry)} is not synchronous");

            ThrowIfNull(($"{nameof(toRetry)}", toRetry));
            
            for (int attempt = 1; attempt <= Attempts; attempt++)
            {
                AttemptStart?.Invoke(this, new AttemptStartEventArgs(attempt));

                try
                {
                    if (toRetry())
                        return true;
                }
                catch (Exception ex)
                {
                    CheckContinueThrow(
                        ex,
                        IgnoreExceptions,
                        ExceptionWhitelist,
                        ExceptionBlacklist
                        );

                    IgnoredException?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
                }

                AttemptFailure?.Invoke(this, new AttemptStartEventArgs(attempt));
                
                Thread.Sleep(
                        GetWaitTime(
                            attempt,
                            InitialDelay,
                            backoff
                    ));
            }

            return false;
        }

        public async Task<bool> BeginASync()
        {
            if (!IsASync)
                throw new ArgumentException($"{nameof(Retry)} is not asynchronous");

            ThrowIfNull(($"{nameof(toRetryASync)}", toRetryASync));

            for (int attempt = 1; attempt <= Attempts; attempt++)
            {
                AttemptStart?.Invoke(this, new AttemptStartEventArgs(attempt));

                try
                {
                    if (await toRetryASync())
                        return true;
                }
                catch (Exception ex)
                {
                    CheckContinueThrow(
                        ex,
                        IgnoreExceptions,
                        ExceptionWhitelist,
                        ExceptionBlacklist
                        );

                    IgnoredException?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
                }

                AttemptFailure?.Invoke(this, new AttemptStartEventArgs(attempt));

                await Task.Delay(
                        GetWaitTime(
                            attempt,
                            InitialDelay,
                            backoff
                    ));
            }

            return false;
        }

        #endregion

        #region Private Methods
        private static void ThrowIfNull(params (string name, object val)[] parameters)
        {
            foreach (var param in parameters)
                if (param.val == null)
                    throw new ArgumentNullException($"parameter cannot be null: {param.name}");
        }

        private static void CheckContinueThrow(
            Exception exception,
            bool ignoreExceptions,
            HashSet<Type> exceptionWhitelist,
            HashSet<Type> exceptionBlacklist
            )
        {
            if (ignoreExceptions)
                return;

            bool
                useWhitelist = (exceptionWhitelist != default(HashSet<Type>)),
                useBlacklist = (exceptionBlacklist != default(HashSet<Type>));

            if (
                    (
                        !useWhitelist ||
                        !exceptionWhitelist.Contains(exception.GetType())
                    ) &&
                    (
                        !useBlacklist ||
                        exceptionBlacklist.Contains(exception.GetType())
                    )
                ) throw exception;
        }

        private static int GetWaitTime(
            int attempt,
            int initialDelay,
            Backoff retryType)
        {
            switch (retryType)
            {
                // ex: 50, 100, 150, 200...
                case (Backoff.Linear):
                    return initialDelay * attempt;

                // ex: 50, 100, 200, 400...
                case (Backoff.Quadratic):
                    return initialDelay * (2 ^ (attempt - 1));

                // ex: 50, 150, 450, 1350...
                case (Backoff.Cubic):
                    return initialDelay * (3 ^ (attempt - 1));

                default:
                    throw new NotImplementedException($"{nameof(Backoff)}: {retryType}; is not supported");
            }
        }

        #endregion

        #endregion

    }
}
