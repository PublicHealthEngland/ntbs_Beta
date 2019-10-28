using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ntbs_service.Models;
using ntbs_service.Pages_Notifications;
using ntbs_service.Services;

namespace ntbs_service.Pages.Notifications.Edit
{
    public class TravelModel : NotificationEditModelBase
    {
        private readonly NtbsContext context;

        [BindProperty]
        public TravelDetails TravelDetails { get; set; }

        [BindProperty]
        public VisitorDetails VisitorDetails { get; set; }

        public SelectList HighTbIncidenceCountries { get; set; }

        public TravelModel(INotificationService service, IAuthorizationService authorizationService, NtbsContext context) : base(service, authorizationService)
        {
            this.context = context;
            HighTbIncidenceCountries = new SelectList(
                context.GetAllHighTbIncidenceCountriesAsync().Result,
                nameof(Country.CountryId),
                nameof(Country.Name));
        }

        public override async Task<IActionResult> OnGetAsync(int id, bool isBeingSubmitted)
        {
            return await base.OnGetAsync(id, isBeingSubmitted);
        }

        protected override async Task<IActionResult> PreparePageForGet(int id, bool isBeingSubmitted)
        {
            TravelDetails = Notification.TravelDetails;
            VisitorDetails = Notification.VisitorDetails;
            await SetNotificationProperties(isBeingSubmitted, TravelDetails);
            await SetNotificationProperties(isBeingSubmitted, VisitorDetails);

            if (TravelDetails.ShouldValidateFull)
                TryValidateModel(TravelDetails, TravelDetails.GetType().Name);
            if (VisitorDetails.ShouldValidateFull)
                TryValidateModel(VisitorDetails, VisitorDetails.GetType().Name);

            return Page();
        }

        protected override IActionResult RedirectToNextPage(int? notificationId, bool isBeingSubmitted)
        {
            return RedirectToPage("./Comorbidities", new { id = notificationId, isBeingSubmitted });
        }

        protected override async Task<bool> ValidateAndSave()
        {
            if (!ModelState.IsValid)
            {
                return false;
            }

            await service.UpdateTravelAndVisitorAsync(Notification, TravelDetails, VisitorDetails);
            return true;
        }

        public IActionResult OnGetValidateTravel(TravelDetails travelDetails)
        {
            return validationService.ValidateFullModel(travelDetails);
        }

        public IActionResult OnGetValidateVisitor(VisitorDetails visitorDetails)
        {
            return validationService.ValidateFullModel(visitorDetails);
        }
    }
}