﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ntbs_service.Models;
using ntbs_service.Models.Entities;

namespace ntbs_service.Services
{
    public interface ISearchService
    {
        IQueryable<Notification> FilterById(IQueryable<Notification> IQ, string IdFilter);
        IQueryable<Notification> FilterBySex(IQueryable<Notification> IQ, int sexId);
        IQueryable<Notification> FilterByPartialDate(IQueryable<Notification> IQ, PartialDate partialDate);
        Task<(IList<int> notificationIds, int count)> OrderAndPaginateQueryablesAsync(INtbsSearchBuilder firstQueryable, 
            INtbsSearchBuilder secondQueryable, PaginationParameters paginationParameters);
    }

    public class SearchService : ISearchService
    {

        public IQueryable<Notification> FilterById(IQueryable<Notification> notifications, string IdFilter) 
        {
            int.TryParse(IdFilter, out int parsedIdFilter);
            return notifications.Where(s => s.NotificationId.Equals(parsedIdFilter) 
                    || s.ETSID.Equals(IdFilter) || s.LTBRID.Equals(IdFilter) || s.PatientDetails.NhsNumber.Equals(IdFilter));
        }

        public IQueryable<Notification> FilterByPartialDate(IQueryable<Notification> notifications, PartialDate partialDate) 
        {
            partialDate.TryConvertToDateTimeRange(out DateTime? dateRangeStart, out DateTime? dateRangeEnd);
            return notifications.Where(s => s.PatientDetails.Dob >= dateRangeStart && s.PatientDetails.Dob < dateRangeEnd);
        }

        public IQueryable<Notification> FilterBySex(IQueryable<Notification> notifications, int sexId) 
        {
            return notifications.Where(s => s.PatientDetails.SexId.Equals(sexId));
        }

        public async Task<(IList<int> notificationIds, int count)> OrderAndPaginateQueryablesAsync(
            INtbsSearchBuilder firstBuilder,
            INtbsSearchBuilder secondBuilder,
            PaginationParameters paginationParameters)
        {
            var notificationIdsQueryable = OrderQueryableByNotificationDate(firstBuilder.GetResult())
                                                                .Union(OrderQueryableByNotificationDate(secondBuilder.GetResult()));

            var notificationIds = await GetPaginatedItemsAsync(notificationIdsQueryable.Select(n => n.NotificationId), paginationParameters);
            var count = await notificationIdsQueryable.CountAsync();
            return (notificationIds, count);
        }

        private IQueryable<Notification> OrderQueryableByNotificationDate(IQueryable<Notification> query) 
        {
            return query
                .OrderByDescending(n => n.NotificationDate ?? n.CreationDate)
                // For notifications with the same NotificationDate order by NotificationId so that each search returns the same order each time
                .OrderByDescending(n => n.NotificationId);
        }

        private async Task<IList<T>> GetPaginatedItemsAsync<T>(IQueryable<T> items, PaginationParameters paginationParameters)
        {
            return await items.Skip(paginationParameters.NtbsOffset ?? 0)
                .Take(paginationParameters.NumberOfNtbsNotificationsToFetch).ToListAsync();
        }
    }
}
