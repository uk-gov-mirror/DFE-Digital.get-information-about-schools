﻿using Edubase.Data.Identity;
using Edubase.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace Edubase.Web.UI.Controllers
{
    public class ApprovalController : Controller
    {
        // GET: Approval
        public ActionResult Index(int? urn = null, int skip = 0, int take = 10, string updatedName = null, string a = null)
        {
            ViewBag.UpdatedName = updatedName;
            ViewBag.ActionName = a;
            var dto = new ApprovalService().GetAll(User as ClaimsPrincipal, skip, take, urn);

            if(dto.Items.Count == 0 && dto.Count > 0 && skip > 0)
            {
                return RedirectToAction("Index", new { skip = Math.Max(skip - take, 0), take, urn, updatedName, a });
            }

            return View(dto);
        }

        [HttpPost]
        public ActionResult Accept(int id, int? urn, int skip, int take)
        {
            var fieldname = new ApprovalService().Accept(User as ClaimsPrincipal, id);
            return RedirectToAction("Index", new { skip, take, urn, updatedName = fieldname, a = "a" });
        }

        [HttpPost]
        public ActionResult Reject(int id, int? urn, int skip, int take, string reason)
        {
            var fieldname = new ApprovalService().Reject(User as ClaimsPrincipal, id, reason);
            return RedirectToAction("Index", new { skip, take, urn, updatedName = fieldname, a = "r" });
        }


    }
}