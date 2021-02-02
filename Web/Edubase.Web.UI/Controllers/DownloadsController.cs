using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Edubase.Services.Downloads;
using System.Threading.Tasks;
using System.Web.Http.Results;
using Edubase.Web.UI.Models;
using Newtonsoft.Json;
using Edubase.Services.Domain;
using Edubase.Services.Enums;
using Edubase.Services.Establishments;
using Edubase.Services.Groups.Downloads;
using Edubase.Web.UI.Filters;
using Edubase.Common;
using Edubase.Web.UI.Helpers;
using System.Linq;
using System.Net.Http;
using System.Web.Routing;
using Castle.Components.DictionaryAdapter;
using Edubase.Services;
using Edubase.Services.Downloads.Models;
using Edubase.Web.UI.Models.Search;
using MoreLinq;

namespace Edubase.Web.UI.Controllers
{
    [RoutePrefix("Downloads"), Route("{action=index}")]
    public class DownloadsController : Controller
    {
        private readonly IDownloadsService _downloadsService;
        private readonly IEstablishmentReadService _establishmentReadService;
        private readonly IGroupDownloadService _groupDownloadService;
        private readonly HttpClientWrapper _httpClientHelper;

        public DownloadsController(IDownloadsService downloadsService, IEstablishmentReadService establishmentReadService, IGroupDownloadService groupDownloadService, HttpClientWrapper httpClientHelper)
        {
            _downloadsService = downloadsService;
            _establishmentReadService = establishmentReadService;
            _groupDownloadService = groupDownloadService;
            _httpClientHelper = httpClientHelper;
        }

        public async Task<ActionResult> Index(int? skip, DateTimeViewModel filterDate, eDownloadFilter searchType = eDownloadFilter.Latest)
        {
            var viewModel = await GetDownloads(skip, filterDate, searchType);
            return View(viewModel);
        }

        [HttpGet, Route("results-js")]
        public async Task<PartialViewResult> ResultsPartial(DownloadsViewModel model)
        {
            var viewModel = await GetDownloads(model.Skip, model.FilterDate, model.SearchType);
            return PartialView("Partials/_DownloadsResults", viewModel);
        }

        private async Task<DownloadsViewModel> GetDownloads(int? skip, DateTimeViewModel filterDate, eDownloadFilter searchType = eDownloadFilter.Latest)
        {
            var dateLookup = searchType == eDownloadFilter.Latest ? new DateTimeViewModel(DateTime.Today) :
                filterDate.IsValid() ? filterDate : new DateTimeViewModel(DateTime.Today);

            var viewModel = new DownloadsViewModel
            {
                Downloads = await _downloadsService.GetListAsync(dateLookup.ToDateTime() ?? DateTime.Today, User),
                ScheduledExtracts = await _downloadsService.GetScheduledExtractsAsync(skip.GetValueOrDefault(), 100, User),
                FilterDate = dateLookup,
                SearchType = searchType,
                Skip = skip
            };

            return viewModel;
        }

        [Route("Collate", Name = "CollateDownloads")]
        public async Task<ActionResult> CollateDownloads(DownloadsViewModel model)
        {
            var collection = new FileDownloadRequestArray();
            foreach (var fileDownload in model.Downloads.Where(x => x.Selected))
            {
                collection.Files.Add(new FileDownloadRequest(fileDownload.Tag, fileDownload.FileGeneratedDate));
            }

            if (!collection.Files.Any())
            {
                var routeValuesDictionary = new RouteValueDictionary
                {
                    { nameof(model.Skip), model.Skip },
                    { $"{nameof(model.FilterDate)}.{nameof(model.FilterDate.Day)}", model.FilterDate.Day },
                    { $"{nameof(model.FilterDate)}.{nameof(model.FilterDate.Month)}", model.FilterDate.Month },
                    { $"{nameof(model.FilterDate)}.{nameof(model.FilterDate.Year)}", model.FilterDate.Year },
                    { nameof(model.SearchType), model.SearchType },
                };
                return RedirectToAction(nameof(Index), routeValuesDictionary);
            }

            var response = await _downloadsService.CollateDownloadsAsync(collection, User);
            if (response.Contains("fileLocationUri")) // Hack because the API sometimes returns ApiResultDto and sometimes ProgressDto!
            {
                ViewBag.isDownload = true;
                return View("ReadyToDownload", JsonConvert.DeserializeObject<ProgressDto>(response));
            }
            else
            {
                return RedirectToAction(nameof(DownloadGenerated), new { id = JsonConvert.DeserializeObject<ApiResultDto<Guid>>(response).Value });
            }
        }

        [Route("Generate", Name = "GenerateDownload")]
        public async Task<ActionResult> GenerateDownload(string id)
        {
            var response = await _downloadsService.GenerateExtractAsync(id, User);

            if (response.Contains("fileLocationUri")) // Hack because the API sometimes returns ApiResultDto and sometimes ProgressDto!
            {
                ViewBag.isDownload = true;
                return View("ReadyToDownload", JsonConvert.DeserializeObject<ProgressDto>(response));
            }
            else
            {
                return RedirectToAction(nameof(DownloadGenerated), new { id = JsonConvert.DeserializeObject<ApiResultDto<Guid>>(response).Value });
            }
        }

        [Route("GenerateAjax/{id}", Name = "GenerateAjax")]
        public async Task<ActionResult> GenerateAjax(Guid id)
        {
            var model = await _downloadsService.GetProgressOfGeneratedExtractAsync(id, User);
            return Json(JsonConvert.SerializeObject(new
            {
                status = model.IsComplete, redirect = string.Concat("/Downloads/Generated/", id)
            }), JsonRequestBehavior.AllowGet);

        }

        [Route("Generated/{id}", Name = "DownloadGenerated")]
        public async Task<ActionResult> DownloadGenerated(Guid id)
        {
            var model = await _downloadsService.GetProgressOfGeneratedExtractAsync(id, User);

            if (model.HasErrored)
                throw new Exception($"Download generation failed; Underlying error: '{model.Error}'");

            ViewBag.isDownload = true;
            if (!model.IsComplete)
                return View("PreparingFilePleaseWait", model);

            return View("ReadyToDownload", model);
        }

        [Route("RequestScheduledExtract/{id}", Name = "RequestScheduledExtract")]
        public async Task<ActionResult> RequestScheduledExtract(int id)
        {
            var response = await _downloadsService.GenerateScheduledExtractAsync(id, User);

            if (response.Contains("fileLocationUri")) // Hack because the API sometimes returns ApiResultDto and sometimes ProgressDto!
            {
                return View("ReadyToDownload", JsonConvert.DeserializeObject<ProgressDto>(response));
            }
            else
            {
                return RedirectToAction(nameof(Download), new { id = JsonConvert.DeserializeObject<ApiResultDto<Guid>>(response).Value });
            }
        }

        [Route("Download/{id}", Name = "DownloadScheduledExtract")]
        public async Task<ActionResult> Download(Guid id)
        {
            var model = await _downloadsService.GetProgressOfScheduledExtractGenerationAsync(id, User);

            if (model.HasErrored)
                throw new Exception($"Download generation failed; Underlying error: '{model.Error}'");

            if (!model.IsComplete)
                return View("PreparingFilePleaseWait", model);

            return View("ReadyToDownload", model);
        }

        [HttpGet, Route("Download/Establishment/{urn}", Name = "EstabDataDownload")]
        public async Task<ActionResult> DownloadEstablishmentData(int urn, string state, DownloadType? downloadType = null, bool start = false)
        {
            state.AssertIsNotEmpty(nameof(state));
            ViewBag.RouteName = "EstabDataDownload";
            ViewBag.BreadcrumbRoutes = UriHelper.DeserializeUrlToken<RouteDto[]>(state);
            if (downloadType.HasValue && !start) return View("Download");
            else if (downloadType.HasValue && start) return Redirect((await _establishmentReadService.GetDownloadAsync(urn, downloadType.Value, User)).Url);
            else return View("SelectFormat");
        }

        [HttpGet, Route("Download/Group/{uid}", Name = "GroupDataDownload")]
        public async Task<ActionResult> DownloadGroupData(int uid, string state, DownloadType? downloadType = null, bool start = false)
        {
            state.AssertIsNotEmpty(nameof(state));
            ViewBag.RouteName = "GroupDataDownload";
            ViewBag.BreadcrumbRoutes = UriHelper.DeserializeUrlToken<RouteDto[]>(state);
            if (downloadType.HasValue && !start) return View("Download");
            else if (downloadType.HasValue && start) return Redirect((await _groupDownloadService.DownloadGroupData(uid, downloadType.Value, User)).Url);
            else return View("SelectFormat");
        }

        [HttpGet, EdubaseAuthorize]
        [Route("Download/ChangeHistory/{downloadType}")]
        public async Task<ActionResult> DownloadChangeHistory(int? groupId, int? establishmentUrn, DownloadType downloadType)
        {
            if (groupId != null)
            {
                return Redirect((await _groupDownloadService.DownloadGroupHistory(groupId.Value, downloadType, null, null, null, User)).Url);
            }

            if (establishmentUrn != null)
            {
                return Redirect((await _establishmentReadService.GetChangeHistoryDownloadAsync(establishmentUrn.Value, downloadType, User)).Url);
            }

            return null;
        }

        [HttpGet, EdubaseAuthorize]
        [Route("Download/Establishment/{id}/{downloadType}", Name = "DownloadEstablishmentGovernanceChangeHistory")]
        public async Task<ActionResult> DownloadGovernanceChangeHistoryAsync(int id, DownloadType downloadType)
            => Redirect((await _establishmentReadService.GetGovernanceChangeHistoryDownloadAsync(id, downloadType, User)).Url);

        [HttpGet, EdubaseAuthorize]
        [Route("Download/Group/{id}/Governance/{downloadType}", Name = "DownloadGroupGovernanceChangeHistory")]
        public async Task<ActionResult> DownloadGroupGovernanceChangeHistoryAsync(int id, DownloadType downloadType)
            => Redirect((await _groupDownloadService.GetGovernanceChangeHistoryDownloadAsync(id, downloadType, User)).Url);

        [HttpGet]
        [Route("Download/File", Name = "DownloadFile")]
        public async Task<ActionResult> DownloadFileAsync(string id, DateTime? filterDate)
        {
            var file = (await _downloadsService.GetListAsync(filterDate ?? DateTime.Today, User)).FirstOrDefault(x => x.Tag == id);
            if (file != null)
            {
                using (var c = IocConfig.CreateHttpClient())
                {
                    var requestMessage = await _httpClientHelper.CreateHttpRequestMessageAsync(HttpMethod.Get, file.Url, User);
                    var response = (await c.SendAsync(requestMessage)).EnsureSuccessStatusCode();
                    return new FileStreamResult(await response.Content.ReadAsStreamAsync(), response.Content.Headers.ContentType.MediaType)
                    {
                        FileDownloadName = response.Content.Headers.ContentDisposition.FileName.RemoveSubstring("\"")
                    };
                }
            }
            else
            {
                return HttpNotFound();
            }
        }

        [HttpPost, Route("Download/Extract", Name="DownloadExtract")]
        public async Task<ActionResult> DownloadExtractAsync(string path, string id, string searchQueryString = null, eLookupSearchSource? searchSource = null)
        {
            var uri = new Uri(path);
            var downloadAvailable = await _downloadsService.IsDownloadAvailable($"/{uri.Segments.Last()}", id, User);

            if (downloadAvailable)
            {
                return Redirect($"{path}?id={id}");
            }
            else
            {
                var view = new DownloadErrorViewModel
                {
                    SearchQueryString = searchQueryString,
                    SearchSource = searchSource
                };
                return View("Downloads/DownloadError", view);
            }
        }

        [HttpGet, EdubaseAuthorize, MvcAuthorizeRoles(AuthorizedRoles.CanManageAcademyTrusts)]
        [Route("Download/MATClosureReport", Name = "DownloadMATClosureReport")]
        public async Task<ActionResult> DownloadMATClosureReportAsync()
        {
            using (var c = IocConfig.CreateHttpClient())
            {
                var requestMessage = await _httpClientHelper.CreateHttpRequestMessageAsync(HttpMethod.Get, "downloads/matclosurereport.csv", User);
                var response = (await c.SendAsync(requestMessage));

                if(response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return HttpNotFound();
                }
                else
                {
                    response.EnsureSuccessStatusCode();
                    return new FileStreamResult(await response.Content.ReadAsStreamAsync(), response.Content.Headers.ContentType.MediaType) { FileDownloadName = $"SatAndMatClosureReport_{DateTime.Now.Date.ToString("dd-MM-yyyy")}.csv" };
                }
            }
        }

    }
}
