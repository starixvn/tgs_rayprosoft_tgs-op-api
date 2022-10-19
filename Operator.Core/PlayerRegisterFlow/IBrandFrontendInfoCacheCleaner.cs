using System;

namespace TGS.Services.Operator.PlayerRegisterFlow;

internal interface IBrandFrontendInfoCacheCleaner
{
	void Clean(Guid brandId);
}
