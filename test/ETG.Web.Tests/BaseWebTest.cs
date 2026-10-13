using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace ETG.Web.Tests;

/// <summary>
/// Base test class providing common setup and utilities for Web project tests
/// </summary>
public abstract class BaseWebTest
{
    /// <summary>
    /// Provides mock implementations and test data
    /// </summary>
    protected static class TestData
    {
        public static string ValidUrl => "https://example.com/page";
        public static string ValidText => "Click Here";
        public static string EmptyString => string.Empty;
        public static string DefaultTitle => "Default Page Title";
    }

    /// <summary>
    /// Common test setup
    /// </summary>
    [SetUp]
    public virtual void Setup()
    {
        // Override in derived classes for specific setup
    }

    /// <summary>
    /// Common test teardown
    /// </summary>
    [TearDown]
    public virtual void TearDown()
    {
        // Override in derived classes for cleanup
    }
}
