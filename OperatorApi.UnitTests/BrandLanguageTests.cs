using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using TGS.Core.DataServices.Tgs;
using TGS.Services.Operator.ApplicationServices.BrandLanguage;
using CoreModels = TGS.Core.DataServices.Tgs.Models;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class BrandLanguageTests
{
	[Test]
	public async Task Get_Brand_Languages_Should_Success()
	{
		var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsRepository>();
		_ = dbContextOptionsBuilder
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
		ITgsRepository repository = new TgsRepository(dbContextOptionsBuilder.Options);
		var brandId = Guid.NewGuid();

		_ = repository.Brands.Add(new CoreModels.Brand()
		{
			Id = brandId,
			ApiKey = string.Empty,
			ClientId = string.Empty,
			Code = "Test",
			CreatedByUsername = "System",
			UpdatedByUsername = "System",
			Name = "Test",
			SecretKey = string.Empty
		});

		_ = repository.BrandLanguages.Add(new CoreModels.BrandLanguage()
		{
			BrandId = brandId,
			CultureCode = "zh-TW",
			BrandCultureCode = "zh-TW"
		});

		_ = await repository.SaveChangesAsync();

		var sut = new GetBrandLanguageQueryService(repository);

		var query = new BrandLanguageQuery(brandId);
		var actual = await sut.QueryAsync(query).ToArrayAsync();

		ClassicAssert.IsTrue(actual.Length >= 1);
	}
}
