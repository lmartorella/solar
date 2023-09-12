using System;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Access isolated storage
    /// </summary>
    internal interface IIsolatedStorageService : IService
    {
        void InitAppRoot(string appRoot);
        T GetState<T>(string serviceName, bool useCommonServerRoot, Func<T> defaultProvider);
        void SetState<T>(string serviceName, bool useCommonServerRoot, T value);
    }
}