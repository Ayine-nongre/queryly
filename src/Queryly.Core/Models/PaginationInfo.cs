public class PaginationInfo
{
    public int PageNumber { get; private set; }
    public int PageSize { get; }
    public int TotalRows { get; set; }
    public int TotalPages { get; set; }
    
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
    
    public PaginationInfo(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
    
    public void NextPage()
    {
        if (HasNextPage)
            PageNumber++;
    }
    
    public void PreviousPage()
    {
        if (HasPreviousPage)
            PageNumber--;
    }
    
    public void GoToPage(int pageNumber)
    {
        if (pageNumber >= 1 && pageNumber <= TotalPages)
            PageNumber = pageNumber;
    }
    
    public int GetOffset()
    {
        return (PageNumber - 1) * PageSize;
    }
}