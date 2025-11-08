using Neasden.Consumer.Clients.StratfordClient;
using Neasden.Consumer.Repositories;
using Neasden.Library.Clients;
using Neasden.Models;
using NSubstitute;

namespace Neasden.Consumer.Unit.Tests;
public class DisruptionNotifierTests
{
    private readonly DisruptionNotifier _notifier;

    private readonly IWaterlooClient _waterlooClient;
    private readonly IStratfordClient _stratfordClient;
    private readonly IUserNotifiedRepository _userNotifiedRepository;

    private readonly Line _line;
    private readonly Station _startStation;
    private readonly Station _endStation;
    private readonly TimeOnly _endTime;
    private readonly IEnumerable<Station> _affectedStations;

    public DisruptionNotifierTests()
    {
        _line = new Line(Guid.Parse("8c3a4d59-f2e0-46a8-9f56-ec27eaffded9"), "District");
        _startStation = new Station(Guid.Parse("73bce1de-143f-4903-928a-c34ceb3db42e"), "Mile End");
        _endStation = new Station(Guid.Parse("968bc258-138c-45cf-83c0-599705285d25"), "West Ham");
        _endTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddMinutes(30));
        _affectedStations = [
            new Station(Guid.Parse("73bce1de-143f-4903-928a-c34ceb3db42e"), "Mile End"),
            new Station(Guid.Parse("3db408d6-248a-4ef7-8486-203e87cc408a"), "Bow Road"),
            new Station(Guid.Parse("a391396c-6921-4202-ace2-2d5033bfac1f"), "Bromley By Bow"),
            new Station(Guid.Parse("968bc258-138c-45cf-83c0-599705285d25"), "West Ham"),
            ];

        _waterlooClient =  Substitute.For<IWaterlooClient>();
        _stratfordClient = Substitute.For<IStratfordClient>();
        _userNotifiedRepository =  Substitute.For<IUserNotifiedRepository>();
    }
}
