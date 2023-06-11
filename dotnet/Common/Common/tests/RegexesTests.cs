// Hirameku is a cloud-native, vendor-agnostic, serverless application for
// studying flashcards with support for localization and accessibility.
// Copyright (C) 2023 Jon Nicholson
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

namespace Hirameku.Common.Tests;

using Bogus;
using Hirameku.TestTools;
using System.Text.RegularExpressions;

[TestClass]
public class RegexesTests
{
    private const string Base64Characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz+/";

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_Base64_DoesNotMatch()
    {
        Assert.IsFalse(Regex.IsMatch(@"`~!@#$%^&*()_[]{}\|;':\"",.<>?", Regexes.Base64String));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_Base64_Matches_DoublePadding()
    {
        Assert.IsTrue(Regex.IsMatch(Base64Characters + "+/==", Regexes.Base64String));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_Base64_Matches_NoPadding()
    {
        Assert.IsTrue(Regex.IsMatch(Base64Characters, Regexes.Base64String));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_Base64_Matches_SinglePadding()
    {
        Assert.IsTrue(Regex.IsMatch(Base64Characters + "z+/=", Regexes.Base64String));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_EmailAddress_DoesNotMatch()
    {
        // TODO: testing all the possible variations of valid email addresses is going to be a pain,
        // so for now there's only this stub until I have the time to be more exhaustive
        var faker = new Faker();

        Assert.IsFalse(Regex.IsMatch(faker.Random.Words(), Regexes.EmailAddress));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_EmailAddress_Matches()
    {
        // TODO: testing all the possible variations of valid email addresses is going to be a pain,
        // so for now there's only this stub until I have the time to be more exhaustive
        Assert.IsTrue(Regex.IsMatch("john.doe@example.com", Regexes.EmailAddress));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_EntirelyWhiteSpace_EmptyString_DoesNotMatch()
    {
        Assert.IsFalse(Regex.IsMatch(string.Empty, Regexes.EntirelyWhiteSpace));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_EntirelyWhiteSpace_RandomText_DoesNotMatch()
    {
        var faker = new Faker();

        Assert.IsFalse(Regex.IsMatch(faker.Random.Word(), Regexes.EntirelyWhiteSpace));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_EntirelyWhiteSpace_WhiteSpace_Matches()
    {
        Assert.IsTrue(Regex.IsMatch(" \t\r\n", Regexes.EntirelyWhiteSpace));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_HtmlTag_DoesNotMatch()
    {
        var faker = new Faker();

        Assert.IsFalse(Regex.IsMatch(faker.Random.Words(), Regexes.HtmlTag));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_HtmlTag_Matches()
    {
        Assert.IsTrue(Regex.IsMatch("<script src='https://malicious.domain/foo.js'></script>", Regexes.HtmlTag));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_ObjectId_DoesNotMatch()
    {
        var faker = new Faker();

        Assert.IsFalse(Regex.IsMatch(
            faker.Random.Hexadecimal(Constants.InvalidIdLength, string.Empty),
            Regexes.ObjectId));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_ObjectId_Matches()
    {
        var faker = new Faker();

        Assert.IsTrue(Regex.IsMatch(
            faker.Random.Hexadecimal(Constants.ValidIdLength, string.Empty),
            Regexes.ObjectId));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_UserName_DoesNotMatch()
    {
        var faker = new Faker();

        // these characters are from the Unicode Latin-1 Punctuation & Symbols range
        const char BeginningCharacter = '\u00A0';
        const char EndingCharacter = '\u00BF';

        Assert.IsFalse(Regex.IsMatch(
            faker.Random.String(32, BeginningCharacter, EndingCharacter),
            Regexes.UserName));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_UserName_Matches()
    {
        Assert.IsTrue(Regex.IsMatch(TestData.GetRandomUserName(), Regexes.UserName));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_Z85EncodedString_DoesNotMatch()
    {
        Assert.IsFalse(Regex.IsMatch("これはZ85でエンコードされていませんね", Regexes.Z85EncodedString));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Regexes_Z85EncodedString_Matches()
    {
        Assert.IsTrue(Regex.IsMatch(@"09AZaz.-:+=^!/*?&<>()[]{}@%$#", Regexes.Z85EncodedString));
    }
}
