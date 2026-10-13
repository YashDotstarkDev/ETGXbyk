namespace ETG.Web.Features.Enquire;

public class EnquireUrlRewriteStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<EnquireUrlRewriteMiddleware>();
            next(app);
        };
    }
}