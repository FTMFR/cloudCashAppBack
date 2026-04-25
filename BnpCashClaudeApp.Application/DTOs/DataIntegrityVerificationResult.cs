using System;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Application.DTOs
{
    /// <summary>
    /// نتیجه بررسی صحت داده‌های حساس
    /// </summary>
    public class DataIntegrityVerificationResult
    {
        /// <summary>
        /// تعداد کل نقض‌های شناسایی شده
        /// </summary>
        public int TotalViolations { get; set; }

        /// <summary>
        /// تعداد نقض‌ها تفکیک شده بر اساس نوع Entity
        /// </summary>
        public Dictionary<string, int> ViolationsByEntityType { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// زمان بررسی
        /// </summary>
        public DateTime VerificationTime { get; set; } = DateTime.UtcNow;
    }
}

