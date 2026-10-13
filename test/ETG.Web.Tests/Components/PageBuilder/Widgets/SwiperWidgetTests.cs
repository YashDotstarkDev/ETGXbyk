using ETG.Core.Components;
using ETG.Web.Components.PageBuilder.Widgets.Swiper;
using ETG.Web.Components.PageBuilder.Widgets.Hero;
using ETG.Core.Content;
using CSharpFunctionalExtensions;

namespace ETG.Web.Tests.Components.PageBuilder.Widgets;

[TestFixture]
public class SwiperWidgetTests : BaseWebTest
{
    [SetUp]
    public override void Setup()
    {
        base.Setup();
    }

    #region Identifier Tests

    [Test]
    public void Widget_HasCorrectName()
    {
        Assert.That(SwiperWidget.NAME, Is.EqualTo("Swiper Slider"));
    }

    #endregion

    [Test]
    public void Widget_HasCorrectIdentifier()
    {
        Assert.That(SwiperWidget.IDENTIFIER, Is.EqualTo("Website.SwiperWidget"));
    }

    [Test]
    public void SwiperWidgetProperties_HaveDefaults()
    {
        var props = new SwiperWidgetProperties();

        Assert.That(props.Label, Is.EqualTo(string.Empty));
        Assert.That(props.DataSource, Is.EqualTo(nameof(SwiperDataSources.Individual)));
        Assert.That(props.Padding, Is.EqualTo(nameof(MarginSizes.Medium)));
        Assert.That(props.Loop, Is.True);
        Assert.That(props.InitialSlide, Is.EqualTo(0));
        Assert.That(props.SlidesPerView, Is.EqualTo(1));
        Assert.That(props.SpaceBetween, Is.EqualTo(0));
        Assert.That(props.CenteredSlides, Is.False);
        Assert.That(props.Autoplay, Is.False);
        Assert.That(props.AutoplayDelay, Is.EqualTo(5000));
        Assert.That(props.Navigation, Is.True);
        Assert.That(props.Pagination, Is.False);
        Assert.That(props.PaginationType, Is.EqualTo(nameof(SwiperPaginationType.Bullets)));
        Assert.That(props.Scrollbar, Is.False);
        Assert.That(props.ScrollbarDraggable, Is.True);
        Assert.That(props.Keyboard, Is.False);
        Assert.That(props.Mousewheel, Is.False);
        Assert.That(props.Effect, Is.EqualTo("slide"));
        Assert.That(props.CustomCssClass, Is.EqualTo(string.Empty));
    }

    [Test]
    public void SwiperWidgetViewModel_HandlesEmptySlidesGracefully()
    {
        var props = new SwiperWidgetProperties();
        props.ID = Guid.NewGuid();

        var vm = new SwiperWidgetViewModel(props, new List<SwiperSlideItem>(), Maybe<SwiperSlidesGroup>.None);

        Assert.That(vm.Slides, Is.Not.Null);
        Assert.That(vm.Slides, Is.Empty);
        Assert.That(vm.HasLabel, Is.False);
    }

    [Test]
    public void SwiperSlideItem_DefaultsAndProperties()
    {
        var item = new SwiperSlideItem
        {
            LegacyMediaFileTitle = "T",
            BasicItemShortDescription = "D",
            DesktopUrl = null,
            MobileUrl = null,
            CTAText = "Go",
            CTAUrl = "/go",
            OpenInNewTab = false,
            CustomCSSClass = "c"
        };

        Assert.That(item.LegacyMediaFileTitle, Is.EqualTo("T"));
        Assert.That(item.DesktopUrl, Is.Null);
        Assert.That(item.CTAUrl, Is.EqualTo("/go"));
    }

    [Test]
    public void SwiperPaginationType_EnumValues()
    {
        Assert.That(SwiperPaginationType.Bullets.ToString(), Is.EqualTo("Bullets"));
        Assert.That(SwiperPaginationType.Fraction.ToString(), Is.EqualTo("Fraction"));
        Assert.That(SwiperPaginationType.Progressbar.ToString(), Is.EqualTo("Progressbar"));
    }

    [Test]
    public void SwiperWidgetViewModel_InitializesFromProperties()
    {
        var props = new SwiperWidgetProperties
        {
            Label = "Test Slider",
            Loop = false,
            InitialSlide = 2,
            SlidesPerView = 3,
            SpaceBetween = 10,
            CenteredSlides = true,
            Autoplay = true,
            AutoplayDelay = 2000,
            Navigation = false,
            Pagination = true,
            PaginationType = nameof(SwiperPaginationType.Fraction),
            Scrollbar = true,
            ScrollbarDraggable = false,
            Keyboard = true,
            Mousewheel = true,
            Effect = "fade",
            CustomCssClass = "custom-swiper",
            Padding = nameof(MarginSizes.Large)
        };

        // set known ID
        props.ID = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var slide = new SwiperSlideItem
        {
            LegacyMediaFileTitle = "Slide 1",
            BasicItemShortDescription = "Desc",
            DesktopUrl = null,
            MobileUrl = null,
            CTAText = "Learn",
            CTAUrl = "/learn",
            OpenInNewTab = true,
            CustomCSSClass = "slide-class"
        };

        var slides = new List<SwiperSlideItem> { slide };

        var vm = new SwiperWidgetViewModel(props, slides, Maybe<SwiperSlidesGroup>.None);

        Assert.That(vm.Label, Is.EqualTo("Test Slider"));
        Assert.That(vm.WidgetInstanceID, Is.EqualTo(props.ID));
        Assert.That(vm.Loop, Is.False);
        Assert.That(vm.InitialSlide, Is.EqualTo(2));
        Assert.That(vm.SlidesPerView, Is.EqualTo(3));
        Assert.That(vm.SpaceBetween, Is.EqualTo(10));
        Assert.That(vm.CenteredSlides, Is.True);
        Assert.That(vm.Autoplay, Is.True);
        Assert.That(vm.AutoplayDelay, Is.EqualTo(2000));
        Assert.That(vm.Navigation, Is.False);
        Assert.That(vm.Pagination, Is.True);
        Assert.That(vm.PaginationType, Is.EqualTo(SwiperPaginationType.Fraction));
        Assert.That(vm.Scrollbar, Is.True);
        Assert.That(vm.ScrollbarDraggable, Is.False);
        Assert.That(vm.Keyboard, Is.True);
        Assert.That(vm.Mousewheel, Is.True);
        Assert.That(vm.Effect, Is.EqualTo("fade"));
        Assert.That(vm.CustomCssClass, Is.EqualTo("custom-swiper"));
        Assert.That(vm.Padding, Is.EqualTo(MarginSizes.Large));
        Assert.That(vm.Slides.Count, Is.EqualTo(1));
        Assert.That(vm.Slides[0].LegacyMediaFileTitle, Is.EqualTo("Slide 1"));
    }
}
