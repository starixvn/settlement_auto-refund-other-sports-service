using AutoRefundOtherSports;

namespace AutoRefund.UnitTests.Services
{
    public class LoggingServiceTests
    {
        private readonly IDynamicRepository repository;

        private readonly CreateQueryService createQueryService;

        private readonly LoggingService service;

        public LoggingServiceTests()
        {
            repository = Substitute.For<IDynamicRepository>();
            createQueryService = Substitute.For<CreateQueryService>();
            service = new LoggingServiceImp(repository, createQueryService);
        }

        [Fact]
        public async Task WriteLog_Message_WriteCorrectLog()
        {
            var match = new Match
            {
                ResultDetails = new List<ResultDetail>
                {
                    new()
                    {
                        ResourceId = "0101"
                    }
                }
            };

            var logMessage = "NAP: Auto Refund OS Result: 0101<br/>Log message";

            await service.WriteLog(match, "Log message");

            await repository.Received().ExecuteAsync(Arg.Is<WriteLogCriteria>(c => c.Remark == logMessage));
        }

        [Theory]
        [InlineData(null, "NAP: Auto Refund OS Result: 0101<br/>Auto Refund failed due to system error.")]
        [InlineData("Error message", "NAP: Auto Refund OS Result: 0101<br/>Error message")]
        public async Task WriteLog_Exception_WriteCorrectLog(string description, string logMessage)
        {
            var match = new Match
            {
                ResultDetails = new List<ResultDetail>
                {
                    new()
                    {
                        ResourceId = "0101"
                    }
                }
            };

            var exception = new AutoRefundException
            {
                Description = description
            };
            await service.WriteLog(match, exception);

            await repository.Received().ExecuteAsync(Arg.Is<WriteLogCriteria>(c => c.Remark == logMessage));
        }

        [Fact]
        public async Task UpdateRemark_Always_ExecuteCriteria()
        {
            var criteria = new UpdateResultStatusCriteria();
            createQueryService.QueryUpdateResult(Arg.Any<Match>(), Arg.Any<string>(), Arg.Any<int>())
                .Returns(criteria);

            await service.Update(new Match(), "", 0);

            await repository.Received().ExecuteAsync(criteria);
        }
    }
}