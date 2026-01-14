using Microsoft.AspNetCore.Mvc;
public class StatWidgetViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(string title, int total, string changeHtml)
    {
        return View(new StatWidgetViewModel
        {
            Title = title,
            Total = total,
            ChangeHtml = changeHtml
        });
    }
}
