using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AEM.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using AEM.Domains;
using AEM.Data_Access;
using Microsoft.EntityFrameworkCore;

namespace AEM.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private static HttpClient client = new HttpClient();
        private AEMContext _db;

        public HomeController(ILogger<HomeController> logger, AEMContext db)
        {
            _logger = logger;
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Index(User user)
        {

            if (ModelState.IsValid)
            {
                var value = JsonConvert.SerializeObject(new { username = user.Username, password = user.Password }, Formatting.Indented);

                var responseTask = await client.PostAsync("http://test-demo.aem-enersol.com/api/Account/Login", new StringContent(value, Encoding.UTF8, "application/json"));

                if (responseTask.IsSuccessStatusCode)
                {
                    var readTask = await responseTask.Content.ReadAsStringAsync();
                    var TokenString = readTask;
                    return RedirectToAction("Dashboard", new { TokenString });
                }
            }

            return View();
        }

        public IActionResult Dashboard(string TokenString)
        {
            if (!string.IsNullOrEmpty(TokenString))
            {
                var data = new User();
                data.Token = JsonConvert.DeserializeObject<string>(TokenString);
                return View(data);
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> SyncActual(string TokenString)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenString);
            var responseTask = await client.GetAsync("http://test-demo.aem-enersol.com/api/PlatformWell/GetPlatformWellActual");

            List<PlatformDTO> Platforms = new List<PlatformDTO>();
            if (responseTask.IsSuccessStatusCode)
            {
                var readTask = await responseTask.Content.ReadAsStringAsync();
                Platforms = JsonConvert.DeserializeObject<List<PlatformDTO>>(readTask);
            }

            var SyncResult = new SyncResultDTO();
            try{
                if (Platforms.Any())
                {
                    SyncResult = Sync(Platforms);
                }
                SyncResult.Success = true;
            }
            catch (Exception e)
            {
                SyncResult.Success = false;
                SyncResult.Message = e.Message;
            }
            return View(SyncResult);
        }

        public async Task<IActionResult> SyncDummy(string TokenString)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenString);
            var responseTask = await client.GetAsync("http://test-demo.aem-enersol.com/api/PlatformWell/GetPlatformWellDummy");

            List<PlatformDTO> Platforms = new List<PlatformDTO>();
            if (responseTask.IsSuccessStatusCode)
            {
                var readTask = await responseTask.Content.ReadAsStringAsync();
                Platforms = JsonConvert.DeserializeObject<List<PlatformDTO>>(readTask);
            }

            var SyncResult = new SyncResultDTO();
            try
            {
                if (Platforms.Any())
                {
                    SyncResult = Sync(Platforms);
                }
                SyncResult.Success = true;
            }
            catch (Exception e)
            {
                SyncResult.Success = false;
                SyncResult.Message = e.Message;
            }
            return View(SyncResult);
        }

        public SyncResultDTO Sync(List<PlatformDTO> Platforms)
        {
            int pUpdatedCount, pAddedCount, pDeletedCount, wUpdatedCount, wAddedCount, wDeletedCount;
            pUpdatedCount = pAddedCount = pDeletedCount = wUpdatedCount = wAddedCount = wDeletedCount = 0;
            var PlatformIQ = _db.Platforms.AsQueryable();

            foreach (var pItem in Platforms)
            {
                var existPlatform = PlatformIQ.Where(x => x.Id == pItem.Id).Include(x => x.Well).SingleOrDefault();
                if (existPlatform != null)
                {
                    pUpdatedCount++;
                    existPlatform.PlatformName = pItem.UniqueName;
                    existPlatform.Latitude = pItem.Latitude;
                    existPlatform.Longitude = pItem.Longitude;
                    existPlatform.CreatedAt = pItem.CreatedAt ?? existPlatform.CreatedAt;
                    existPlatform.UpdatedAt = pItem.UpdatedAt ?? existPlatform.UpdatedAt;

                    var existWell = existPlatform.Well;

                    var toUpdateWell = existWell?.Where(x => pItem.Well.Any(y => y.Id == x.Id));
                    var toAddWell = pItem.Well.Where(x => existWell.All(y => y.Id != x.Id));
                    var toDeleteWell = existWell?.Where(x => pItem.Well.All(y => y.Id != x.Id));

                    foreach (var wItem in toUpdateWell)
                    {
                        wUpdatedCount++;
                        var updatedData = pItem.Well.Where(x => x.Id == wItem.Id).SingleOrDefault();

                        wItem.WellName = updatedData.UniqueName;
                        wItem.Latitude = updatedData.Latitude;
                        wItem.Longitude = updatedData.Longitude;
                        wItem.CreatedAt = updatedData.CreatedAt ?? wItem.CreatedAt;
                        wItem.UpdatedAt = updatedData.UpdatedAt ?? wItem.UpdatedAt;
                    }

                    foreach (var wItem in toAddWell)
                    {
                        wAddedCount++;
                        _db.Wells.Add(new Well
                        {
                            Id = wItem.Id,
                            WellName = wItem.UniqueName,
                            Latitude = wItem.Latitude,
                            Longitude = wItem.Longitude,
                            CreatedAt = (DateTime)wItem.CreatedAt,
                            UpdatedAt = (DateTime)wItem.UpdatedAt,
                            PlatformId = wItem.PlatformId
                        });
                    }

                    wDeletedCount += toDeleteWell.Count();
                    _db.Wells.RemoveRange(toDeleteWell);
                }
                else
                {
                    pAddedCount++;
                    wAddedCount += pItem.Well.Count();
                    _db.Platforms.Add(new Platform
                    {
                        Id = pItem.Id,
                        PlatformName = pItem.UniqueName,
                        Latitude = pItem.Latitude,
                        Longitude = pItem.Longitude,
                        CreatedAt = (DateTime)pItem.CreatedAt,
                        UpdatedAt = (DateTime)pItem.UpdatedAt,
                        Well = pItem.Well.Select(x => new Well
                        {
                            Id = x.Id,
                            WellName = x.UniqueName,
                            Latitude = x.Latitude,
                            Longitude = x.Longitude,
                            CreatedAt = (DateTime)x.CreatedAt,
                            UpdatedAt = (DateTime)x.UpdatedAt,
                            PlatformId = x.PlatformId
                        }).ToList()
                    });
                }
            }

            var toDeletePlatform = PlatformIQ.AsEnumerable().Where(x => Platforms.All(y => y.Id != x.Id));
            pDeletedCount += toDeletePlatform.Count();
            wDeletedCount += toDeletePlatform.Sum(x => x.Well?.Count() ?? 0);
            _db.Platforms.RemoveRange(toDeletePlatform);

            _db.SaveChanges();

            var result = new SyncResultDTO();
            if (pUpdatedCount > 0) result.UpdatedMessage.Add("Platform Updated : " + pUpdatedCount);
            if (pAddedCount > 0) result.UpdatedMessage.Add("Platform Added : " + pAddedCount);
            if (pDeletedCount > 0) result.UpdatedMessage.Add("Platform Deleted : " + pDeletedCount);
            if (wUpdatedCount > 0) result.UpdatedMessage.Add("Well Updated : " + wUpdatedCount);
            if (wAddedCount > 0) result.UpdatedMessage.Add("Well Added : " + wAddedCount);
            if (wDeletedCount > 0) result.UpdatedMessage.Add("Well Deleted : " + wDeletedCount);

            return result;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
