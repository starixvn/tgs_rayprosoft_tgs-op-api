using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.BrandCommunication;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BrandCommunicationController : ControllerBase
{
    [HttpGet]
    [Description("Get a brand's communications")]
    [AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
    public IAsyncEnumerable<BrandCommunicationModel> GetBrandCommunicationsAsync(
        [FromServices] IQueryService<BrandCommunicationQuery, BrandCommunicationInfo> queryService)
    {
        var brandId = HttpContext.Items.GetBrandId();

        var query = new BrandCommunicationQuery(brandId);
        var info = queryService.QueryAsync(query);

        return info.Select(x =>
            new BrandCommunicationModel()
            {
                Type = x.CommunicationType,
                Phone = x.Phone,
                AppId = x.AppId,
                QRCodeUrl = x.QRCodeUrl
            })
            .Take(5);
    }
}
