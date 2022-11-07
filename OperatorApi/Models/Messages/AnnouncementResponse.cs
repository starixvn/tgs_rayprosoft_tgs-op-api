namespace TGS.Apis.OperatorApi.Models.Messages;

public class AnnouncementResponse
{
	public Announcement[] Deposits { get; init; }

	public Announcement[] Mains { get; init; }

	public Announcement[] Marquees { get; init; }
}
