using GuestCard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;



namespace GuestCard.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private GuestcardContext context { get; set; }

        public HomeController(GuestcardContext ctx) => context = ctx;

        public IActionResult Index(string id)
        {
            // load current filters and data needed for filter drop downs in ViewBag
            var filters = new Filters(id);
            ViewBag.Filters = filters;
            ViewBag.Hosts = context.Hosts.ToList();
            ViewBag.Statuses = context.Statuses.ToList();
            ViewBag.DueFilters = Filters.DueFilterValues;

            // get open tasks from database based on current filters
            IQueryable<Guest> query = context.Guests
                .Include(t => t.Host).Include(t => t.Status);

            if (filters.HasCategory)
            {
                query = query.Where(t => t.HostId == int.Parse(filters.HostId));
            }
            if (filters.HasStatus)
            {
                query = query.Where(t => t.StatusId == filters.StatusId);
            }
            if (filters.HasDue)
            {
                var today = DateTime.Today;
                if (filters.IsPast)
                    query = query.Where(t => t.DueDate < today);
                else if (filters.IsFuture)
                    query = query.Where(t => t.DueDate > today);
                else if (filters.IsToday)
                    query = query.Where(t => t.DueDate == today);
            }
            var tasks = query.OrderBy(t => t.DueDate).ToList();

            //return Content("Product controller, Index action");
            return View(tasks);
        }

        [HttpPost]
        public IActionResult Filter(string[] filter)
        {
            string id = string.Join('-', filter);
            return RedirectToAction("Index", new { ID = id });
        }

        [HttpPost]
        public IActionResult MarkComplete([FromRoute] string id, Guest selected)
        {
            selected = context.Guests.Find(selected.GuestId)!;  // use null-forgiving operator to suppress null warning
            if (selected != null)
            {
                selected.StatusId = "closed";
                context.SaveChanges();
            }

            return RedirectToAction("Index", new { ID = id });
        }

        [HttpPost]
        public IActionResult DeleteComplete(string id)
        {
            var toDelete = context.Guests
                .Where(t => t.StatusId == "closed").ToList();

            foreach (var task in toDelete)
            {
                context.Guests.Remove(task);
            }
            context.SaveChanges();

            return RedirectToAction("Index", new { ID = id });
        }
    }
}
