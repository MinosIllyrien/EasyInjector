namespace EasyInjector.Sample;

public class FooParent1 : IFooParent
{
    public IFooChild1 F1 { get; set; }
    public IFooChild2 F2 { get; set; }
    public int V1 => F1.V;
    public string V2 => F2.V;

    public FooParent1(IFooChild1 f1, IFooChild2 f2)
    {
        F1 = f1;
        F2 = f2;
    }
}

public class FooParent2 : IFooParent
{
    public IFooChild1 F1 { get; set; }
    public IFooChild2 F2 { get; set; }
    public int V1 => F1.V;
    public string V2 => F2.V;

    public FooParent2(FooChild1 f1, FooChild2 f2)
    {
        F1 = f1;
        F2 = f2;
    }
}

public class FooChild1 : IFooChild1
{
    public int V { get; set; }
}

public class FooChild2 : IFooChild2
{
    public string V { get; set; } = "";
}