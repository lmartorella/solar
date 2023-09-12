using System.Resources;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Base class for singleton services that have a persisted state
    /// </summary>
    public abstract class ServiceBaseWithData<T> : ServiceBase where T : class, new()
    {
        private T _state;
        private bool useCommonServerRoot;

        protected ServiceBaseWithData(bool useCommonServerRoot, bool verboseLog)
            :base(verboseLog)
        {
            this.useCommonServerRoot = useCommonServerRoot;
        }

        /// <summary>
        /// Serializable state
        /// </summary>
        public T State
        {
            get
            {
                if (_state == null)
                {
                    _state = Manager.GetService<IIsolatedStorageService>().GetState<T>(LogName, useCommonServerRoot, () => DefaultState);
                }
                return _state;
            }
            set
            {
                _state = value;
                Save();
            }
        }

        protected virtual T DefaultState
        {
            get
            {
                return new T();
            }
        }

        protected void Save()
        {
            Manager.GetService<IIsolatedStorageService>().SetState(LogName, useCommonServerRoot, State);
        }
    }
}