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

[TestClass]
public class PasswordExceptionTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordException_Parameterless_Constructor()
    {
        var target = new PasswordException();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordException_Message_Constructor()
    {
        const string Password = nameof(Password);

        var target = new PasswordException(Password);

        Assert.AreEqual(Password, target.Message);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PasswordException_Message_InnerException_Constructor()
    {
        var innerException = new ArgumentException();

        var target = new PasswordException(string.Empty, innerException);

        Assert.AreEqual(innerException, target.InnerException);
    }
}
