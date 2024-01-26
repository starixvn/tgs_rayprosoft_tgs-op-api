using NSubstitute;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Announcement;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class AnnouncementTests
{
	[Test]
	public async Task Can_Get_Announcements()
	{
		var fakeBrandRepository = Substitute.For<IBrandRepository>();

		var sut = new GetAnnouncementQueryService(fakeBrandRepository);

		var query = new GetAnnouncementQuery(Guid.NewGuid());

		_ = await sut.QueryAsync(query).ToArrayAsync();

		_ = fakeBrandRepository
			.Received(1)
			.QueryAnnouncementInfosAsync(
				Arg.Is(query.BrandId),
				Arg.Any<DateTimeOffset>());
	}
}
