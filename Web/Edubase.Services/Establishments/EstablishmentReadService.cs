﻿using AutoMapper;
using Edubase.Common;
using Edubase.Data;
using Edubase.Data.Entity;
using Edubase.Data.Repositories.Establishments;
using Edubase.Data.Repositories.LocalAuthorities;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Edubase.Services.Establishments
{
    using Domain;
    using Services.Enums;
    using DisplayPolicies;
    using Models;
    using Search;
    using Exceptions;
    using Groups.Models;
    using IntegrationEndPoints.AzureSearch;
    using IntegrationEndPoints.AzureSearch.Models;
    using Security;
    using Data.DbContext;
    using Common.Cache;
    using System.IO;
    using Common.IO;
    using Ionic.Zip;
    using Lookup;
    using Doc = Search.SearchEstablishmentDocument;
    using static Search.EstablishmentSearchPayload;
    using Common.Spatial;

    public class EstablishmentReadService : IEstablishmentReadService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ICachedLookupService _cachedLookupService;
        private readonly IAzureSearchEndPoint _azureSearchService;
        private readonly IEstablishmentReadRepository _establishmentRepository;
        private readonly ILAReadRepository _laRepository;
        private ICacheAccessor _cacheAccessor;
        private IBlobService _blobService;

        /// <summary>
        /// Allow these roles to see establishments of all statuses
        /// </summary>
        private readonly string[] _nonStatusRestrictiveRoles = new[] { EdubaseRoles.EFA, EdubaseRoles.AOS, EdubaseRoles.FSG,
            EdubaseRoles.IEBT, EdubaseRoles.School, EdubaseRoles.PRU, EdubaseRoles.Admin };


        private readonly int[] _restrictedStatuses = new[]
        {
            eLookupEstablishmentStatus.Closed,
            eLookupEstablishmentStatus.Open,
            eLookupEstablishmentStatus.OpenButProposedToClose,
            eLookupEstablishmentStatus.ProposedToOpen
        }.Select(x => (int)x).ToArray();
        

        public EstablishmentReadService(
            IApplicationDbContext dbContext, 
            IMapper mapper, 
            ICachedLookupService cachedLookupService, 
            IAzureSearchEndPoint azureSearchService,
            ICachedEstablishmentReadRepository establishmentRepository,
            ICachedLAReadRepository laRepository,
            ICacheAccessor cacheAccessor,
            IBlobService blobService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _cachedLookupService = cachedLookupService;
            _establishmentRepository = establishmentRepository;
            _azureSearchService = azureSearchService;
            _laRepository = laRepository;
            _cacheAccessor = cacheAccessor;
            _blobService = blobService;
        }
        
        public async Task<ServiceResultDto<EstablishmentModel>> GetAsync(int urn, IPrincipal principal)
        {
            var dataModel = await _establishmentRepository.GetAsync(urn);

            if (dataModel != null)
            {
                if (HasAccess(principal, dataModel.StatusId))
                {
                    var domainModel = _mapper.Map<Establishment, EstablishmentModel>(dataModel);
                    domainModel.AdditionalAddressesCount = domainModel.AdditionalAddresses.Count;

                    if (!principal.InRole(EdubaseRoles.Admin, EdubaseRoles.IEBT))
                    {
                        var toRemove = domainModel.AdditionalAddresses.Where(x => x.IsRestricted == true).ToArray();
                        for (int i = 0; i < toRemove.Length; i++)
                        {
                            domainModel.AdditionalAddresses.Remove(toRemove[i]);
                        }
                    }

                    if (domainModel.TypeId == (int)eLookupEstablishmentType.ChildrensCentre
                        && domainModel.LocalAuthorityId.HasValue) // supply LA contact details
                    {
                        var la = await _laRepository.GetAsync(domainModel.LocalAuthorityId.Value);
                        domainModel.CCLAContactDetail = new ChildrensCentreLocalAuthorityDto(la);
                    }
                    return new ServiceResultDto<EstablishmentModel>(domainModel);
                }
                else return new ServiceResultDto<EstablishmentModel>(eServiceResultStatus.PermissionDenied);
            }
            else return new ServiceResultDto<EstablishmentModel>(eServiceResultStatus.NotFound);
        }

        private bool HasAccess(IPrincipal principal, int? statusId)
        {
            var isRestricted = IsRoleRestrictedOnStatus(principal);
            if (isRestricted && !statusId.HasValue) throw new Exception("StatusId is null but the principal has restricted access; impossible to acertain permissions");
            return !IsRoleRestrictedOnStatus(principal)
              || GetPermittedStatusIds(principal).Any(x => x == statusId.Value);
        }

        public EstablishmentDisplayPolicy GetDisplayPolicy(IPrincipal user, EstablishmentModelBase establishment) 
            => new DisplayPolicyFactory().Create(user, establishment);


        public async Task<IEnumerable<LinkedEstablishmentModel>> GetLinkedEstablishments(int urn)
        {
            return (await _dbContext.EstablishmentLinks
                    .Include(x => x.LinkedEstablishment)
                    .Include(x => x.LinkType)
                    .Where(x => x.EstablishmentUrn == urn && x.IsDeleted == false).ToArrayAsync())
                    .Select(x => new LinkedEstablishmentModel(x)).ToArray();
        }

        public async Task<IEnumerable<EstablishmentChangeDto>> GetChangeHistoryAsync(int urn, int take, IPrincipal user)
        {
            return Enumerable.Empty<EstablishmentChangeDto>();
        }
        
        public async Task<IEnumerable<EstablishmentSuggestionItem>> SuggestAsync(string text, IPrincipal principal, int take = 10)
        {
            var oDataFilters = new ODataFilterList(ODataFilterList.AND, AzureSearchEndPoint.ODATA_FILTER_DELETED);
            if (IsRoleRestrictedOnStatus(principal))
            {
                oDataFilters.Add(ODataUtil.Or(nameof(Doc.StatusId), _restrictedStatuses));
            }
            return await _azureSearchService.SuggestAsync<EstablishmentSuggestionItem>(EstablishmentsSearchIndex.INDEX_NAME, EstablishmentsSearchIndex.SUGGESTER_NAME, text, oDataFilters.ToString(), take);
        }

        public int[] GetPermittedStatusIds(IPrincipal principal)
        {
            if (IsRoleRestrictedOnStatus(principal)) return _restrictedStatuses;
            else return null;
        }


        public async Task<AzureSearchResult<Doc>> SearchAsync(EstablishmentSearchPayload payload, IPrincipal principal)
        {
            if (IsRoleRestrictedOnStatus(principal))
            {
                if (payload.Filters.StatusIds.Any())
                {
                    if (!payload.Filters.StatusIds.All(x => _restrictedStatuses.Any(s => s == x)))
                        throw new EdubaseException("One or more of the status ids requested are outside the permissions of the current principal");
                }
                else payload.Filters.StatusIds = _restrictedStatuses.ToArray();
            }

            var predicates = payload.Filters.ToODataPredicateList(AzureSearchEndPoint.ODATA_FILTER_DELETED);

            string orderByODataExpression = null;
            if (payload.GeoSearchLocation != null)
            {
                var distance = new Distance(payload.RadiusInMiles ?? 3);
                var geoPredicate = new ODataGeographyExpression(payload.GeoSearchLocation, nameof(Doc.Location));
                predicates.Add(geoPredicate.ToFilterODataExpression(distance.Kilometres));
                if(payload.SortBy == eSortBy.Distance) orderByODataExpression = geoPredicate.ToODataExpression();
            }

            if (payload.SortBy.OneOfThese(eSortBy.NameAlphabeticalAZ, eSortBy.NameAlphabeticalZA))
                orderByODataExpression = string.Concat(nameof(Doc.NameDistilled), " ", (payload.SortBy == eSortBy.NameAlphabeticalAZ ? "asc" : "desc"));

            if (payload.SENIds.Any())
            {
                var senPredicates = new[] { nameof(Doc.SEN1Id), nameof(Doc.SEN2Id), nameof(Doc.SEN3Id), nameof(Doc.SEN4Id) }
                    .SelectMany(x => payload.SENIds.Select(s => new { Name = x, Value = s }));
                var senODataFilter = new ODataFilterList(ODataFilterList.OR);
                senPredicates.ForEach(x => senODataFilter.Add(x.Name, x.Value));
                predicates.Add($"({senODataFilter})");
            }

            var oDataFilterExpression = string.Join(" and ", predicates);

            return await _azureSearchService.SearchAsync<Doc>(EstablishmentsSearchIndex.INDEX_NAME,
                payload.Text,
                oDataFilterExpression,
                payload.Skip,
                payload.Take,
                new List<string> { nameof(Doc.NameDistilled) },
                new List<string> { orderByODataExpression });
        }

        private bool IsRoleRestrictedOnStatus(IPrincipal principal)
            => !_nonStatusRestrictiveRoles.Any(x => principal.IsInRole(x));
        
        public async Task<ServiceResultDto<bool>> CanAccess(int urn, IPrincipal principal)
        {
            var statusId = await _establishmentRepository.GetStatusAsync(urn);
            if (statusId.HasValue)
            {
                if (HasAccess(principal, statusId)) return new ServiceResultDto<bool>(true);
                else return new ServiceResultDto<bool>(eServiceResultStatus.PermissionDenied);
            }
            else return new ServiceResultDto<bool>(eServiceResultStatus.NotFound);
        }


        


    }
}
