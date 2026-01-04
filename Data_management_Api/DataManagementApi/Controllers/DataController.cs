using Microsoft.AspNetCore.Mvc;
using DataManagementApi.Data;
using DataManagementApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;

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
        public async Task<ActionResult> CheckConflict(int year, string month, string item, string location, string brand)
        {
            var query = GetQueryableByBrand(brand);
            if (query == null) return BadRequest("Invalid Brand");

            // Corrected: Cast to BaseRecord to access common properties
            var record = await query.FirstOrDefaultAsync(r => r.Year == year && r.Item == item && r.Location == location);

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
            var prop = obj.GetType().GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return 0;
            var val = prop.GetValue(obj);
            return val == null ? 0 : Convert.ToDouble(val);
        }

        /// <summary>
        /// 获取数据列表
        /// </summary>
        /// <param name="brand">必填：品牌筛选</param>
        /// <returns>数据记录数组</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BaseRecord>>> GetRecords(string? brand = null)
        {
            if (string.IsNullOrEmpty(brand))
            {
                // 如果不传品牌，返回所有数据汇总（Union All）
                // 仅用于大屏汇总，性能消耗较大，建议前端明确传参
                var allRecords = new List<BaseRecord>();
                allRecords.AddRange(await _context.SteriliteRecords.ToListAsync());
                allRecords.AddRange(await _context.NikeRecords.ToListAsync());
                allRecords.AddRange(await _context.TJXRecords.ToListAsync());
                allRecords.AddRange(await _context.LandmarkSplashRecords.ToListAsync());
                allRecords.AddRange(await _context.LandmarkBBSRecords.ToListAsync());
                allRecords.AddRange(await _context.LandmarkMAXRecords.ToListAsync());
                allRecords.AddRange(await _context.NilronRecords.ToListAsync());
                allRecords.AddRange(await _context.WalmartRecords.ToListAsync());
                allRecords.AddRange(await _context.HMRecords.ToListAsync());
                allRecords.AddRange(await _context.TTIRecords.ToListAsync());
                allRecords.AddRange(await _context.TATARecords.ToListAsync());
                allRecords.AddRange(await _context.InditexRecords.ToListAsync());
                allRecords.AddRange(await _context.DCLRecords.ToListAsync());
                allRecords.AddRange(await _context.PadiniRecords.ToListAsync());
                allRecords.AddRange(await _context.KMARTRecords.ToListAsync());
                return allRecords;
            }

            var query = GetQueryableByBrand(brand);
            if (query == null) return BadRequest($"Brand '{brand}' not supported.");

            return await query.ToListAsync();
        }

        /// <summary>
        /// 获取现有品牌列表
        /// </summary>
        [HttpGet("brands")]
        public async Task<ActionResult<IEnumerable<string>>> GetBrands()
        {
            // 固定返回支持的品牌列表，后续可改为查库
            var brands = new List<string>
            {
                "Sterilite", "Nike", "TJX", "Landmark-Splash", "Landmark-BBS", "Landmark-MAX",
                "Nilron", "Walmart", "H&M", "TTI", "TATA", "Inditex", "DCL", "Padini", "KMART"
            };
            return Ok(brands);
        }

        /// <summary>
        /// 保存或更新单条数据记录
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BaseRecord>> SaveRecord([FromBody] Dictionary<string, object> payload)
        {
            // 由于多态反序列化复杂，这里接收 Dictionary 手动处理
            if (!payload.ContainsKey("brand") || payload["brand"] == null)
                return BadRequest("Brand is required.");

            string brand = payload["brand"].ToString()!;
            
            // 提取通用属性
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            
            // 根据 Brand 反序列化为具体类型
            BaseRecord? record = null;
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            switch(brand) {
                case "Sterilite": record = System.Text.Json.JsonSerializer.Deserialize<SteriliteRecord>(json, options); break;
                case "Nike": record = System.Text.Json.JsonSerializer.Deserialize<NikeRecord>(json, options); break;
                case "TJX": record = System.Text.Json.JsonSerializer.Deserialize<TJXRecord>(json, options); break;
                case "Landmark-Splash": record = System.Text.Json.JsonSerializer.Deserialize<LandmarkSplashRecord>(json, options); break;
                case "Landmark-BBS": record = System.Text.Json.JsonSerializer.Deserialize<LandmarkBBSRecord>(json, options); break;
                case "Landmark-MAX": record = System.Text.Json.JsonSerializer.Deserialize<LandmarkMAXRecord>(json, options); break;
                case "Nilron": record = System.Text.Json.JsonSerializer.Deserialize<NilronRecord>(json, options); break;
                case "Walmart": record = System.Text.Json.JsonSerializer.Deserialize<WalmartRecord>(json, options); break;
                case "H&M": record = System.Text.Json.JsonSerializer.Deserialize<HMRecord>(json, options); break;
                case "TTI": record = System.Text.Json.JsonSerializer.Deserialize<TTIRecord>(json, options); break;
                case "TATA": record = System.Text.Json.JsonSerializer.Deserialize<TATARecord>(json, options); break;
                case "Inditex": record = System.Text.Json.JsonSerializer.Deserialize<InditexRecord>(json, options); break;
                case "DCL": record = System.Text.Json.JsonSerializer.Deserialize<DCLRecord>(json, options); break;
                case "Padini": record = System.Text.Json.JsonSerializer.Deserialize<PadiniRecord>(json, options); break;
                case "KMART": record = System.Text.Json.JsonSerializer.Deserialize<KMARTRecord>(json, options); break;
                default: return BadRequest("Unknown brand");
            }

            if (record == null) return BadRequest("Invalid data");

            // 保存逻辑
            if (record.Id > 0)
            {
                var dbSet = GetDbSetObjectByBrand(brand);
                var existing = await dbSet.FindAsync(record.Id);
                if (existing != null)
                {
                    CalculateComputedFields((BaseRecord)record);
                    _context.Entry(existing).CurrentValues.SetValues(record);
                    await _context.SaveChangesAsync();
                    return Ok(existing);
                }
            }
            
            CalculateComputedFields(record);
            await AddRecordToDb(brand, record);
            await _context.SaveChangesAsync();
            return Ok(record);
        }
        
        // 辅助方法：添加记录到对应的 DbSet
        private async Task AddRecordToDb(string brand, BaseRecord record)
        {
             switch (brand)
            {
                case "Sterilite": await _context.SteriliteRecords.AddAsync((SteriliteRecord)record); break;
                case "Nike": await _context.NikeRecords.AddAsync((NikeRecord)record); break;
                case "TJX": await _context.TJXRecords.AddAsync((TJXRecord)record); break;
                case "Landmark-Splash": await _context.LandmarkSplashRecords.AddAsync((LandmarkSplashRecord)record); break;
                case "Landmark-BBS": await _context.LandmarkBBSRecords.AddAsync((LandmarkBBSRecord)record); break;
                case "Landmark-MAX": await _context.LandmarkMAXRecords.AddAsync((LandmarkMAXRecord)record); break;
                case "Nilron": await _context.NilronRecords.AddAsync((NilronRecord)record); break;
                case "Walmart": await _context.WalmartRecords.AddAsync((WalmartRecord)record); break;
                case "H&M": await _context.HMRecords.AddAsync((HMRecord)record); break;
                case "TTI": await _context.TTIRecords.AddAsync((TTIRecord)record); break;
                case "TATA": await _context.TATARecords.AddAsync((TATARecord)record); break;
                case "Inditex": await _context.InditexRecords.AddAsync((InditexRecord)record); break;
                case "DCL": await _context.DCLRecords.AddAsync((DCLRecord)record); break;
                case "Padini": await _context.PadiniRecords.AddAsync((PadiniRecord)record); break;
                case "KMART": await _context.KMARTRecords.AddAsync((KMARTRecord)record); break;
            }
        }

        private dynamic GetDbSetObjectByBrand(string brand)
        {
             switch (brand)
            {
                case "Sterilite": return _context.SteriliteRecords;
                case "Nike": return _context.NikeRecords;
                case "TJX": return _context.TJXRecords;
                case "Landmark-Splash": return _context.LandmarkSplashRecords;
                case "Landmark-BBS": return _context.LandmarkBBSRecords;
                case "Landmark-MAX": return _context.LandmarkMAXRecords;
                case "Nilron": return _context.NilronRecords;
                case "Walmart": return _context.WalmartRecords;
                case "H&M": return _context.HMRecords;
                case "TTI": return _context.TTIRecords;
                case "TATA": return _context.TATARecords;
                case "Inditex": return _context.InditexRecords;
                case "DCL": return _context.DCLRecords;
                case "Padini": return _context.PadiniRecords;
                case "KMART": return _context.KMARTRecords;
                default: throw new Exception("Unknown brand");
            }
        }

        private IQueryable<BaseRecord>? GetQueryableByBrand(string brand)
        {
            switch (brand)
            {
                case "Sterilite": return _context.SteriliteRecords;
                case "Nike": return _context.NikeRecords;
                case "TJX": return _context.TJXRecords;
                case "Landmark-Splash": return _context.LandmarkSplashRecords;
                case "Landmark-BBS": return _context.LandmarkBBSRecords;
                case "Landmark-MAX": return _context.LandmarkMAXRecords;
                case "Nilron": return _context.NilronRecords;
                case "Walmart": return _context.WalmartRecords;
                case "H&M": return _context.HMRecords;
                case "TTI": return _context.TTIRecords;
                case "TATA": return _context.TATARecords;
                case "Inditex": return _context.InditexRecords;
                case "DCL": return _context.DCLRecords;
                case "Padini": return _context.PadiniRecords;
                case "KMART": return _context.KMARTRecords;
                default: return null;
            }
        }

        /// <summary>
        /// 计算汇总字段（季度和差异）
        /// </summary>
        private void CalculateComputedFields(BaseRecord r)
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
