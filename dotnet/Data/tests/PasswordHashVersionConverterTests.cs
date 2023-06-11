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

namespace Hirameku.Data.Tests;

using Moq;
using Newtonsoft.Json;

[TestClass]
public class PasswordHashVersionConverterTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHashVersionConverter_Constructor()
    {
        var target = new PasswordHashVersionConverter();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHashVersionConverter_ReadJson()
    {
        var mockReader = new Mock<JsonReader>();
        var name = PasswordHashVersion.HMACSHA512.Name;
        _ = mockReader.Setup(m => m.Value)
            .Returns(name);

        var target = new PasswordHashVersionConverter();

        var version = target.ReadJson(
            mockReader.Object,
            typeof(PasswordHashVersion),
            default,
            new JsonSerializer())
            as PasswordHashVersion;

        Assert.AreEqual(name, version?.Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public void PasswordHashVersionConverter_ReadJson_ReaderIsNull_Throws()
    {
        var target = new PasswordHashVersionConverter();

        _ = target.ReadJson(
            null!,
            typeof(PasswordHashVersion),
            default,
            new JsonSerializer());

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(JsonSerializationException))]
    [DataRow(null, DisplayName = nameof(PasswordHashVersionConverter_ReadJson_Value_Throws) + "(null)")]
    [DataRow("", DisplayName = nameof(PasswordHashVersionConverter_ReadJson_Value_Throws) + "(string.Empty)")]
    [DataRow(" \t\r\n", DisplayName = nameof(PasswordHashVersionConverter_ReadJson_Value_Throws) + "(WhiteSpace)")]
    [DataRow(Math.PI, DisplayName = nameof(PasswordHashVersionConverter_ReadJson_Value_Throws) + "(Math.PI)")]
    public void PasswordHashVersionConverter_ReadJson_Value_Throws(object value)
    {
        var mockReader = new Mock<JsonReader>();
        _ = mockReader.Setup(m => m.Value)
            .Returns(value);
        var target = new PasswordHashVersionConverter();

        _ = target.ReadJson(
            mockReader.Object,
            typeof(PasswordHashVersion),
            default,
            new JsonSerializer());

        Assert.Fail(nameof(JsonSerializationException) + " expected");
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHashVersionConverter_WriteJson()
    {
        var version = PasswordHashVersion.HMACSHA512;
        var mockWriter = new Mock<JsonWriter>();
        _ = mockWriter.Setup(m => m.WriteValue(version.Name));
        var target = new PasswordHashVersionConverter();

        target.WriteJson(mockWriter.Object, version, new JsonSerializer());

        mockWriter.VerifyAll();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordHashVersionConverter_WriteJson_ValueIsNull_Throws()
    {
        var mockWriter = new Mock<JsonWriter>();
        var target = new PasswordHashVersionConverter();

        target.WriteJson(mockWriter.Object, null, new JsonSerializer());

        mockWriter.VerifyNoOtherCalls();
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    [ExpectedException(typeof(ArgumentNullException))]
    public void PasswordHashVersionConverter_WriteJson_WriterIsNull_Throws()
    {
        var target = new PasswordHashVersionConverter();

        target.WriteJson(null!, default, new JsonSerializer());

        Assert.Fail(nameof(ArgumentNullException) + " expected");
    }
}
