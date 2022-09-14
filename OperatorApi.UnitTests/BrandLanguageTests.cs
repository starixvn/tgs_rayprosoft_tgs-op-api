using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NUnit.Framework;
using TGS.Core.DataServices.Tgs;
using CoreModels = TGS.Core.DataServices.Tgs.Models;
using TGS.Services.Operator.ApplicationServices.BrandLanguage;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class BrandLanguageTests
{
    [Test]
    public async Task Get_Brand_Languages_Should_Success()
    {
        var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsRepository>();
        dbContextOptionsBuilder
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        ITgsRepository repository = new TgsRepository(dbContextOptionsBuilder.Options);
        var brandId = Guid.NewGuid();

        repository.Brands.Add(new CoreModels.Brand()
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

        repository.BrandLanguages.Add(new CoreModels.BrandLanguage()
        {
            BrandId = brandId,
            CultureCode = "zh-TW",
            BrandCultureCode = "zh-TW"
        });

        await repository.SaveChangesAsync();

        var sut = new GetBrandLanguageQueryService(repository);

        var query = new BrandLanguageQuery(brandId);
        var actual = await sut.QueryAsync(query).ToArrayAsync();

        Assert.IsTrue(actual.Length >= 1);
    }
}