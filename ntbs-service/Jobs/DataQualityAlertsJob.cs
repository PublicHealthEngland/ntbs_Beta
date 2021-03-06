﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using ntbs_service.DataAccess;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Entities.Alerts;
using ntbs_service.Services;
using Serilog;

namespace ntbs_service.Jobs
{
    public class DataQualityAlertsJob : HangfireJobContext
    {
        private readonly IAlertService _alertService;
        private readonly IDataQualityRepository _dataQualityRepository;
        private const int CountPerBatch = 500;

        public DataQualityAlertsJob(
            IAlertService alertService,
            IDataQualityRepository dataQualityRepository,
            NtbsContext ntbsContext) : base(ntbsContext)
        {
            _alertService = alertService;
            _dataQualityRepository = dataQualityRepository;
        }

        private delegate Task<int> GetNotificationsEligibleForDqAlertsCount();
        private delegate Task<IList<Notification>> GetMultipleNotificationsEligibleForDataQualityDraftAlerts(
            int countPerBatch,
            int offset);

        private async Task CreateDraftAlertsInBulkAsync() => await CreateAlertsInBulkAsync<DataQualityDraftAlert>(
            _dataQualityRepository.GetNotificationsEligibleForDqDraftAlertsCountAsync,
            _dataQualityRepository.GetNotificationsEligibleForDqDraftAlertsAsync
        );

        private async Task CreateBirthCountryAlertsInBulkAsync() => await CreateAlertsInBulkAsync<DataQualityBirthCountryAlert>(
            _dataQualityRepository.GetNotificationsEligibleForDqBirthCountryAlertsCountAsync,
            _dataQualityRepository.GetNotificationsEligibleForDqBirthCountryAlertsAsync
        );

        private async Task CreateClinicalDatesAlertsInBulkAsync() => await CreateAlertsInBulkAsync<DataQualityClinicalDatesAlert>(
            _dataQualityRepository.GetNotificationsEligibleForDqClinicalDatesAlertsCountAsync,
            _dataQualityRepository.GetNotificationsEligibleForDqClinicalDatesAlertsAsync
        );

        private async Task CreateDotVotAlertsInBulkAsync() => await CreateAlertsInBulkAsync<DataQualityDotVotAlert>(
            _dataQualityRepository.GetNotificationsEligibleForDqDotVotAlertsCountAsync,
            _dataQualityRepository.GetNotificationsEligibleForDqDotVotAlertsAsync
        );

        private async Task CreateTreatmentOutcome12MonthAlertsInBulkAsync() => await CreateAlertsInBulkAsync<DataQualityTreatmentOutcome12>(
            _dataQualityRepository.GetNotificationsEligibleForDqTreatmentOutcome12AlertsCountAsync,
            _dataQualityRepository.GetNotificationsEligibleForDqTreatmentOutcome12AlertsAsync
        );

        private async Task CreateTreatmentOutcome24MonthAlertsInBulkAsync() => await CreateAlertsInBulkAsync<DataQualityTreatmentOutcome24>(
            _dataQualityRepository.GetNotificationsEligibleForDqTreatmentOutcome24AlertsCountAsync,
            _dataQualityRepository.GetNotificationsEligibleForDqTreatmentOutcome24AlertsAsync
        );

        private async Task CreateTreatmentOutcome36MonthAlertsInBulkAsync() => await CreateAlertsInBulkAsync<DataQualityTreatmentOutcome36>(
            _dataQualityRepository.GetNotificationsEligibleForDqTreatmentOutcome36AlertsCountAsync,
            _dataQualityRepository.GetNotificationsEligibleForDqTreatmentOutcome36AlertsAsync
        );

        private async Task CreateAlertsInBulkAsync<T>(
            GetNotificationsEligibleForDqAlertsCount getCount,
            GetMultipleNotificationsEligibleForDataQualityDraftAlerts getNotifications) where T : Alert
        {
            var notificationsForAlertsCount = await getCount();
            var offset = 0;
            while (offset < notificationsForAlertsCount)
            {
                var notificationsForAlerts = await getNotifications(CountPerBatch, offset);

                var now = DateTime.Now;
                var dqAlerts = notificationsForAlerts
                    .Select(n =>
                    {
                        var alert = (T)Activator.CreateInstance(typeof(T));
                        alert.NotificationId = n.NotificationId;
                        alert.CreationDate = now;

                        return (Alert)alert;
                    })
                    .ToList();

                // When sourcing the alerts, we made sure to only take ones where no duplicate alerts will be created.
                // This means we are safe to just add them all and not have to ask _alertService to repeat that check
                await _alertService.AddAlertsRangeAsync(dqAlerts);
                offset += CountPerBatch;
            }
        }

        public async Task Run(IJobCancellationToken token)
        {
            Log.Information($"Starting data quality alerts job");
            await CreateDraftAlertsInBulkAsync();
            await CreateBirthCountryAlertsInBulkAsync();
            await CreateClinicalDatesAlertsInBulkAsync();
            await CreateDotVotAlertsInBulkAsync();
            await CreateTreatmentOutcome12MonthAlertsInBulkAsync();
            await CreateTreatmentOutcome24MonthAlertsInBulkAsync();
            await CreateTreatmentOutcome36MonthAlertsInBulkAsync();

            var possibleDuplicateNotificationIds =
                await _dataQualityRepository.GetNotificationIdsEligibleForDqPotentialDuplicateAlertsAsync();
            foreach (var notification in possibleDuplicateNotificationIds)
            {
                var alert = new DataQualityPotentialDuplicateAlert
                {
                    NotificationId = notification.NotificationId,
                    DuplicateId = notification.DuplicateId
                };
                await _alertService.AddUniquePotentialDuplicateAlertAsync(alert);
            }

            Log.Information($"Finished data quality alerts job");
        }
    }
}
