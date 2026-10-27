using CMS.EmailEngine;
using ETG;
using ETG.Core.Content;
using ETG.Core.Content.Pages;
using ETG.Web.Components.PageBuilder.Sections.Grid;
using ETG.Web.Features.TourPackage.Pdf;
using ETG.Web.Features.Search;
using ETG.Web.Features.Search.Services;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Kentico.Xperience.Algolia;
using Microsoft.Extensions.Options;
using System.Net.Mime;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection;

using ContainerContent = ETG.Core.Content.Container;
using DefaultContent = ETG.Core.Content.Default;

public static class ServiceCollectionXperienceExtensions
{
    public static IServiceCollection AddAppXperience(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env) =>
        services
            .AddKentico(features =>
            {
                features.UsePageBuilder(new PageBuilderOptions
                {
                    DefaultSectionIdentifier = GridSection.IDENTIFIER,
                    RegisterDefaultSection = true,
                    ContentTypeNames =
                    [
                        Homepage.CONTENT_TYPE_NAME,
                        Destination.CONTENT_TYPE_NAME,
                        Container.CONTENT_TYPE_NAME,
                        Incentive.CONTENT_TYPE_NAME,
                        AgentPortalMainPage.CONTENT_TYPE_NAME,
                        IncentiveFolder.CONTENT_TYPE_NAME,
                        Article.CONTENT_TYPE_NAME,
                        Experience.CONTENT_TYPE_NAME,
                        TravelTypeLanding.CONTENT_TYPE_NAME,
                        TravelTypeDetail.CONTENT_TYPE_NAME,
                        Page.CONTENT_TYPE_NAME,
                        CareerRole.CONTENT_TYPE_NAME,
                        Tour.CONTENT_TYPE_NAME,
                        DefaultContent.CONTENT_TYPE_NAME
                    ]
                });

                features.UseWebPageRouting();
            })
            .AddKenticoAlgolia(b =>
            {
                b.RegisterStrategy<ArticleIndexingStrategy>("ArticleIndexingStrategy");
                b.RegisterStrategy<TourIndexingStrategy>("TourIndexingStrategy");
            }, config)
            .AddScoped<IAlgoliaSyncService, AlgoliaSyncService>()

            .AddKenticoTagManager(config)
            .AddXperienceChannelSmtp("ETGEmails", options =>
            {
                var smtpConfig = config.GetSection("SmtpOptions");

                options.Server = new SmtpServer
                {
                    Host = smtpConfig["Server:Host"],
                    Port = smtpConfig.GetValue<int>("Server:Port"),
                    UserName = smtpConfig["Server:UserName"],
                    Password = smtpConfig["Server:Password"]
                };

                options.Encoding = Encoding.UTF8;
                options.TransferEncoding = TransferEncoding.QuotedPrintable;
            })
            .AddXperienceSystemSmtp(options =>
            {
                var smtpConfig = config.GetSection("SmtpOptions");
            
                options.Server = new SmtpServer
                {
                    Host = smtpConfig["Server:Host"],
                    Port = smtpConfig.GetValue<int>("Server:Port"),
                    UserName = smtpConfig["Server:UserName"],
                    Password = smtpConfig["Server:Password"]
                };
            
                options.Encoding = Encoding.UTF8;
                options.TransferEncoding = TransferEncoding.QuotedPrintable;
            })
            .Configure<SystemEmailOptions>(config.GetSection("SystemEmailOptions"))
            .AddHttpContextAccessor()
            .Configure<PdfSettings>(
                config.GetSection(PdfSettings.SectionName))
            .AddScoped<IPDFGenerator, PDFGenerator>()
            .AddScoped<ITourPdfProvider, TourPdfProvider>();


}
