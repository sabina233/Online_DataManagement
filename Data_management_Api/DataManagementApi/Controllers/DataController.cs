using Microsoft.AspNetCore.Mvc;
using DataManagementApi.Data;
using DataManagementApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace DataManagementApi.Controllers
{
    /// <summary>
    /// 数据管理控制器 - 处理核心业务数据
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 启用 JWT 认证
    public class DataController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DataController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 检查数据冲突（防止重复录入）
        /// </summary>
        [HttpGet("check-conflict")]
        public async Task<ActionResult> CheckConflict(int year, string month, string item, string location)
        {
            var record = await _context.DataRecords
                                       .FirstOrDefaultAsync(r => r.Year == year && r.Item == item && r.Location == location);

            if (record == null)
            {
                return Ok(new { exists = false });
            }

            // 检查指定月份是否有数据
            var acValue = GetPropValue(record, $"{month.ToLower()}_ac");
            var fcValue = GetPropValue(record, $"{month.ToLower()}_fc");

            if (acValue > 0 || fcValue > 0)
            {
                return Ok(new { 
                    exists = true, 
                    record = new { 
                        ac = acValue, 
                        fc = fcValue 
                    } 
                });
            }

            return Ok(new { exists = false });
        }

        /// <summary>
        /// 利用反射获取属性值的辅助方法
        /// </summary>
        private double GetPropValue(object obj, string name)
        {
            var prop = obj.GetType().GetProperty(name, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (prop == null) return 0;
            return Convert.ToDouble(prop.GetValue(obj));
        }

        /// <summary>
        /// 获取数据列表
        /// </summary>
        /// <param name="brand">可选品牌筛选</param>
        /// <returns>数据记录数组</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DataRecord>>> GetRecords(string? brand = null)
        {
            // 如果不传品牌，返回所有数据，供大屏汇总使用
            if (string.IsNullOrEmpty(brand))
            {
                return await _context.DataRecords.ToListAsync();
            }
            return await _context.DataRecords
                                 .Where(r => r.Item == brand)
                                 .ToListAsync();
        }

        /// <summary>
        /// 获取现有品牌列表
        /// </summary>
        [HttpGet("brands")]
        public async Task<ActionResult<IEnumerable<string>>> GetBrands()
        {
            var items = await _context.DataRecords
                                 .Select(r => r.Item)
                                 .Distinct()
                                 .Where(i => i != null)
                                 .ToListAsync();
            return Ok(items.Select(i => i!));
        }

        /// <summary>
        /// 保存或更新单条数据记录
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<DataRecord>> SaveRecord(DataRecord record)
        {
            if (record.Id > 0)
            {
                var existing = await _context.DataRecords.FindAsync(record.Id);
                if (existing != null)
                {
                    // 更新并重算计算字段
                    CalculateComputedFields(record);
                    _context.Entry(existing).CurrentValues.SetValues(record);
                    await _context.SaveChangesAsync();
                    return Ok(existing);
                }
            }
            
            // 执行计算
            CalculateComputedFields(record);

            // 新增记录
            _context.DataRecords.Add(record);
            await _context.SaveChangesAsync();
            return Ok(record);
        }
        
        /// <summary>
        /// 批量保存数据记录
        /// </summary>
        [HttpPost("batch")]
        public async Task<ActionResult> BatchSave(IEnumerable<DataRecord> records)
        {
            foreach (var record in records)
            {
                if (record.Id > 0)
                {
                    var existing = await _context.DataRecords.FindAsync(record.Id);
                    if (existing != null)
                    {
                        CalculateComputedFields(record);
                        _context.Entry(existing).CurrentValues.SetValues(record);
                        continue;
                    }
                }
                
                // 执行计算
                CalculateComputedFields(record);
                
                _context.DataRecords.Add(record);
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// 计算汇总字段（季度和差异）
        /// </summary>
        private void CalculateComputedFields(DataRecord r)
        {
            // 每月差异计算
            r.Jan_diff = CalculateDiff(r.Jan_ac, r.Jan_fc);
            r.Feb_diff = CalculateDiff(r.Feb_ac, r.Feb_fc);
            r.Mar_diff = CalculateDiff(r.Mar_ac, r.Mar_fc);
            r.Apr_diff = CalculateDiff(r.Apr_ac, r.Apr_fc);
            r.May_diff = CalculateDiff(r.May_ac, r.May_fc);
            r.Jun_diff = CalculateDiff(r.Jun_ac, r.Jun_fc);
            r.Jul_diff = CalculateDiff(r.Jul_ac, r.Jul_fc);
            r.Aug_diff = CalculateDiff(r.Aug_ac, r.Aug_fc);
            r.Sep_diff = CalculateDiff(r.Sep_ac, r.Sep_fc);
            r.Oct_diff = CalculateDiff(r.Oct_ac, r.Oct_fc);
            r.Nov_diff = CalculateDiff(r.Nov_ac, r.Nov_fc);
            r.Dec_diff = CalculateDiff(r.Dec_ac, r.Dec_fc);

            // 季度累计录入
            r.Q1_ac = (r.Jan_ac ?? 0) + (r.Feb_ac ?? 0) + (r.Mar_ac ?? 0);
            r.Q1_fc = (r.Jan_fc ?? 0) + (r.Feb_fc ?? 0) + (r.Mar_fc ?? 0);
            r.Q1_diff = CalculateDiff(r.Q1_ac, r.Q1_fc);

            r.Q2_ac = (r.Apr_ac ?? 0) + (r.May_ac ?? 0) + (r.Jun_ac ?? 0);
            r.Q2_fc = (r.Apr_fc ?? 0) + (r.May_fc ?? 0) + (r.Jun_fc ?? 0);
            r.Q2_diff = CalculateDiff(r.Q2_ac, r.Q2_fc);

            r.Q3_ac = (r.Jul_ac ?? 0) + (r.Aug_ac ?? 0) + (r.Sep_ac ?? 0);
            r.Q3_fc = (r.Jul_fc ?? 0) + (r.Aug_fc ?? 0) + (r.Sep_fc ?? 0);
            r.Q3_diff = CalculateDiff(r.Q3_ac, r.Q3_fc);

            r.Q4_ac = (r.Oct_ac ?? 0) + (r.Nov_ac ?? 0) + (r.Dec_ac ?? 0);
            r.Q4_fc = (r.Oct_fc ?? 0) + (r.Nov_fc ?? 0) + (r.Dec_fc ?? 0);
            r.Q4_diff = CalculateDiff(r.Q4_ac, r.Q4_fc);
        }

        /// <summary>
        /// 计算达成率（AC/FC * 100）
        /// </summary>
        private double? CalculateDiff(double? ac, double? fc)
        {
            if (fc == null || fc == 0) return 0;
            // 按需求计算 AC / FC 的百分比
            return (ac ?? 0) / fc.Value * 100;
        }
    }
}
