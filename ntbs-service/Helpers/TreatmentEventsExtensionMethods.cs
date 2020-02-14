﻿using System;
using System.Collections.Generic;
using System.Linq;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Enums;

namespace ntbs_service.Helpers
{
    public static class EpisodesExtensionMethods
    {
        static readonly List<TreatmentOutcomeType> episodeEndingOutcomeTypes = new List<TreatmentOutcomeType>
        {
            TreatmentOutcomeType.Completed,
            TreatmentOutcomeType.Cured,
            TreatmentOutcomeType.Died,
            TreatmentOutcomeType.Lost,
            TreatmentOutcomeType.Failed,
            TreatmentOutcomeType.TreatmentStopped
        };
        
        public static Dictionary<int, List<TreatmentEvent>> GroupByEpisode(this IEnumerable<TreatmentEvent> treatmentEvents)
        {
            var orderedTreatmentEvents = treatmentEvents.OrderBy(t => t.EventDate);
            var groupedEpisodes = new Dictionary<int, List<TreatmentEvent>>();
            var episodeCount = 1;
            
            foreach (var treatmentEvent in orderedTreatmentEvents)
            {
                if (!groupedEpisodes.ContainsKey(episodeCount))
                {
                    groupedEpisodes.Add(episodeCount, new List<TreatmentEvent>() {treatmentEvent});
                }
                else
                {
                    groupedEpisodes[episodeCount].Add(treatmentEvent);
                }
                if (IsEpisodeEndingTreatmentEvent(treatmentEvent))
                {
                    episodeCount++;
                }
            }

            return groupedEpisodes;
        }

        public static TreatmentEvent GetMostRecentTreatmentOutcomeInPeriod(this IEnumerable<TreatmentEvent> treatmentEvents, DateTime startTime, DateTime endTime)
        {
            return treatmentEvents.Where(t => t.TreatmentEventTypeIsOutcome
                                                && t.EventDate > startTime
                                                && t.EventDate <= endTime)
                .OrderByDescending(t => t.EventDate)
                .FirstOrDefault();
        }

        public static TreatmentEvent GetMostRecentTreatmentOutcome(this IEnumerable<TreatmentEvent> treatmentEvents)
        {
            return treatmentEvents.Where(t => t.TreatmentEventTypeIsOutcome)
                .OrderByDescending(t => t.EventDate)
                .FirstOrDefault();
        }

        private static bool IsEpisodeEndingTreatmentEvent(TreatmentEvent treatmentEvent)
        {
            return (treatmentEvent.TreatmentOutcome != null 
                   && episodeEndingOutcomeTypes.Contains(treatmentEvent.TreatmentOutcome.TreatmentOutcomeType))
                   || treatmentEvent.TreatmentEventType == TreatmentEventType.TransferOut;
        }
        
    }
}