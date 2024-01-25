using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using TGS.Core.DataServices.Tgs;
using TGS.Services.Operator.ApplicationServices.BrandLiveChat;
using CoreModels = TGS.Core.DataServices.Tgs.Models;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class BrandLiveChatTests
{
	[Test]
	public async Task Get_Brand_Live_Chat_Should_Success()
	{
		var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsRepository>();
		_ = dbContextOptionsBuilder
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
		ITgsRepository repository = new TgsRepository(dbContextOptionsBuilder.Options);
		var brandId = Guid.NewGuid();

		_ = repository.BrandLiveChats.Add(new CoreModels.BrandLiveChat()
		{
			BrandId = brandId,
			ChatName = "LiveChat",
			ChatSetting = "{\"url\":\"https://www.google.com\"}"
		});
		_ = await repository.SaveChangesAsync();

		var sut = new GetBrandLiveChatQueryService(repository);

		var query = new BrandLiveChatQuery(BrandId: brandId);

		var info = await sut.GetAsync(query);

		ClassicAssert.IsTrue(info.Setting != null);
	}
}
