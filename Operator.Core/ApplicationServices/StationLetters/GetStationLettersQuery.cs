using Tgs.Paging;
using TGS.Services.Operator.StationLetters;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;

public record GetStationLettersQuery(string PlayerCode, Paging<StationLetter> Paging);