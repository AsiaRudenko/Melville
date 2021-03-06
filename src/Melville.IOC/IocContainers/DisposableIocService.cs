﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Melville.IOC.IocContainers
{
    public interface IDisposableIocService : IIocService, IAsyncDisposable, IDisposable
    { 
    }

    public interface IRegisterDispose
    {
        void RegisterForDispose(object obj);
    }
    
    public class DisposableIocService: GenericScope, IDisposableIocService, IRegisterDispose
    {
        public DisposableIocService(IIocService parentScope) : base(parentScope)
        {
        }

        private readonly List<object> itemsToDispose = new List<object>();

        public void RegisterForDispose(object ret)
        {
            itemsToDispose.Add(ret);
        }

        public async ValueTask DisposeAsync()
        {
            //We dispose in reverse order.  Since most objects are created after their dependencies that means
            // that most objects will be disposed before their dependencies are disposed.
            //
            //We call distinct so that if the user accidentally got the class registered twice, ike because it
            // called registerWrapperForDisposal without a wrapper. it does not get disposed twice
            foreach (var item in Enumerable.Reverse(itemsToDispose).Distinct())
            {
                await DisposeSingleItem(item);
            }
            itemsToDispose.Clear();
        }

        private static async Task DisposeSingleItem(object item)
        {
            switch (item)
            {
                case IAsyncDisposable ad:
                    await ad.DisposeAsync();
                    break;
                case IDisposable d:
                    d.Dispose();
                    break;
            }
        }

        public void Dispose()
        {
            // if we are disposed of without an async context, at least everything will get disposed of.
            // but the async disposal may continue after this function returns.  This is unfortunate but
            // it is the best we can do in the context
            GC.KeepAlive(DisposeAsync());
        }
    }
}