namespace EasyInjector.Sample;

public static class SampleDemo
{
    public static void RunDemos()
    {
        Demo1();
        Demo2();
    }

    private static void Demo1()
    {
        var ioc = new global::EasyInjector.EasyInjector();

        var child2 = new FooChild2
        {
            V = "Hello"
        };

        ioc.RegisterSingleton<IFooChild1, FooChild1>();
        ioc.RegisterSingleton<IFooChild2, FooChild2>(child2);

        ioc.ResolveAndVerify();

        ioc.Get<IFooChild1>().V = 42;

        var parent1 = ioc.Get<FooParent1>();
        _ = parent1.V1; //42
        _ = parent1.V2; //"Hello"


        var parent2 = ioc.Get<FooParent2>();
        _ = parent2.V1; //0
        _ = parent2.V2; //Empty
    }

    private static void Demo2()
    {
        var ioc = new global::EasyInjector.EasyInjector();

        var child2 = new FooChild2()
        {
            V = "Hello"
        };

        ioc.RegisterSingleton<IFooChild1, FooChild1>();
        ioc.RegisterSingleton<IFooChild2, FooChild2>(child2);
        ioc.RegisterTransient<IFooParent, FooParent1>();

        ioc.ResolveAndVerify();

        ioc.Get<IFooChild1>().V = 42;

        var parent = ioc.Get<IFooParent>();
        _ = parent.V1; //42
        _ = parent.V2; //"Hello"
    }
}