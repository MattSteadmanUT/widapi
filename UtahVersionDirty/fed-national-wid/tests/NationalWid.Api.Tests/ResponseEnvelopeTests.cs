using Microsoft.AspNetCore.Http;
using NationalWid.Api.Services;

namespace NationalWid.Api.Tests;

public sealed class ResponseEnvelopeMessageTests
{
    [Fact]
    public void Create_SetsNoRecordsMessageWhenTotalIsZero()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/wages";
        context.Request.QueryString = new QueryString("?occCode=11*");

        var page = new PagedResult<object>
        {
            Items = [],
            Total = 0,
            Page = 1,
            PageSize = 100,
            NextCursor = null,
        };

        var envelope = ResponseEnvelope.Create(page, context.Request);

        Assert.Equal("No records found with the supplied filters.", envelope.Meta.Message);
    }
}
