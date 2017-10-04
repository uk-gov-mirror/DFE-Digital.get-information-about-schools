﻿using Edubase.Services.Governors.Models;
using Edubase.Web.UI.Models.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using Edubase.Common;
using Edubase.Common.Text;
using Edubase.Web.UI.Areas.Establishments.Models;
using Edubase.Web.UI.Helpers;

namespace Edubase.Web.UI.Areas.Governors.Models
{
    using Services.Governors.DisplayPolicies;
    using Services.Nomenclature;
    using UI.Models;
    using GR = Services.Enums.eLookupGovernorRole;
    using Services.Enums;
    using Edubase.Services.Domain;
    using Edubase.Services.Groups.Models;

    public class GovernorsGridViewModel : Groups.Models.CreateEdit.IGroupPageViewModel, IEstablishmentPageViewModel
    {
        private readonly NomenclatureService _nomenclatureService;

        public List<GovernorGridViewModel> Grids { get; set; } = new List<GovernorGridViewModel>();
        public List<GovernorGridViewModel> HistoricGrids { get; set; } = new List<GovernorGridViewModel>();
        public List<HistoricGovernorViewModel> HistoricGovernors { get; set; } = new List<HistoricGovernorViewModel>();
        public List<LookupItemViewModel> GovernorRoles { get; internal set; }
        public GovernorsDetailsDto DomainModel { get; set; }

        public bool EditMode { get; private set; }

        public string Action { get; set; }

        public string Layout { get; set; }

        public int? Id => EstablishmentUrn ?? GroupUId;

        public int? EstablishmentUrn { get; set; }

        public int? GroupUId { get; set; }

        public string ParentEntityName => GroupUId.HasValue ? "group" : "establishment";

        public string DelegationInformation { get; set; }

        public bool ShowDelegationInformation { get; set; }

        /// <summary>
        /// GID of the governor that's being removed.
        /// </summary>
        public int? RemovalGid { get; set; }

        public bool? GovernorShared { get; set; }

        public DateTimeViewModel RemovalAppointmentEndDate { get; set; } = new DateTimeViewModel();

        public string ListOfEstablishmentsPluralName { get; set; }

        public string GroupName { get; set; }

        public int? GroupTypeId { get; set; }

        public string SelectedTabName { get; set; }

        string IEstablishmentPageViewModel.SelectedTab { get; set; }

        int? IEstablishmentPageViewModel.Urn { get; set; }
        GroupModel IEstablishmentPageViewModel.LegalParentGroup { get; set; }
        string IEstablishmentPageViewModel.LegalParentGroupToken { get; set; }

        string IEstablishmentPageViewModel.Name { get; set; }
        public string TypeName { get; set; }
        public string GroupTypeName { get; set; }

        TabDisplayPolicy IEstablishmentPageViewModel.TabDisplayPolicy { get; set; }

        public eGovernanceMode? GovernanceMode { get; set; }

        public IEnumerable<LookupDto> Nationalities { get; private set; }
        public IEnumerable<LookupDto> AppointingBodies { get; private set; }
        
        public GovernorsGridViewModel(GovernorsDetailsDto dto, bool editMode, int? groupUId, int? establishmentUrn, NomenclatureService nomenclatureService, IEnumerable<LookupDto> nationalities, IEnumerable<LookupDto> appointingBodies)
        {
            _nomenclatureService = nomenclatureService;
            DomainModel = dto;
            EditMode = editMode;
            GroupUId = groupUId;
            Nationalities = nationalities;
            AppointingBodies = appointingBodies;
            EstablishmentUrn = establishmentUrn;
            CreateGrids(dto, dto.CurrentGovernors, false, groupUId, establishmentUrn);
            CreateGrids(dto, dto.HistoricalGovernors, true, groupUId, establishmentUrn);
        }

        public GovernorsGridViewModel()
        {

        }

        private void CreateGrids(GovernorsDetailsDto dto, IEnumerable<GovernorModel> governors, bool isHistoric, int? groupUid, int? establishmentUrn)
        {
            var roles = dto.ApplicableRoles.Where(role => !EnumSets.eSharedGovernorRoles.Contains(role)
                                                          ||
                                                          (RoleEquivalence.GetLocalEquivalentToSharedRole(role) != null
                                                           && !dto.ApplicableRoles.Contains(RoleEquivalence.GetLocalEquivalentToSharedRole(role).Value)));
            foreach (var role in roles)
            {
                var equivalantRoles = RoleEquivalence.GetEquivalentToLocalRole(role).Cast<int>().ToList();
                var pluralise = !EnumSets.eSingularGovernorRoles.Contains(role);


                var grid = new GovernorGridViewModel($"{_nomenclatureService.GetGovernorRoleName(role, eTextCase.SentenceCase, pluralise)}{(isHistoric ? " (in past 12 months)" : string.Empty)}")
                {
                    Tag = isHistoric ? "historic" : "current",
                    Role = role,
                    IsSharedRole = EnumSets.eSharedGovernorRoles.Contains(role),
                    GroupUid = groupUid,
                    EstablishmentUrn = establishmentUrn,
                    IsHistoricRole = isHistoric,
                    RoleName = _nomenclatureService.GetGovernorRoleName(role)
                };

                var displayPolicy = dto.RoleDisplayPolicies.Get(role);
                Guard.IsNotNull(displayPolicy, () => new Exception($"The display policy should not be null for the role '{role}'"));
                bool includeEndDate = ((isHistoric && role == GR.Member || role != GR.Member) && displayPolicy.AppointmentEndDate) || 
                                       (role.OneOfThese(GR.ChiefFinancialOfficer, GR.AccountingOfficer) && isHistoric);

                SetupHeader(role, grid, displayPolicy, includeEndDate);

                var list = governors.Where(x => x.RoleId.HasValue && equivalantRoles.Contains(x.RoleId.Value));
                foreach (var governor in list)
                {
                    var isShared = governor.RoleId.HasValue && EnumSets.SharedGovernorRoles.Contains(governor.RoleId.Value);
                    var establishments = string.Join(", ",
                        governor.Appointments?.Select(a => $"{a.EstablishmentName}, URN: {a.EstablishmentUrn}") ??
                        new string[] { });
                    var appointment = governor.Appointments?.SingleOrDefault(a => a.EstablishmentUrn == EstablishmentUrn);
                    var startDate = (isShared && appointment != null) ? appointment.AppointmentStartDate : governor.AppointmentStartDate;
                    var endDate = (isShared && appointment != null) ? appointment.AppointmentEndDate : governor.AppointmentEndDate;

                    var row = grid.AddRow(governor).AddCell(governor.GetFullName(), displayPolicy.FullName)
                                                    .AddCell(string.IsNullOrWhiteSpace(establishments) ? null : establishments, role.OneOfThese(GR.LocalGovernor, GR.ChairOfLocalGoverningBody))
                                                    .AddCell(governor.Id, displayPolicy.Id)
                                                    .AddCell(AppointingBodies.FirstOrDefault(x => x.Id == governor.AppointingBodyId)?.Name, displayPolicy.AppointingBodyId)
                                                    .AddCell(startDate?.ToString("dd/MM/yyyy"), displayPolicy.AppointmentStartDate)
                                                    .AddCell(endDate?.ToString("dd/MM/yyyy"), includeEndDate)
                                                    .AddCell(governor.PostCode, displayPolicy.PostCode)
                                                    .AddCell(governor.DOB?.ToString("dd/MM/yyyy"), displayPolicy.DOB)
                                                    .AddCell(governor.GetPreviousFullName(), displayPolicy.PreviousFullName)
                                                    .AddCell(governor.EmailAddress, displayPolicy.EmailAddress)
                                                    .AddCell(governor.TelephoneNumber, displayPolicy.TelephoneNumber);
                    if (isHistoric)
                    {
                        var gov = new HistoricGovernorViewModel
                        {
                            AppointingBodyId = governor.AppointingBodyId,
                            AppointingBody = AppointingBodies.FirstOrDefault(x => x.Id == governor.AppointingBodyId)?.Name,
                            AppointmentEndDate = new DateTimeViewModel(governor.AppointmentEndDate),
                            AppointmentStartDate = new DateTimeViewModel(governor.AppointmentStartDate),
                            FullName = governor.GetFullName(),
                            RoleName = _nomenclatureService.GetGovernorRoleName(role)
                        };

                        HistoricGovernors.Add(gov);
                    }
                }

                if (isHistoric)
                {
                    HistoricGrids.Add(grid);
                }
                else
                {
                    Grids.Add(grid);
                }
            }
        }

        private void SetupHeader(GR role, GridViewModel<GovernorModel> grid, GovernorDisplayPolicy displayPolicy, bool includeEndDate)
        {
            grid.AddHeaderCell("Name", displayPolicy.FullName)
                .AddHeaderCell("Shared with", role.OneOfThese(GR.LocalGovernor, GR.ChairOfLocalGoverningBody))
                .AddHeaderCell("GID", displayPolicy.Id)
                .AddHeaderCell("Appointed by", displayPolicy.AppointingBodyId)
                .AddHeaderCell("From", displayPolicy.AppointmentStartDate)
                .AddHeaderCell(role == GR.Member ? "Date stepped down" : "To", includeEndDate)
                .AddHeaderCell("Postcode", displayPolicy.PostCode)
                .AddHeaderCell("Date of birth", displayPolicy.DOB)
                .AddHeaderCell("Previous name", displayPolicy.PreviousFullName)
                .AddHeaderCell("Email address", displayPolicy.EmailAddress)
                .AddHeaderCell("Telephone", displayPolicy.TelephoneNumber);
        }
    }
}