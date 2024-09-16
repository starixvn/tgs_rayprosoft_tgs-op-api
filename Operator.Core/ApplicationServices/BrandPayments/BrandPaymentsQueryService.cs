using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Tgs.Payment.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.BrandPayments;

internal class BrandPaymentsQueryService(
	IPaymentServiceClient paymentServiceClient,
	IPlayerServiceClient playerServiceClient) : IQueryService<BrandPaymentsQuery, BrandPaymentInfo>
{
	private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));

	public async IAsyncEnumerable<BrandPaymentInfo> QueryAsync(
		BrandPaymentsQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var brandPayments = await _paymentServiceClient
			.GetBrandPaymentsAsync(query.BrandId, cancellationToken)
			.Where(bp => bp.Enabled == true)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false)!;

		var acls = await _paymentServiceClient
			.GetBrandPaymentAclsAsync(
				query.PlayerCode,
				brandPayments.Select(bp => bp.Id),
				cancellationToken)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var blockList = acls
			.Where(acl => acl.IsBlock)
			.Select(acl => acl.BrandPaymentId)
			.Distinct()
			.ToArray();

		var whiteList = acls
			.Where(acl => !acl.IsBlock)
			.Select(acl => acl.BrandPaymentId)
			.Distinct()
			.ToArray();

		var playerInfo = await _playerServiceClient
			.GetPlayerInfoAsync(query.PlayerCode, cancellationToken)
			.ConfigureAwait(false)!;

		var depositedAmount = await _playerServiceClient
			.GetPlayerDepositedAmountAsync(query.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		var brandPaymentInfos = await _paymentServiceClient
			.FilterBrandPaymentsAsync(
				brandPayments,
				new Tgs.Payment.Clients.PlayerInfo(
					playerInfo!.BrandId,
					depositedAmount,
					playerInfo.IdentityAuthentication,
					playerInfo.PhoneAuthentication,
					playerInfo.PlayerCode,
					playerInfo.VipLevelId),
				blockList,
				whiteList,
				cancellationToken)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		foreach (var brandPaymentInfo in brandPaymentInfos)
			yield return brandPaymentInfo;
	}
}