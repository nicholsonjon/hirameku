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

namespace Hirameku.Common.Service.Tests;

using Autofac;
using AutoMapper;
using System.Diagnostics.CodeAnalysis;

[TestClass]
public class ContainerBuilderExtensionsTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ContainerBuilderExtensions_RegisterMapper()
    {
        var target = new ContainerBuilder();
        _ = target.RegisterType<ProfileOne>().As<Profile>();
        _ = target.RegisterType<ProfileTwo>().As<Profile>();
        _ = target.RegisterMapper();

        var container = target.Build();
        var mapper = container.Resolve<IMapper>();
        var foo = new Foo()
        {
            Bar = nameof(Foo.Bar),
            Baz = nameof(Foo.Baz),
        };
        var bar = new Bar()
        {
            Baz = nameof(Bar.Baz),
            Foo = nameof(Bar.Foo),
        };

        var mappedBar = mapper.Map<Bar>(foo);
        var mappedFoo = mapper.Map<Foo>(bar);

        Assert.AreEqual(mappedBar.Baz, mappedFoo.Baz);
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Test code", Scope = "type", Target = "~T:Hirameku.Common.Service.Tests.ContainerBuilderExtensionsTests.ProfileOne")]
    private sealed class ProfileOne : Profile
    {
        public ProfileOne()
        {
            _ = this.CreateMap<Foo, Bar>(MemberList.None);
        }
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Test code", Scope = "type", Target = "~T:Hirameku.Common.Service.Tests.ContainerBuilderExtensionsTests.ProfileTwo")]
    private sealed class ProfileTwo : Profile
    {
        public ProfileTwo()
        {
            _ = this.CreateMap<Bar, Foo>(MemberList.None);
        }
    }

    private sealed class Foo
    {
        public string? Bar { get; set; }

        public string? Baz { get; set; }
    }

    private sealed class Bar
    {
        public string? Baz { get; set; }

        public string? Foo { get; set; }
    }
}
