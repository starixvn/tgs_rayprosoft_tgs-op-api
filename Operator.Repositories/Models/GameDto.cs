using System;
using TGS.Core.Models.Enums;

namespace TGS.Operator.Repositories.Models;
public class GameDto
{
	public int Id { get; set; }
	public string Code { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public string GameProviderCode { get; set; }
	public string GameProviderName { get; set; }
	public Guid GameProviderId { get; set; }
	public string SubGameProviderCode { get; set; }
	public GameStatus BranGameStatus { get; set; }
	public GameStatus Status { get; set; }
	public Status GameProviderStatus { get; set; }
	public string Localization { get; set; }
	public int DisplayOrder { get; set; }
	public GameType Type { get; set; }
	public GameTag Tag { get; set; }
}