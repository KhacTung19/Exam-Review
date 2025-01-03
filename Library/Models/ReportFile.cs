﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Models
{
    public partial class ReportFile
    {
        public int FileId { get; set; }
        public int ReportId { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public string? FileType { get; set; }
        public long? FileSize { get; set; }
        public DateTime UploadDate { get; set; }

        public virtual Report Report { get; set; } = null!;

    }
}
