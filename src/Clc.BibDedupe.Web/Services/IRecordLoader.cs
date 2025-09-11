using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public interface IRecordLoader
{
    Task<(RecordData Left, RecordData Right)> LoadAsync(int leftBibId, int rightBibId);
}
