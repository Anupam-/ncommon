﻿#region license
//Copyright 2010 Ritesh Rao 

//Licensed under the Apache License, Version 2.0 (the "License"); 
//you may not use this file except in compliance with the License. 
//You may obtain a copy of the License at 

//http://www.apache.org/licenses/LICENSE-2.0 

//Unless required by applicable law or agreed to in writing, software 
//distributed under the License is distributed on an "AS IS" BASIS, 
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
//See the License for the specific language governing permissions and 
//limitations under the License. 
#endregion

using System;
using System.Transactions;
using Common.Logging;
using NCommon.Data.Impl;

namespace NCommon.Data
{
    /// <summary>
    /// Helper class that allows starting and using a unit of work like:
    /// <![CDATA[
    ///     using (UnitOfWorkScope scope = new UnitOfWorkScope()) {
    ///         //Do some stuff here.
    ///         scope.Commit();
    ///     }
    /// 
    /// ]]>
    /// </summary>
    public class UnitOfWorkScope : IUnitOfWorkScope
    {
        bool _disposed;
        bool _commitAttempted;
        bool _completed;
        readonly Guid _scopeId = Guid.NewGuid();
        readonly ILog _logger = LogManager.GetLogger<UnitOfWorkScope>();

        /// <summary>
        /// Event fired when the scope is comitting.
        /// </summary>
        public event Action<IUnitOfWorkScope> ScopeComitting;

        /// <summary>
        /// Event fired when the scope is rollingback.
        /// </summary>
        public event Action<IUnitOfWorkScope> ScopeRollingback;

        /// <summary>
        /// Default Constuctor.
        /// Creates a new <see cref="UnitOfWorkScope"/> with the <see cref="System.Data.IsolationLevel.Serializable"/> 
        /// transaction isolation level.
        /// </summary>
        public UnitOfWorkScope() : this(false) { }

        /// <summary>
        /// Gets the unique Id of the <see cref="UnitOfWorkScope"/>.
        /// </summary>
        /// <value>A <see cref="Guid"/> representing the unique Id of the scope.</value>
        public Guid ScopeId
        {
            get { return _scopeId; }
        }

        /// <summary>
        /// Overloaded Constructor.
        /// Creates a new instance of the <see cref="UnitOfWorkScope"/> class.
        /// </summary>
        /// <param name="newTransaction">To create a new scope that does not enlist in an existing ambient 
        /// <see cref="UnitOfWorkScope"/> or <see cref="TransactionScope"/>, specify new, otherwise specify false.</param>
        public UnitOfWorkScope(bool newTransaction)
        {
            _logger.Info(x => x("New UnitOfWorkScope {0} started with newTransaction setting as : {1}", _scopeId, newTransaction));
            UnitOfWorkManager.CurrentTransactionManager.EnlistScope(this, newTransaction);
        }

        ///<summary>
        /// Commits the current running transaction in the scope.
        ///</summary>
        public void Commit()
        {
            Guard.Against<ObjectDisposedException>(_disposed,
                                                   "Cannot commit a disposed UnitOfWorkScope instance.");
            Guard.Against<InvalidOperationException>(_completed,
                                                     "This unit of work scope has been marked completed. A child scope participating in the " +
                                                     "transaction has rolledback and the transaction aborted. The parent scope cannot be commit.");

            
            _commitAttempted = true;
            OnCommit();
        }

        /// <summary>
        /// Marks the scope as completed.
        /// Used for internally by NCommon and should not be used by consumers.
        /// </summary>
        public void Complete()
        {
            _completed = true;
        }

        /// <summary>
        /// 
        /// </summary>
        void OnCommit()
        {
            _logger.Info(x => x("UnitOfWorkScope {0} Comitting.", _scopeId));
            if (ScopeComitting != null)
                ScopeComitting(this);
        }

        /// <summary>
        /// 
        /// </summary>
        void OnRollback()
        {
            _logger.Info(x => x("UnitOfWorkScope {0} Rolling back.", _scopeId));
            if (ScopeRollingback != null)
                ScopeRollingback(this);
        }

        /// <summary>
        /// Disposes off the <see cref="UnitOfWorkScope"/> insance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes off the managed and un-managed resources used.
        /// </summary>
        /// <param name="disposing"></param>
        void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    if (!_commitAttempted && !_completed && UnitOfWorkSettings.AutoCompleteScope)
                        OnCommit();
                    else
                        OnRollback();
                }
                finally
                {
                    ScopeComitting = null;
                    ScopeRollingback = null;
                    _disposed = true;
                }
            }
        }
    }
}
