using AutoRefundOtherSports.Criteria;
using AutoRefundOtherSports.Models;
using Fanex.Data.Repository;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoRefundOtherSports.Services
{
    public interface ILoggingService
    {
        Task WriteLog(Match match, string message);

        Task WriteLog(Match match, AutoRefundException exception);
    }

    public class LoggingServiceImp : ILoggingService
    {
        private readonly IDynamicRepository repository;

        public LoggingServiceImp(IDynamicRepository repository)
        {
            this.repository = repository;
        }

        public async Task WriteLog(Match match, string message)
        {
            var errorMessage = GenerateErrorLogMessage(match, message);
            var criteria = new WriteLogCriteria(match, errorMessage);
            await repository.ExecuteAsync(criteria);
        }

        public async Task WriteLog(Match match, AutoRefundException exception)
        {
            var message = exception.Description ?? "Auto Refund failed due to system error.";
            var errorMessage = GenerateErrorLogMessage(match, message);
            var criteria = new WriteLogCriteria(match, errorMessage);
            await repository.ExecuteAsync(criteria);
        }

        private static string GenerateErrorLogMessage(Match match, string message)
        {
            var resourceIds = match.ResultDetails.Select(resultDetail => resultDetail.ResourceId).ToList();
            var resources = string.Join(",", resourceIds);
            var title = $"NAP: Auto Refund OS Result: {resources}";
            var remark = new List<string>
            {
                title, message
            };

            return string.Join("<br/>", remark);
        }
    }
}