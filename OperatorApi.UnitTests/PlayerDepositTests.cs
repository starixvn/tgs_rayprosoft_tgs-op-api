using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Tgs.Payment.Clients;
using TGS.Core.Models.Messages;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class PlayerDepositTests
{
	[Test]
	public async Task Get_Player_Deposit_Info_Should_Success()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

		var sut = new QueryDepositFormsService(fakePaymentServiceClient);

		var query = new DepositFormsQuery(
			BrandId: Guid.NewGuid(),
			"playerCode",
			new List<PaymentFlowState>(),
			CreateOnRange: null,
			UpdateOnRange: null,
			new PageableRequest());

		_ = await sut.GetAsync(query, default);

		_ = fakePaymentServiceClient
			.Received(1)
			.QueryDepositFormsAsync(
				Arg.Any<DepositFormQueryConditions>(),
				Arg.Any<Tgs.Payment.Clients.Paging>());
	}

	[Test]
	public async Task Get_Player_Deposit_Info_Should_Use_UserId_Condition()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

		var sut = new QueryDepositFormsService(fakePaymentServiceClient);

		var query = new DepositFormsQuery(
			BrandId: Guid.NewGuid(),
			"playerCode",
			new List<PaymentFlowState>(),
			CreateOnRange: null,
			UpdateOnRange: null,
			new PageableRequest());

		_ = await sut.GetAsync(query, default);

		_ = fakePaymentServiceClient
			.Received(1)
			.QueryDepositFormsAsync(
				Arg.Is<DepositFormQueryConditions>(c => c.UserId == query.PlayerCode),
				Arg.Any<Tgs.Payment.Clients.Paging>());
	}

	[Test]
	public void Usdt_Despost_To_Sytem_Dollar()
	{
		var sendUsdt = 1000M;

		var rate = 0.23M;

		var actual = Math.Floor(sendUsdt / rate * 100) / 100;

		var expected = 4347.82M;
		Assert.AreEqual(expected, actual);
	}


	[Test]
	public void Wanda_Pay_Real_Name_Chinese()
	{
		var strRealName = "王小明";

		var flag = true;
		var dRange = 0;
		int dstringmax = Convert.ToInt32("9fff", 16);
		int dstringmin = Convert.ToInt32("4e00", 16);
		for (var i = 0; i < strRealName.Length; i++)
		{
			dRange = Convert.ToInt32(Convert.ToChar(strRealName.Substring(i, 1)));
			if (dRange >= dstringmin && dRange < dstringmax)
			{
				flag = true;
			}
			else
			{
				flag = false;
				break;
			}
		}
		Assert.IsTrue(flag);
	}

	[Test]
	public void Wanda_Pay_Real_Name_English()
	{
		var strRealName = "王小明qoo";

		var flag = true;
		var dRange = 0;
		int dstringmax = Convert.ToInt32("9fff", 16);
		int dstringmin = Convert.ToInt32("4e00", 16);
		for (var i = 0; i < strRealName.Length; i++)
		{
			dRange = Convert.ToInt32(Convert.ToChar(strRealName.Substring(i, 1)));
			if (dRange >= dstringmin && dRange < dstringmax)
			{
				flag = true;
			}
			else
			{
				flag = false;
				break;
			}
		}
		Assert.IsFalse(flag);
	}

	[Test]
	public void Wanda_Pay_Real_Name_Number()
	{
		var strRealName = "王小明123";

		var flag = true;
		var dRange = 0;
		int dstringmax = Convert.ToInt32("9fff", 16);
		int dstringmin = Convert.ToInt32("4e00", 16);
		for (var i = 0; i < strRealName.Length; i++)
		{
			dRange = Convert.ToInt32(Convert.ToChar(strRealName.Substring(i, 1)));
			if (dRange >= dstringmin && dRange < dstringmax)
			{
				flag = true;
			}
			else
			{
				flag = false;
				break;
			}
		}
		Assert.IsFalse(flag);
	}
}
