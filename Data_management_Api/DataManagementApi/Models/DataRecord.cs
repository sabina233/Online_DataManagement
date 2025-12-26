using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataManagementApi.Models
{
    /// <summary>
    /// 数据记录模型 (全字段可空，除了ID)
    /// </summary>
    public class DataRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? Location { get; set; }
        public string? Item { get; set; }

        // 日历年份 (如 2024, 2025)
        public int Year { get; set; }

        // 每月数据
        public float? Jan_ac { get; set; }
        public double? Jan_fc { get; set; }
        public double? Jan_diff { get; set; }

        public double? Feb_ac { get; set; }
        public double? Feb_fc { get; set; }
        public double? Feb_diff { get; set; }

        public double? Mar_ac { get; set; }
        public double? Mar_fc { get; set; }
        public double? Mar_diff { get; set; }

        public double? Apr_ac { get; set; }
        public double? Apr_fc { get; set; }
        public double? Apr_diff { get; set; }

        public double? May_ac { get; set; }
        public double? May_fc { get; set; }
        public double? May_diff { get; set; }

        public double? Jun_ac { get; set; }
        public double? Jun_fc { get; set; }
        public double? Jun_diff { get; set; }

        public double? Jul_ac { get; set; }
        public double? Jul_fc { get; set; }
        public double? Jul_diff { get; set; }

        public double? Aug_ac { get; set; }
        public double? Aug_fc { get; set; }
        public double? Aug_diff { get; set; }

        public double? Sep_ac { get; set; }
        public double? Sep_fc { get; set; }
        public double? Sep_diff { get; set; }

        public double? Oct_ac { get; set; }
        public double? Oct_fc { get; set; }
        public double? Oct_diff { get; set; }

        public double? Nov_ac { get; set; }
        public double? Nov_fc { get; set; }
        public double? Nov_diff { get; set; }

        public double? Dec_ac { get; set; }
        public double? Dec_fc { get; set; }
        public double? Dec_diff { get; set; }

        // 季度数据
        public double? Q1_ac { get; set; }
        public double? Q1_fc { get; set; }
        public double? Q1_diff { get; set; }

        public double? Q2_ac { get; set; }
        public double? Q2_fc { get; set; }
        public double? Q2_diff { get; set; }

        public double? Q3_ac { get; set; }
        public double? Q3_fc { get; set; }
        public double? Q3_diff { get; set; }

        public double? Q4_ac { get; set; }
        public double? Q4_fc { get; set; }
        public double? Q4_diff { get; set; }

        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // 与前端 snake_case 命名规则对齐
        [NotMapped] public string? updated_by { get => UpdatedBy; set => UpdatedBy = value; }
        [NotMapped] public DateTime? updated_at { get => UpdatedAt; set => UpdatedAt = value; }
    }
}
