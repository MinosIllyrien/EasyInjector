namespace EasyInjector.Sample;

public interface IFooParent
{
    public IFooChild1 F1 { get; set; }
    public IFooChild2 F2 { get; set; }

    public int V1 { get; }
    public string V2 { get; }
}

public interface IFooChild1
{
    public int V { get; set; }
}

public interface IFooChild2
{
    public string V { get; set; }
}