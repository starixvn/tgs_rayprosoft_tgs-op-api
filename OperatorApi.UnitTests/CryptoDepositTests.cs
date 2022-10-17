using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Tgs.Payment.Clients;
using Tgs.Payment.UmPay;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class CryptoDepositTests
{
    [Test]
    public async Task Query_UmPay_Rate_Exchange_Should_Success()
    {
        var fakePaymentClient = Substitute.For<IPaymentClient>();
        var sut = new GetUmPayExchangeRateQueryService(fakePaymentClient);
        var serializer = new UmPaySerializer();
        var subject = "tgs.payment.umpay.queryexchangerate";
        var setting = serializer.Serialize(new UmPayQueryExchangeRateSettings());
        var query = new GetUmPayExchangeRateQuery(subject, setting);
		_ = await sut.GetAsync(query);

        _ = await fakePaymentClient
            .Received(1)
            .AskAsync(subject, setting);
    }
}