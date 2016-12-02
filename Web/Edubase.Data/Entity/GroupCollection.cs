﻿using Edubase.Data.Entity.ComplexTypes;
using Edubase.Data.Entity.Lookups;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Edubase.Data.Entity
{
    public class GroupCollection : EdubaseEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GroupUID { get; set; }
        public string Name { get; set; }
        public string CompaniesHouseNumber { get; set; }
        public LookupGroupType GroupType { get; set; }
        public int? GroupTypeId { get; set; }
        public DateTime? ClosedDate { get; set; }
        public LookupGroupStatus Status { get; set; }
        public int? StatusId { get; set; }
        public DateTime? OpenDate { get; set; }
        public Person Head { get; set; } = new Person();
        public string Address { get; set; }
        public string ManagerEmailAddress { get; set; }
        
        [Index(IsUnique=false), StringLength(400)]
        public string GroupId { get; set; }

        /// <summary>
        /// Temporary!
        /// </summary>
        [Obsolete, NotMapped]
        public int EstablishmentCount { get; set; }
    }
}