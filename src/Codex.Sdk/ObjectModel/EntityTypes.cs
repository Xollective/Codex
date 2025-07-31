namespace Codex.ObjectModel.Implementation;

public partial class EntityTypes
{
    private static Dictionary<Type, Type> _map;
    public static Dictionary<Type, Type> Map
    {
        get
        {
            if (_map == null)
            {
                _map = new(ToImplementationMap.Concat(FromImplementationMap));
            }

            return _map;
        }
    } 
}

public partial class SearchResult
{
    public override string ToString()
    {
        if (Definition != null)
        {
            return "(def) " + Definition.DisplayName;
        }
        else if (TextLine != null)
        {
            return "(txt) " + TextLine.TextSpan.LineSpanText;
        }
        else
        {
            return "";
        }
    }
}