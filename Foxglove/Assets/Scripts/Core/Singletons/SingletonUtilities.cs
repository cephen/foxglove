using Unity.Entities;
using Unity.Logging;

namespace Foxglove {
    /// <summary>
    /// Tag to mark the entity that will hold Foxglove Singletons
    /// </summary>
    public struct DefaultSingleton : IComponentData { }

    /// <summary>
    /// This class contains extension methods for working with ECS singletons in a more performant way.
    /// By default, every new singleton causes a whole archetype to be allocated
    /// meaning 16kb of memory is allocated for each singleton regardless of the singleton's actual size.
    /// This class provides a way to reduce that memory footprint
    /// by instead attaching all singleton components to a single tracked entity.
    /// </summary>
    public static class SingletonUtilities {
        private static Entity _singletonEntity;

        public static void Setup(EntityManager manager) {
            if (manager.Exists(_singletonEntity)) {
                Log.Error("Singleton Entity already exists");
                return;
            }

            _singletonEntity = manager.CreateEntity();
            manager.AddComponent<DefaultSingleton>(_singletonEntity);
        }

        public static Entity GetDefaultSingletonEntity(this EntityManager manager) => _singletonEntity;

        public static bool HasSingleton<T>(this EntityManager manager)
            where T : struct, IComponentData
            => manager.HasComponent<T>(_singletonEntity);

        public static T GetSingleton<T>(this EntityManager manager)
            where T : unmanaged, IComponentData
            => manager.GetComponentData<T>(_singletonEntity);

        /// <summary>
        /// Create or set a singleton
        /// </summary>
        public static Entity CreateOrSetSingleton<T>(this EntityManager manager, T data)
            where T : unmanaged, IComponentData {
            if (manager.HasComponent<T>(_singletonEntity)) manager.SetComponentData(_singletonEntity, data);
            else manager.AddComponentData(_singletonEntity, data);

            return _singletonEntity;
        }

        /// <summary>
        /// Create a default instance of a singleton if it doesn't already exist
        /// </summary>
        public static Entity CreateOrAddSingleton<T>(this EntityManager manager)
            where T : unmanaged, IComponentData {
            if (!manager.HasComponent<T>(_singletonEntity)) manager.AddComponent<T>(_singletonEntity);

            return _singletonEntity;
        }

        public static void RemoveSingletonComponentIfExists<T>(this EntityManager manager) {
            if (manager.HasComponent<T>(_singletonEntity)) manager.RemoveComponent<T>(_singletonEntity);
        }
    }
}
