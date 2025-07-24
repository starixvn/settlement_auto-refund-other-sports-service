using Microsoft.Extensions.Options;

namespace AutoRefund.UnitTests.Services;

public class VerificationStatusServiceTests
{
    private readonly CreateQueryService createQueryService;

    private readonly IDynamicRepository repository;

    private readonly VerificationStatusService service;

    public VerificationStatusServiceTests()
    {
        createQueryService = Substitute.For<CreateQueryService>();
        repository = Substitute.For<IDynamicRepository>();
        var appSettingsOptions = Options.Create(new AppSettingsKey { SiteDbServer = ["01", "02"] });
        service = new VerificationStatusServiceImp(createQueryService, repository, appSettingsOptions);
    }

    [Fact]
    public async Task CheckRunningOdds_Always_ResultCriteria()
    {
        var criteria = new CheckOddsCriteria();
        createQueryService.CreateQueryCheckOdds(Arg.Any<Match>()).Returns(criteria);
        repository.When(fake => fake.ExecuteAsync(criteria)).Do(_ =>
        {
            criteria.ErrCode = 1;
        });
        var result = await service.CheckRunningOdds(new Match());
        Assert.Equal(criteria, result);
    }
}