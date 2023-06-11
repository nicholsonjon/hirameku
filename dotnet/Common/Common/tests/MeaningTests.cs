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

[TestClass]
public class MeaningTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Meaning_Constructor()
    {
        var target = new Meaning();

        Assert.IsNotNull(target);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Meaning_Example()
    {
        const string Example = nameof(Example);

        var target = new Meaning()
        {
            Example = Example,
        };

        Assert.AreEqual(Example, target.Example);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Meaning_Hint()
    {
        const string Hint = nameof(Hint);

        var target = new Meaning()
        {
            Hint = Hint,
        };

        Assert.AreEqual(Hint, target.Hint);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void Meaning_Text()
    {
        const string Text = nameof(Text);

        var target = new Meaning()
        {
            Text = Text,
        };

        Assert.AreEqual(Text, target.Text);
    }
}
