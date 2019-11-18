using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFAuditer;
using Microsoft.EntityFrameworkCore;
using ntbs_service.Models;
using ntbs_service.Models.Enums;

namespace ntbs_service.DataAccess
{
    public interface IAlertRepository
    {
        Task<Alert> GetAlertByIdAsync(int? alertId);
        Task<Alert> GetAlertByNotificationIdAndTypeAsync(int? alertId, AlertType alertType);
        Task AddAlertAsync(Alert alert);
        Task UpdateAlertAsync(AuditType auditType = AuditType.Edit);
        Task<IList<Alert>> GetAlertsByTbServices(IEnumerable<string> tbServices);
    }

    public class AlertRepository : IAlertRepository
    {
        private readonly NtbsContext _context;

        public AlertRepository(NtbsContext context)
        {
            this._context = context;
        }

        public async Task AddAlertAsync(Alert alert)
        {
            _context.Alert.Add(alert);
            await UpdateAlertAsync();
        }

        public async Task<Alert> GetAlertByIdAsync(int? alertId)
        {
            return await _context.Alert
                .SingleOrDefaultAsync(m => m.AlertId == alertId);
        }

        public async Task<Alert> GetAlertByNotificationIdAndTypeAsync(int? notificationId, AlertType alertType)
        {
            return await _context.Alert
                .SingleOrDefaultAsync(m => m.NotificationId == notificationId && m.AlertType == alertType);
        }

        public async Task<IList<Alert>> GetAlertsByTbServices(IEnumerable<string> tbServices)
        {
            return await _context.Alert.Where(a => tbServices.Contains(a.TbServiceCode)).ToListAsync();
        }
        
        public async Task UpdateAlertAsync(AuditType auditType = AuditType.Edit)
        {
            _context.AddAuditCustomField(CustomFields.AuditDetails, auditType);
            await _context.SaveChangesAsync();
        }
    }
}