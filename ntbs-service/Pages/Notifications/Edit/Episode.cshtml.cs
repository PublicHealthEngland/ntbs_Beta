using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ntbs_service.Models;
using ntbs_service.Pages_Notifications;
using ntbs_service.Services;

namespace ntbs_service.Pages.Notifications.Edit
{
    public class EpisodeModel : NotificationEditModelBase
    {
        private readonly NtbsContext context;

        public SelectList TBServices { get; set; }
        public SelectList Hospitals { get; set; }

        [BindProperty]
        public Episode Episode { get; set; }

        public EpisodeModel(INotificationService service, IAuthorizationService authorizationService, NtbsContext context) : base(service, authorizationService)
        {
            this.context = context;
        }

        public override async Task<IActionResult> OnGetAsync(int id, bool isBeingSubmitted)
        {
            return await base.OnGetAsync(id, isBeingSubmitted);
        }

        protected override async Task<IActionResult> PreparePageForGet(int id, bool isBeingSubmitted)
        {
            Episode = Notification.Episode;
            await SetNotificationProperties(isBeingSubmitted, Episode);

            TBServices = new SelectList(context.GetAllTbServicesAsync().Result,
                                        nameof(TBService.Code),
                                        nameof(TBService.Name));

            Hospitals = new SelectList(context.GetAllHospitalsAsync().Result,
                                        nameof(Hospital.HospitalId),
                                        nameof(Hospital.Name));

            if (Episode.ShouldValidateFull)
            {
                TryValidateModel(Episode, Episode.GetType().Name);
            }

            return Page();
        }

        public JsonResult OnGetHospitalsByTBService(string tbServiceCode)
        {
            var tbServices = context.GetHospitalsByTBService(tbServiceCode).Result;
            return new JsonResult(tbServices);
        }

        protected override IActionResult RedirectToNextPage(int? notificationId, bool isBeingSubmitted)
        {
            return RedirectToPage("./ClinicalDetails", new { id = notificationId, isBeingSubmitted });
        }

        protected override async Task<bool> ValidateAndSave() 
        {
            Episode.SetFullValidation(Notification.NotificationStatus);
            if (!TryValidateModel(this))
            {
                return false;
            }

            await service.UpdateEpisodeAsync(Notification, Episode);
            return true;
        }

        public ContentResult OnGetValidateEpisodeProperty(string key, string value, bool shouldValidateFull)
        {
            return validationService.ValidateModelProperty<Episode>(key, value, shouldValidateFull);
        }
    }
}