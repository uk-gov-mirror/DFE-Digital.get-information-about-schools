﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edubase.Services.Domain
{
    public class SearchDownloadGenerationProgressDto
    {
        public Guid Id { get; set; }
        public int PercentageComplete => (int) Math.Ceiling((ProcessedCount > 0 && TotalRecordsCount > 0 ? (double) ProcessedCount / TotalRecordsCount : 0) * 100);
        public string ExceptionMessageId { get; set; }
        public long TotalRecordsCount { get; set; }
        public int ProcessedCount { get; set; }
        public string Status { get; set; } = "Initialising...";
        public bool IsComplete { get; set; }
        public string FileLocation { get; set; }
        public bool HasErrored => ExceptionMessageId != null;
        public string FileExtension { get; set; }

        public SearchDownloadGenerationProgressDto()
        {

        }

        public SearchDownloadGenerationProgressDto(Guid id)
        {
            Id = id;
        }
    }
}