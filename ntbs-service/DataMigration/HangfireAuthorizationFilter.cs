using Hangfire.Dashboard;

namespace ntbs_service.DataMigration
{
    public class HangfireAuthorisationFilter : IDashboardAuthorizationFilter
    {
        private readonly string _policyName;
        public HangfireAuthorisationFilter(string policyName)
        {
            _policyName = policyName;
        }

        public bool Authorize(DashboardContext context)
        {
            return context.GetHttpContext().User.IsInRole(_policyName);
        }
    }
}