namespace CodexTestProject;

public class IReferenceXedoc
{
    public IXedoc Get()
    {
        return new XedocImpl();
    }
}