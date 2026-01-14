using Microsoft.AspNetCore.Mvc;

public class StatWidgetViewModel
{
    public required string Title { get; set; }
    public int Total { get; set; }
    public required string ChangeHtml { get; set; }
}
