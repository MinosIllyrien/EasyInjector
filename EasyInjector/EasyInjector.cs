namespace EasyInjector;

public class EasyInjector : IServiceProvider
{
    private readonly Dictionary<Type, Type> SingletonRegistrations = new();
    private readonly Dictionary<Type, Type> TransientRegistrations = new();

    private readonly Dictionary<Type, object> SingletonObjects = new();

    private readonly object NotExistingObject = new();

    /// <summary>
    /// Returns all registered types.
    /// </summary>
    public Type[] GetRegisteredTypes()
    {
        var types = new List<Type>();
        types.AddRange(SingletonRegistrations.Keys);
        types.AddRange(TransientRegistrations.Keys);
        return types.ToArray();
    }

    /// <summary>
    /// Registers a singleton.
    /// </summary>
    public bool RegisterSingleton<T>()
    {
        return RegisterSingleton<T, T>();
    }

    /// <summary>
    /// Registers an instance as a singleton.
    /// </summary>
    public bool RegisterSingleton<T>(T instance)
    {
        return RegisterSingleton<T, T>(instance);
    }

    /// <summary>
    /// Registers a singleton.
    /// </summary>
    public bool RegisterSingleton<TService, T>() where T : TService
    {
        var key = typeof(TService);

        if (SingletonRegistrations.ContainsKey(key)) return false;
        if (TransientRegistrations.ContainsKey(key)) return false;

        SingletonRegistrations.Add(key, typeof(T));

        return true;
    }

    /// <summary>
    /// Registers an instance as a singleton.
    /// </summary>
    public bool RegisterSingleton<TService, T>(T instance) where T : TService
    {
        var key = typeof(TService);

        if (SingletonRegistrations.ContainsKey(key)) return false;
        if (TransientRegistrations.ContainsKey(key)) return false;
        if (instance is null) return false;

        SingletonRegistrations.Add(key, instance.GetType());
        SingletonObjects.Add(key, instance);

        return true;
    }

    /// <summary>
    /// Registers a transient.
    /// </summary>
    public bool RegisterTransient<T>()
    {
        var key = typeof(T);

        if (SingletonRegistrations.ContainsKey(key)) return false;
        if (TransientRegistrations.ContainsKey(key)) return false;

        TransientRegistrations.Add(key, key);

        return true;
    }

    /// <summary>
    /// Registers a transient.
    /// </summary>
    public bool RegisterTransient<TService, T>() where T : TService
    {
        var key = typeof(TService);

        if (SingletonRegistrations.ContainsKey(key)) return false;
        if (TransientRegistrations.ContainsKey(key)) return false;

        TransientRegistrations.Add(key, typeof(T));

        return true;
    }

    /// <summary>
    /// Resolves all singletons.
    /// </summary>
    public bool Resolve(bool throwErrorOnUnresolved)
    {
        var unresolvedKeys = GetUnresolvedSingletons().ToHashSet();

        var remainingRegistrations = new Dictionary<Type, Type>(from registration in SingletonRegistrations where unresolvedKeys.Contains(registration.Key) select registration);

        while (remainingRegistrations.Any())
        {
            var addedKeys = new HashSet<Type>();

            foreach (var key in remainingRegistrations.Keys)
            {
                var testObj = TryGet(key);

                if (testObj is null || testObj == NotExistingObject)
                {
                    continue;
                }

                SingletonObjects.Add(key, testObj);
                addedKeys.Add(key);
            }

            if (remainingRegistrations.Any() && !addedKeys.Any())
            {
                if (throwErrorOnUnresolved)
                {
                    var remainingRegistrationExample = remainingRegistrations.ToArray()[0];
                    var remainingRegistrationExampleKey = remainingRegistrationExample.Key;
                    var remainingRegistrationExampleValue = remainingRegistrationExample.Value;

                    throw new Exception(
                        $"Some registrations, including {remainingRegistrationExampleKey} -> {remainingRegistrationExampleValue}, could not be resolved.");
                }

                return false;
            }

            foreach (var addedKey in addedKeys)
            {
                remainingRegistrations.Remove(addedKey);
            }
        }

        return true;
    }

    /// <summary>
    /// Returns the registration types of the singletons which have not been resolved.
    /// </summary>
    public Type[] GetUnresolvedSingletons()
    {
        var keySetRegistrations = new HashSet<Type>(SingletonRegistrations.Keys);
        var keySetResolved = new HashSet<Type>(SingletonObjects.Keys);

        var unresolvedKeys = keySetRegistrations.Except(keySetResolved);

        return unresolvedKeys.ToArray();
    }

    /// <summary>
    /// Verifies that all registered types can be created and that all singletons have been resolved.
    /// </summary>
    public bool Verify(bool throwErrorOnUnresolved = true)
    {
        var allKeys = new HashSet<Type>(SingletonObjects.Keys);
        allKeys.UnionWith(TransientRegistrations.Keys);

        foreach (var key in allKeys)
        {
            var obj = TryGet(key);

            if (obj is not null && obj != NotExistingObject) continue;

            if (throwErrorOnUnresolved)
            {
                throw new Exception($"{key} could not be resolved.");
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// Resolves singletons and verifies that all registered types can be created.
    /// </summary>
    public bool ResolveAndVerify(bool throwErrorOnUnresolved = true) => Resolve(throwErrorOnUnresolved) && Verify(throwErrorOnUnresolved);

    /// <summary>
    /// Returns registered objects.
    /// </summary>
    public T Get<T>()
    {
        var key = typeof(T);
        return (T)Get(key);
    }

    /// <summary>
    /// Returns registered objects.
    /// </summary>
    public T? Get<T>(bool throwErrorOnUnresolved)
    {
        var key = typeof(T);
        return (T?)Get(key, throwErrorOnUnresolved);
    }

    /// <summary>
    /// Returns registered objects - throws error on Null. 
    /// </summary>
    public object Get(Type key)
    {
        if (SingletonObjects.ContainsKey(key))
        {
            var obj = SingletonObjects[key];
            return obj;
        }

        if (TransientRegistrations.ContainsKey(key))
        {
            var obj = Create(key);
            return obj;
        }

        if (SingletonRegistrations.ContainsKey(key))
        {
            throw new Exception($"{key} was registered as a Singleton, but Resolve() may not have been called.");
        }
        else
        {
            var tryCreated = TryGet(key);

            if (tryCreated is not null && tryCreated != NotExistingObject)
            {
                return tryCreated;
            }

            throw new Exception($"{key} was not found to be registered as either a Singleton or as a Transient and it could not be created.");
        }
    }

    /// <summary>
    /// Returns registered nullable objects. 
    /// </summary>
    public object? Get(Type key, bool throwErrorOnUnresolved)
    {
        if (SingletonObjects.ContainsKey(key))
        {
            var obj = SingletonObjects[key];
            return obj;
        }

        if (TransientRegistrations.ContainsKey(key))
        {
            var obj = Create(key);
            return obj;
        }

        if (SingletonRegistrations.ContainsKey(key))
        {
            if (throwErrorOnUnresolved)
            {
                throw new Exception($"{key} was registered as a Singleton, but Resolve() may not have been called.");
            }

            return null;
        }
        else
        {
            var tryCreated = TryGet(key);

            if (tryCreated is not null && tryCreated != NotExistingObject)
            {
                return tryCreated;
            }

            if (throwErrorOnUnresolved)
            {
                throw new Exception($"{key} was not found to be registered as either a Singleton or as a Transient and it could not be created.");
            }

            return null;
        }
    }

    /// <summary>
    /// Creates new objects from types and injects arguments. If the type is registered as a transient, this resolves to Get().
    /// Types need not be registered to be created by this method. Existing registrations will be injected as arguments.
    /// </summary>
    public T Create<T>()
    {
        var key = typeof(T);
        return (T)Create(key);
    }

    /// <summary>
    /// Creates new objects from types and injects arguments. If the type is registered as a transient, this resolves to Get().
    /// Types need not be registered to be created by this method. Existing registrations will be injected as arguments.
    /// </summary>
    public object Create(Type key)
    {
        var obj = TryGet(key, true);

        if (obj is null || obj == NotExistingObject)
        {
            throw new Exception($"{key} could not be created.");
        }

        return obj;
    }

    private object? TryGet(Type key, bool forceCreateNew = false, Type[]? argumentAncestors = null)
    {
        if (argumentAncestors?.Contains(key) ?? false)
        {
            throw new ArgumentException($"There is a cyclic dependency on {key}.");
        }

        Type type;

        if (!forceCreateNew && SingletonObjects.ContainsKey(key))
        {
            return SingletonObjects[key];
        }
        if (TransientRegistrations.ContainsKey(key))
        {
            type = TransientRegistrations[key];
        }
        else if (SingletonRegistrations.ContainsKey(key))
        {
            type = SingletonRegistrations[key];
        }
        else
        {
            type = key;
        }

        var constructors = type.GetConstructors();

        if (constructors.Length == 0)
        {
            throw new Exception($"{type} does not have any constructors, possibly because it is an interface!");
        }

        if (constructors.Length > 1)
        {
            throw new Exception($"{type} has more than one constructor!");
        }

        var constructor = constructors[0];

        var argTypes = (from p in constructor.GetParameters() select p.ParameterType).ToArray();

        //Check if the constructor requires arguments which have not been resolved
        if (argTypes.Any(argType => SingletonRegistrations.ContainsKey(argType) && !SingletonObjects.ContainsKey(argType)))
        {
            return NotExistingObject;
        }

        if (argTypes.Length == 0)
        {
            return Activator.CreateInstance(type);
        }

        var newAncestors = new List<Type>(argumentAncestors ?? Array.Empty<Type>()) { key };

        var args = (from argType in argTypes select TryGet(argType, false, newAncestors.ToArray())).ToArray();

        return args.Contains(NotExistingObject) ? NotExistingObject : Activator.CreateInstance(type, args);
    }

    public object? GetService(Type serviceType) => Get(serviceType, false);
}