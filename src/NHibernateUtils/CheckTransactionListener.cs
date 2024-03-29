﻿// Copyright 2022 王建军
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using NHibernate;
using NHibernate.Event;

namespace NHibernateUtils;

/// <summary>
/// 检查当前是否有活动的事务，如果没有，则抛出 <see cref="NoTransactionException"/>。
/// </summary>
public class CheckTransactionListener : IPreInsertEventListener, IPreDeleteEventListener, IPreUpdateEventListener, IPreLoadEventListener
{
    public bool OnPreDelete(PreDeleteEvent @event)
    {
        CheckTransaction(@event.Session);
        return false;
    }

    public Task<bool> OnPreDeleteAsync(PreDeleteEvent @event, CancellationToken cancellationToken)
    {
        OnPreDelete(@event);
        return Task.FromResult(false);
    }

    public bool OnPreInsert(PreInsertEvent @event)
    {
        CheckTransaction(@event.Session);
        return false;
    }

    public Task<bool> OnPreInsertAsync(PreInsertEvent @event, CancellationToken cancellationToken)
    {
        OnPreInsert(@event);
        return Task.FromResult(false);
    }

    public void OnPreLoad(PreLoadEvent @event)
    {
        CheckTransaction(@event.Session);
    }

    public Task OnPreLoadAsync(PreLoadEvent @event, CancellationToken cancellationToken)
    {
        OnPreLoad(@event);
        return Task.CompletedTask;
    }

    public bool OnPreUpdate(PreUpdateEvent @event)
    {
        CheckTransaction(@event.Session);
        return false;
    }

    public Task<bool> OnPreUpdateAsync(PreUpdateEvent @event, CancellationToken cancellationToken)
    {
        OnPreUpdate(@event);
        return Task.FromResult(false);
    }

    private void CheckTransaction(ISession session)
    {
        var tx = session.GetCurrentTransaction();
        if (tx is null || tx.IsActive == false)
        {
            throw new NoTransactionException();
        }
    }
}
