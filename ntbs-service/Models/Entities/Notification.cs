﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EFAuditer;
using ExpressiveAnnotations.Attributes;
using ntbs_service.Helpers;
using ntbs_service.Models.Enums;
using ntbs_service.Models.Validations;

namespace ntbs_service.Models.Entities
{
    public class Notification : ModelBase, IOwnedEntityForAuditing
    {
        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        public Notification()
        {
            NotificationStatus = NotificationStatus.Draft;
            PatientDetails = new PatientDetails();
            HospitalDetails = new HospitalDetails();
            SocialRiskFactors = new SocialRiskFactors();
            ClinicalDetails = new ClinicalDetails();
            PatientTBHistory = new PatientTBHistory();
            ContactTracing = new ContactTracing();
            ImmunosuppressionDetails = new ImmunosuppressionDetails();
            TravelDetails = new TravelDetails();
            VisitorDetails = new VisitorDetails();
            ComorbidityDetails = new ComorbidityDetails();
            MDRDetails = new MDRDetails();
            TestData = new TestData();
            DrugResistanceProfile = new DrugResistanceProfile();
            MBovisDetails = new MBovisDetails();
        }

        #region DB Mapped Fields

        [Display(Name = "NTBS Id")]
        public int NotificationId { get; set; }
        [MaxLength(50)]
        public string ETSID { get; set; }
        [MaxLength(50)]
        public string LTBRPatientId { get; set; }
        [MaxLength(50)]
        public string LTBRID { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? SubmissionDate { get; set; }
        [Display(Name = "Deletion reason")]
        [MaxLength(150)]
        public string DeletionReason { get; set; }
        public int? GroupId { get; set; }
        [Display(Name = "Cluster Id")]
        public string ClusterId { get; set; }

        [Display(Name = "Notification date")]
        [RequiredIf(@"ShouldValidateFull", ErrorMessage = ValidationMessages.FieldRequired)]
        [AssertThat(@"PatientDetails.Dob == null || NotificationDate > PatientDetails.Dob", ErrorMessage = ValidationMessages.DateShouldBeLaterThanDob)]
        [ValidClinicalDate]
        public DateTime? NotificationDate { get; set; }
        public NotificationStatus NotificationStatus { get; set; }

        [NotMapped]
        public override bool? IsLegacy => LTBRID != null || ETSID != null;

        #endregion

        #region Navigation Properties

        [AssertThat("NotificationSites.Count > 0 || !ShouldValidateFull", ErrorMessage = ValidationMessages.DiseaseSiteIsRequired)]
        public virtual List<NotificationSite> NotificationSites { get; set; }
        public virtual PatientDetails PatientDetails { get; set; }
        public virtual ClinicalDetails ClinicalDetails { get; set; }
        public virtual HospitalDetails HospitalDetails { get; set; }
        public virtual PatientTBHistory PatientTBHistory { get; set; }
        public virtual ContactTracing ContactTracing { get; set; }
        public virtual SocialRiskFactors SocialRiskFactors { get; set; }
        public virtual ImmunosuppressionDetails ImmunosuppressionDetails { get; set; }
        public virtual TravelDetails TravelDetails { get; set; }
        public virtual VisitorDetails VisitorDetails { get; set; }
        public virtual DenotificationDetails DenotificationDetails { get; set; }
        public virtual ComorbidityDetails ComorbidityDetails { get; set; }
        public virtual MDRDetails MDRDetails { get; set; }
        public virtual NotificationGroup Group { get; set; }
        public virtual TestData TestData { get; set; }
        public virtual MBovisDetails MBovisDetails { get; set; }
        public virtual ICollection<Alert> Alerts { get; set; }
        public virtual ICollection<SocialContextVenue> SocialContextVenues { get; set; }
        public virtual ICollection<SocialContextAddress> SocialContextAddresses { get; set; }
        public virtual ICollection<TreatmentEvent> TreatmentEvents { get; set; }
        public virtual DrugResistanceProfile DrugResistanceProfile { get; set; }

        #endregion

        #region Display and Formatting methods/fields
        public string PreviouslyHadTBYesNo => (PatientTBHistory.PreviouslyHadTB).FormatYesNo();
        public string HasRecentVisitor => VisitorDetails.HasVisitor.FormatYesNo();
        public string HasRecentTravel => TravelDetails.HasTravel.FormatYesNo();
        public string SitesOfDiseaseList => CreateSitesOfDiseaseString();
        public string DrugRiskFactorTimePeriods => CreateTimePeriodsString(SocialRiskFactors.RiskFactorDrugs);
        public string HomelessRiskFactorTimePeriods => CreateTimePeriodsString(SocialRiskFactors.RiskFactorHomelessness);
        public string ImprisonmentRiskFactorTimePeriods => CreateTimePeriodsString(SocialRiskFactors.RiskFactorImprisonment);
        [Display(Name = "Date created")]
        public string FormattedCreationDate => CreationDate.ConvertToString();
        [Display(Name = "Date notified")]
        public string FormattedNotificationDate => NotificationDate.ConvertToString();
        public int? AgeAtNotification => GetAgeAtTimeOfNotification();
        public string MDRCaseCountryName => MDRDetails.Country?.Name;
        public bool HasBeenNotified => NotificationStatus == NotificationStatus.Notified || NotificationStatus == NotificationStatus.Legacy;
        public string LegacyId => LTBRID ?? ETSID;
        public bool TransferRequestPending => Alerts?.Any(x => x.AlertType == AlertType.TransferRequest && x.AlertStatus == AlertStatus.Open) == true;
        public bool OverOneYearOld => NotificationDate != null && DateTime.Now > NotificationDate.Value.AddYears(1);
        public string FormattedDenotificationDate => DenotificationDetails?.DateOfDenotification.ConvertToString();
        public string DenotificationReasonString => DenotificationDetails?.Reason.GetDisplayName() + 
                                                    (DenotificationDetails?.Reason == DenotificationReason.Other ? $" - {DenotificationDetails?.OtherDescription}" : "");
        public bool IsMdr => ClinicalDetails.IsMDRTreatment || DrugResistanceProfile.DrugResistanceProfileString == "RR/MDR/XDR";
        public bool IsMBovis => string.Equals("M. bovis", DrugResistanceProfile.Species, StringComparison.InvariantCultureIgnoreCase);

        private string CreateSitesOfDiseaseString()
        {
            if (NotificationSites == null)
            {
                return string.Empty;
            }

            var siteNames = NotificationSites.Select(ns => ns.Site)
                .Where(ns => ns != null)
                .Select(s => s.Description);
            return string.Join(", ", siteNames);
        }
        
        private static string CreateTimePeriodsString(RiskFactorDetails riskFactor)
        {
            var timeStrings = new List<string>();
            if (riskFactor.IsCurrent == true)
            {
                timeStrings.Add("current");
            }
            if (riskFactor.InPastFiveYears == true)
            {
                timeStrings.Add("less than 5 years ago");
            }
            if (riskFactor.MoreThanFiveYearsAgo == true)
            {
                timeStrings.Add("more than 5 years ago");
            }
            return string.Join(", ", timeStrings);
        }

        private int? GetAgeAtTimeOfNotification()
        {
            if (NotificationDate == null || PatientDetails?.Dob == null)
            {
                return null;
            }

            var notificationDate = (DateTime)NotificationDate;
            var birthDate = (DateTime)PatientDetails.Dob;

            var yearDiff = notificationDate.Year - birthDate.Year;
            if ((birthDate.Month * 100 + birthDate.Day) > (notificationDate.Month * 100 + notificationDate.Day))
            {
                yearDiff -= 1;
            }
            return yearDiff;
        }
        
        #endregion
        
        [AssertThat(@"ShouldValidateFull && HasDeathEventForPostMortemCase", ErrorMessage = ValidationMessages.DeathEventRequiredForPostMortemCase)]
        public bool HasDeathEventForPostMortemCase =>
            ClinicalDetails.IsPostMortem != true || (TreatmentEvents != null && TreatmentEvents.Any(x =>
                x.TreatmentEventTypeIsOutcome && x.TreatmentOutcome.TreatmentOutcomeType == TreatmentOutcomeType.Died));

        string IOwnedEntityForAuditing.RootEntityType => RootEntities.Notification;
    }
}
