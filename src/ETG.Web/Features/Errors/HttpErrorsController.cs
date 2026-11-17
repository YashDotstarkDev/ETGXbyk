using System.Threading;
using System.Threading.Tasks;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Infrastructure;
using ETG.Web.Services.Currency;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ETG.Web.Features.Errors;

[Route("error")]
public class HttpErrorsController(
    WebPageMetaService metaService,
    IMediator mediator,
    IWebsiteChannelContext channelContext,
    CurrentCurrencyPricing currencyPricing) : Controller
{
    private readonly WebPageMetaService metaService = metaService;
    private readonly IMediator mediator = mediator;
    private readonly IWebsiteChannelContext channelContext = channelContext;
    private readonly CurrentCurrencyPricing currencyPricing = currencyPricing;

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

        var channelName = !string.IsNullOrWhiteSpace(channelContext.WebsiteChannelName)
            ? channelContext.WebsiteChannelName
            : "ETG";
        var currency = !string.IsNullOrWhiteSpace(currencyPricing.CurrentCurrency)
            ? currencyPricing.CurrentCurrency
            : "AUD";

        var model = await mediator.Send(new PageNotFoundQuery(
            ChannelName: channelName,
            Currency: currency
        ), cancellationToken);

        return View("~/Features/Errors/NotFound.cshtml", model);
    }
}

public class ErrorPageViewModel
{
    public int StatusCode { get; set; }
}
