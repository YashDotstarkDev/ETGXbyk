using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ETG.Web.Components.ViewComponents.SubscribeNow;
using ETG.Web.Tests;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.ViewComponents
{
    [TestFixture]
    public class SubscribeNowTests : BaseWebTest
    {
        private const string ViewPath =
            "~/Components/ViewComponents/SubscribeNow/SubscribeNow.cshtml";

        [Test]
        public void Invoke_ReturnsExpectedViewAndModel()
        {
            var component = new SubscribeNowViewComponent();

            var result = component.Invoke();

            Assert.That(
                result,
                Is.TypeOf<ViewViewComponentResult>());

            var view = (ViewViewComponentResult)result;

            Assert.Multiple(() =>
            {
                Assert.That(view.ViewName, Is.EqualTo(ViewPath));
                Assert.That(
                    view.ViewData.Model,
                    Is.TypeOf<SubscribeNowViewModel>());
            });

            var model = (SubscribeNowViewModel)view.ViewData.Model!;

            Assert.Multiple(() =>
            {
                Assert.That(model.EmailAddress, Is.Empty);
                Assert.That(model.IsTravelAgent, Is.Empty);
            });
        }

        [Test]
        public void Get_ReturnsNewIndependentModels()
        {
            var first = GetSubscribeNowQuery.Get();
            var second = GetSubscribeNowQuery.Get();

            first.EmailAddress = "first@example.com";
            first.IsTravelAgent = "Yes";

            Assert.Multiple(() =>
            {
                Assert.That(second, Is.Not.SameAs(first));
                Assert.That(second.EmailAddress, Is.Empty);
                Assert.That(second.IsTravelAgent, Is.Empty);
            });
        }

        [TestCaseSource(typeof(SubscriptionCases))]
        public void Model_ValidatesExpectedInputs(
            string? email,
            string? agent,
            bool expectedValid,
            string? invalidMember)
        {
            var model = CreateModel(email, agent);
            var results = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(
                model,
                new ValidationContext(model),
                results,
                validateAllProperties: true);

            Assert.That(valid, Is.EqualTo(expectedValid));

            if (expectedValid)
            {
                Assert.That(results, Is.Empty);
            }
            else
            {
                Assert.That(
                    results.SelectMany(x => x.MemberNames),
                    Does.Contain(invalidMember));
            }
        }

        [TestCaseSource(typeof(InvalidSubscriptionCases))]
        public void Save_RejectsInvalidServerSideSubmission(
            string? email,
            string? agent)
        {
            // Calls the server-side method directly:
            // no browser or client-side validation is involved.
            var model = CreateModel(email, agent);

            var result = GetSubscribeNowQuery.Save(model);

            Assert.That(result, Is.False);
        }

        [Test]
        public void Save_NullModel_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => GetSubscribeNowQuery.Save(null!));
        }

        [TestCaseSource(typeof(SubscriptionRequiredFieldCases))]
        public void Model_RequiredFields_ReturnExpectedMessage(
            string member,
            string? value,
            string expectedMessage)
        {
            var model = CreateModel("person@example.com", "Yes");

            if (member == nameof(SubscribeNowViewModel.EmailAddress))
            {
                model.EmailAddress = value!;
            }
            else
            {
                model.IsTravelAgent = value!;
            }

            var results = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(
                model,
                new ValidationContext(model),
                results,
                validateAllProperties: true);

            Assert.Multiple(() =>
            {
                Assert.That(valid, Is.False);

                Assert.That(
                    results
                        .Where(x => x.MemberNames.Contains(member))
                        .Select(x => x.ErrorMessage),
                    Does.Contain(expectedMessage));
            });
        }

        [TestCaseSource(typeof(SubscriptionEmailLengthCases))]
        public void Model_EmailLength_Respects254CharacterLimit(
            int length,
            bool expectedValid)
        {
            var email = SubscriptionTestInputs.EmailOfLength(length);
            var model = CreateModel(email, "No");
            var results = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(
                model,
                new ValidationContext(model),
                results,
                validateAllProperties: true);

            Assert.Multiple(() =>
            {
                Assert.That(email.Length, Is.EqualTo(length));
                Assert.That(valid, Is.EqualTo(expectedValid));
            });

            if (!expectedValid)
            {
                Assert.That(
                    results.Select(x => x.ErrorMessage),
                    Does.Contain("Email address is too long."));
            }
        }

        private static SubscribeNowViewModel CreateModel(
            string? email,
            string? agent)
        {
            return new SubscribeNowViewModel
            {
                EmailAddress = email!,
                IsTravelAgent = agent!
            };
        }
    }

    public abstract class SubscriptionCaseSource
        : IEnumerable<TestCaseData>
    {
        public abstract IEnumerator<TestCaseData> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public sealed class SubscriptionCases : SubscriptionCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var email in SubscriptionTestInputs.ValidEmails())
            {
                foreach (var agent in new[] { "Yes", "No" })
                {
                    yield return new TestCaseData(
                        email, agent, true, null);
                }
            }

            foreach (var email in SubscriptionTestInputs.InvalidEmails())
            {
                yield return new TestCaseData(
                    email,
                    "Yes",
                    false,
                    nameof(SubscribeNowViewModel.EmailAddress));
            }

            foreach (var agent in SubscriptionTestInputs.InvalidAgents())
            {
                yield return new TestCaseData(
                    "person@example.com",
                    agent,
                    false,
                    nameof(SubscribeNowViewModel.IsTravelAgent));
            }
        }
    }

    public sealed class InvalidSubscriptionCases : SubscriptionCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var email in SubscriptionTestInputs.InvalidEmails())
            {
                yield return new TestCaseData(
                    new object?[] { email, "Yes" });
            }

            foreach (var agent in SubscriptionTestInputs.InvalidAgents())
            {
                yield return new TestCaseData(
                    new object?[] { "person@example.com", agent });
            }

            // Direct submission with both fields invalid.
            yield return new TestCaseData("not-an-email", "Maybe");

            // Ensure Save also enforces the email length limit.
            yield return new TestCaseData(
                SubscriptionTestInputs.EmailOfLength(255),
                "No");
        }
    }

    public sealed class SubscriptionRequiredFieldCases
        : SubscriptionCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var value in new string?[]
            {
                null, "", " ", "\t", "\r\n"
            })
            {
                yield return new TestCaseData(
                    nameof(SubscribeNowViewModel.EmailAddress),
                    value,
                    "Please enter your email address.");

                yield return new TestCaseData(
                    nameof(SubscribeNowViewModel.IsTravelAgent),
                    value,
                    "Please select an option.");
            }
        }
    }

    public sealed class SubscriptionEmailLengthCases
        : SubscriptionCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var length in new[] { 253, 254, 255, 256 })
            {
                yield return new TestCaseData(
                    length,
                    length <= 254);
            }
        }
    }

    internal static class SubscriptionTestInputs
    {
        public static IEnumerable<string> ValidEmails()
        {
            foreach (var localPart in new[]
            {
                "person",
                "first.last",
                "name+tag",
                "first_last",
                "first-last"
            })
            {
                foreach (var domain in new[]
                {
                    "example.com",
                    "sub.example.co.uk"
                })
                {
                    yield return $"{localPart}@{domain}";
                }
            }
        }

        public static IEnumerable<string?> InvalidEmails()
        {
            yield return null;

            foreach (var email in new[]
            {
                "",
                " ",
                "\t\r\n",
                "plain-text",
                "@example.com",
                "person@",
                "person@@example.com",
                "person@example",
                "person@-example.com",
                "person@example-.com",
                "person@example..com",
                "per son@example.com",
                "person@example.com/path",
                "<script>alert(1)</script>",
                "person@example.com\r\nBcc:other@example.com"
            })
            {
                yield return email;
            }

            // Validation occurs before Trim(), so padded emails fail.
            foreach (var padding in new[] { " ", "\t", "\r\n" })
            {
                yield return padding + "person@example.com";
                yield return "person@example.com" + padding;
                yield return padding + "person@example.com" + padding;
            }
        }

        public static IEnumerable<string?> InvalidAgents()
        {
            yield return null;

            foreach (var agent in new[]
            {
                "",
                " ",
                "\t",
                "yes",
                "no",
                "YES",
                "NO",
                " Yes",
                "No ",
                "true",
                "false",
                "1",
                "0",
                "Maybe",
                "Yes\n",
                "<script>alert(1)</script>"
            })
            {
                yield return agent;
            }
        }

        public static string EmailOfLength(int totalLength)
        {
            // Used only for the generated 253–256 character cases.
            // Keeps the local part and each domain label within
            // their usual length boundaries.
            const int localLength = 64;
            const int firstLabelLength = 63;
            const int secondLabelLength = 63;

            // Includes @ and two domain separators.
            const int fixedLength =
                localLength + 1
                + firstLabelLength + 1
                + secondLabelLength + 1;

            var finalLabelLength = totalLength - fixedLength;

            if (finalLabelLength < 2 || finalLabelLength > 63)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalLength));
            }

            return new string('a', localLength) + "@"
                + new string('b', firstLabelLength) + "."
                + new string('c', secondLabelLength) + "."
                + new string('d', finalLabelLength);
        }
    }
}