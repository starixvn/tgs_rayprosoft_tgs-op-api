using TGS.Core.Models.Messages;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;

public record GetStationLettersQuery(string PlayerCode, PageableRequest Paging);
