﻿using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Edubase.Services.Security.ClaimsIdentityConverters;
using Edubase.Services.Domain;

namespace Edubase.Services.Security
{
    public interface ISecurityService
    {
        /// <summary>
        /// TODO TEXCHANGE: DELETE THIS!
        /// </summary>
        /// <returns></returns>
        IPrincipal CreateBackOfficePrincipal();

        IPrincipal CreateAnonymousPrincipal();

        Task<CreateGroupPermissionDto> GetCreateGroupPermissionAsync(IPrincipal principal);
        Task<CreateEstablishmentPermissionDto> GetCreateEstablishmentPermissionAsync(IPrincipal principal);
    }
}