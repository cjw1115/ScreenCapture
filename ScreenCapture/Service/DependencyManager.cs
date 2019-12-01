using Autofac;

namespace ScreenCapture.Service
{
    public class DependencyManager
    {
        private ContainerBuilder _containerBuilder = new ContainerBuilder();
        private IContainer _container;

        private DependencyManager()
        {
        }

        private static DependencyManager _instance;
        private static object _lock = new object();
        public static DependencyManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DependencyManager();
                        }
                    }
                }
                return _instance;
            }
        }

        public void BuildContainer()
        {
            _container = _containerBuilder.Build();
        }

        public void RegisterType<T>()
        {
            _containerBuilder.RegisterType<T>();
        }

        public void RegisterInstance<T>() where T : class, new()
        {
            _containerBuilder.RegisterInstance<T>(new T());
        }

        public T ResolveType<T>()
        {
            return _container.Resolve<T>();
        }
    }
}
