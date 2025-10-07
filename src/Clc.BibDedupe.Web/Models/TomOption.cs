namespace Clc.BibDedupe.Web.Models;

public class TomOption
{
    public TomOption(int id, string description)
    {
        Id = id;
        Description = description;
    }

    public int Id { get; }

    public string Description { get; }
}
