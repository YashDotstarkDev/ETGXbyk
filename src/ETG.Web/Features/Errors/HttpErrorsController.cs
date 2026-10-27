using System.Threading;
using System.Threading.Tasks;
using ETG.Core.Content;
using ETG.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ETG.Web.Features.Errors;

[Route("error")]
public class HttpErrorsController(WebPageMetaService metaService, IMediator mediator) : Controller
{
    private readonly WebPageMetaService metaService = metaService;
    private readonly IMediator mediator = mediator;

    [HttpGet("{code:int}")]
    public async Task<ActionResult> Error(int code, CancellationToken cancellationToken)
    {
        if (code != 404)
        {
            metaService.SetMeta(new WebPageMeta("Error", "There was a problem handling your request.", ""));

            Response.StatusCode = code >= 400 && code < 600 ? code : StatusCodes.Status500InternalServerError;

            return View("~/Features/Errors/Exception.cshtml", new ErrorPageViewModel { StatusCode = code });
        }

        metaService.SetMeta(new WebPageMeta("Looks like you've lost your way", "Let us help get you back on track. Search a destination, experience or tour, or browse the content below for some inspiration.", ""));

        Response.StatusCode = StatusCodes.Status404NotFound;

        var model = await mediator.Send(new PageNotFoundQuery(), cancellationToken);

        return View("~/Features/Errors/NotFound.cshtml", model);
    }
}

public class ErrorPageViewModel
{
    public int StatusCode { get; set; }
}
