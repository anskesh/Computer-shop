using System.Collections.Generic;

namespace ComputerShop;

public class TableData
{
    public List<string> Fields;
    public List<string> Values = new ();
    public List<string> Ids = new ();

    public string GetFields()
    {
        List<string> fields = new ();

        foreach (var field in Fields)
            fields.Add($"[{field}]");

        return string.Join(", ", fields.GetRange(1, fields.Count - 1));
    }
}