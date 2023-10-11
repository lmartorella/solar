using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Service manager
    /// </summary>
    public static class Manager
    {
        private static readonly Dictionary<Type, Type> Types = new Dictionary<Type, Type>();
        private static readonly Dictionary<Type, object> Instances = new Dictionary<Type, object>();
        private static readonly object LockObject = new object();
        private static readonly TaskCompletionSource _killDefer = new TaskCompletionSource();

        public static T GetService<T>() where T : IService
        {
            lock (LockObject)
            {
                object retValue;
                if (!Instances.TryGetValue(typeof(T), out retValue))
                {
                    Type instanceType = GetType<T>();
                    retValue = Activator.CreateInstance(instanceType);

                    foreach (var tuple in Types.Where(tuple => tuple.Value == instanceType))
                    {
                        Instances[tuple.Key] = retValue;
                    }
                }
                return (T)retValue;
            }
        }

        private static Type GetType<T>() where T : IService
        {
            Type implType;
            if (!Types.TryGetValue(typeof (T), out implType))
            {
                if (typeof (T).IsAbstract || typeof(T).IsInterface)
                {
                    throw new ArgumentException("Implementation of " + typeof(T) + " not found");
                }
                implType = typeof (T);
                Types.Add(implType, implType);
            }
            return implType;
        }

        public static void Register<TC, TI>() where TI : IService 
                                              where TC : class, TI, new()
        {
            lock (LockObject)
            {
                Types[typeof (TI)] = typeof (TC);
                Types[typeof (TC)] = typeof (TC);
            }
        }

        public static void Register<TC>() where TC : class, new()
        {
            lock (LockObject)
            {
                Types[typeof (TC)] = typeof (TC);
            }
        }

        internal static void Register(Type tc, Type ti)
        {
            if (!ti.IsAssignableFrom(tc))
            {
                throw new ArgumentException("Invalid types");
            }
            lock (LockObject)
            {
                Types[tc] = tc;
                Types[ti] = tc;
            }
        }

        internal static Task Run()
        {
            return _killDefer.Task;
        }

        public static async Task Kill(ILogger logger, string reason, TimeSpan delay)
        {
            logger.Log("Server killing: " + reason + ". Stopping devices...");
            await Task.Delay(delay);
            _killDefer.TrySetResult();
        }
    }
}
