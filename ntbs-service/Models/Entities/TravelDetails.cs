﻿using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExpressiveAnnotations.Attributes;
using Microsoft.EntityFrameworkCore;
using ntbs_service.Models.ReferenceEntities;
using ntbs_service.Models.Validations;

namespace ntbs_service.Models.Entities
{
    [Owned]
    public class TravelDetails : ModelBase, ITravelOrVisitorDetails
    {
        private const int MaxTotalLengthOfStay = 24;

        public bool? HasTravel { get; set; }

        [RequiredIf("ShouldValidateFull && HasTravel == true", ErrorMessage = ValidationMessages.TravelOrVisitTotalNumberOfCountriesRequired)]
        [Range(1, 50)]
        [AssertThat("TotalNumberGreaterOrEqualToInputCountries == true", ErrorMessage = ValidationMessages.TotalNumberOfCountriesTravelledToGreaterThanInputNumber)]
        [DisplayName("total number of countries")]
        public int? TotalNumberOfCountries { get; set; }


        // First country block
        [RequiredIf("ShouldValidateFull && HasTravel == true", ErrorMessage = ValidationMessages.TravelMostRecentCountryRequired)]
        public int? Country1Id { get; set; }

        public virtual Country Country1 { get; set; }

        [RequiredIf("ShouldValidateFull && HasTravel == true", ErrorMessage = ValidationMessages.TravelCountryRequiresDuration)]
        [Range(1, MaxTotalLengthOfStay)]
        [AssertThat("TotalLengthWithinLimit == true", ErrorMessage = ValidationMessages.TravelTotalDurationWithinLimit)]
        [DisplayName("duration of travel")]
        public int? StayLengthInMonths1 { get; set; }


        // Second country block
        [AssertThat("!ShouldValidateFull || Country1Id != null", ErrorMessage = ValidationMessages.TravelIsChronological)]
        [AssertThat("Country2Id != Country1Id", ErrorMessage = ValidationMessages.TravelUniqueCountry)]
        public int? Country2Id { get; set; }

        public Country Country2 { get; set; }

        [RequiredIf("ShouldValidateFull && Country2Id != null", ErrorMessage = ValidationMessages.TravelCountryRequiresDuration)]
        [AssertThat("!ShouldValidateFull || Country2Id != null", ErrorMessage = ValidationMessages.TravelOrVisitDurationHasCountry)]
        [Range(1, MaxTotalLengthOfStay)]
        [AssertThat("TotalLengthWithinLimit == true", ErrorMessage = ValidationMessages.TravelTotalDurationWithinLimit)]
        [DisplayName("duration of travel")]
        public int? StayLengthInMonths2 { get; set; }


        // Third country block
        [AssertThat("!ShouldValidateFull || Country2Id != null", ErrorMessage = ValidationMessages.TravelIsChronological)]
        [AssertThat("Country3Id != Country1Id && Country3Id != Country2Id", ErrorMessage = ValidationMessages.TravelUniqueCountry)]
        public int? Country3Id { get; set; }

        public Country Country3 { get; set; }

        [RequiredIf("ShouldValidateFull && Country3Id != null", ErrorMessage = ValidationMessages.TravelCountryRequiresDuration)]
        [AssertThat("!ShouldValidateFull || Country3Id != null", ErrorMessage = ValidationMessages.TravelOrVisitDurationHasCountry)]
        [Range(1, MaxTotalLengthOfStay)]
        [AssertThat("TotalLengthWithinLimit == true", ErrorMessage = ValidationMessages.TravelTotalDurationWithinLimit)]
        [DisplayName("duration of travel")]
        public int? StayLengthInMonths3 { get; set; }


        // Helper properties for use in expressive annotations
        [NotMapped]
        public bool TotalNumberGreaterOrEqualToInputCountries =>
            TotalNumberOfCountries >=
            Convert.ToInt32(Country1Id.HasValue) +
            Convert.ToInt32(Country2Id.HasValue) +
            Convert.ToInt32(Country3Id.HasValue);

        [NotMapped]
        public bool TotalLengthWithinLimit =>
            MaxTotalLengthOfStay >= TotalDurationOfTravel;

        [NotMapped]
        public int TotalDurationOfTravel => 
            Convert.ToInt32(StayLengthInMonths1) +
            Convert.ToInt32(StayLengthInMonths2) +
            Convert.ToInt32(StayLengthInMonths3);
    }
}
