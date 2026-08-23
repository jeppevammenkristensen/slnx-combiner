using System.Collections.Generic;
using JetBrains.Annotations;
using TruePath;
using Xunit;

namespace Slnx_Combiner.Tests;

[TestSubject(typeof(NameUtil))]
public class NameUtilTest
{
    [Fact]
    public void GetName_ReturnsFileNameWithoutExtension_AndTracksIt()
    {
        var names = new HashSet<string>();

        var name = NameUtil.GetName(names, TestPaths.Absolute("solutions", "Api.slnx"));

        Assert.Equal("Api", name);
        Assert.Contains("Api", names);
    }

    [Fact]
    public void GetName_WhenBaseNameAndFirstSuffixExist_ReturnsAndTracksNextSuffix()
    {
        var names = new HashSet<string> {"Api", "Api_1"};

        var name = NameUtil.GetName(names, TestPaths.Absolute("solutions", "Api.sln"));

        Assert.Equal("Api_2", name);
        Assert.Contains("Api_2", names);
    }
}