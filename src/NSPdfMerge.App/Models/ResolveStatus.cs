namespace NSPdfMerge.App.Models;

public enum ResolveStatus
{
    Pending = 0,
    Found = 1,
    NotFound = 2,
    Ambiguous = 3,
    InvalidPath = 4
}
