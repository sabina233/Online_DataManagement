using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataManagementApi.Models
{
    public class KmartDailyRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Location { get; set; } // e.g., China, Vietnam
        public string Category { get; set; } // e.g., RFID, RF
        public string SubCategory { get; set; } // e.g., 44x19MM, 42x18MM
        
        public DateTime Date { get; set; } // Granularity: Daily
        public int Quantity { get; set; } // PCS

        public string? ModifiedBy { get; set; } // The user who last modified this record
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
