﻿using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace ntbs_service.Pages
{
    public class Health : PageModel
    {
        public Health(IConfiguration configuration)
        {
            Release = configuration.GetValue<string>(Constants.RELEASE);
            AuditEnabled = configuration.GetValue<bool>(Constants.AUDIT_ENABLED_CONFIG_VALUE);
            // Note the value negation, since we're turning mocked => enabled
            ClusterMatchingEnabled = !configuration.GetSection(Constants.CLUSTER_MATCHING_CONFIG)
                .GetValue<bool>(Constants.CLUSTER_MATCHING_CONFIG__MOCKOUT);
            // Note the value negation, since we're turning mocked => enabled
            ReferenceLabResultsEnabled = !configuration.GetSection(Constants.REFERENCE_LAB_RESULTS_CONFIG)
                .GetValue<bool>(Constants.REFERENCE_LAB_RESULTS_CONFIG__MOCKOUT);
        }

        public string Release { get; }
        public bool AuditEnabled { get; }
        public bool ClusterMatchingEnabled { get; }
        public bool ReferenceLabResultsEnabled { get; }
    }
}
