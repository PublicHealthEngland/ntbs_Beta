using System.Threading.Tasks;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.Configuration;
using ntbs_service.Properties;
using Serilog;

namespace ntbs_service.Jobs
{
    public class GenericStoredProcedureJob : StoredProcedureJobBase
    {
        public GenericStoredProcedureJob(IConfiguration configuration)
        : base(configuration)
        {
            var scheduledJobConfig = new ScheduledJobsConfig();
            configuration.GetSection(Constants.ScheduledJobsConfig).Bind(scheduledJobConfig);

            _sqlString = scheduledJobConfig.GenericStoredProcedureNameToRun;
            _parameters = null;
        }

        /// PerformContext context is passed in via Hangfire Server
        public override async Task Run(PerformContext context, IJobCancellationToken token)
        {
            _context = context;
            Log.Information($"Starting generic stored procedure job: {_sqlString}");
            _context.WriteLine($"Starting generic stored procedure job: {_sqlString}");

            await base.Run(context, token);

            Log.Information($"Finishing generic stored procedure job.");
        }
    }
}
