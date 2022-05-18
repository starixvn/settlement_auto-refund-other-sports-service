using AutoRefundOtherSports.Criteria;
using AutoRefundOtherSports.Models;
using Fanex.Data.Repository;
using System.Collections.Generic;

namespace AutoRefundOtherSports.Services
{
    public interface ILoggingService
    {
        void WriteLog(Match match, string message);

        void WriteLog(Match match, AutoRefundException exception);
    }

    public class LoggingServiceImp : ILoggingService
    {
        private readonly IDynamicRepository repository;

        public LoggingServiceImp(IDynamicRepository repository)
        {
            this.repository = repository;
        }

        public void WriteLog(Match match, string message)
        {
            var errorMessage = GenerateErrorLogMessage(match, message);
            var criteria = new WriteLogCriteria(match, errorMessage);
            repository.Execute(criteria);
        }

        public void WriteLog(Match match, AutoRefundException exception)
        {
            var message = exception.Description ?? "Auto Refund failed due to system error.";
            var errorMessage = GenerateErrorLogMessage(match, message);
            var criteria = new WriteLogCriteria(match, errorMessage);
            repository.Execute(criteria);
        }

        private static string GenerateErrorLogMessage(Match match, string message)
        {
            var resourceIds = new List<string>();
            foreach (var resultDetail in match.ResultDetails)
            {
                resourceIds.Add(resultDetail.ResourceId);
            }

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