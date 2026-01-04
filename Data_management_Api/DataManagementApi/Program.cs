using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using DataManagementApi.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 添加控制器服务
builder.Services.AddControllers();

// 注册 DbContext (使用 SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 配置 JWT 认证
var jwtKey = builder.Configuration["Jwt:Key"] ?? "DefaultSecretKeyForDevOnly1234567890!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// 注册 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // 前端地址
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Swagger 配置
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 自动执行 EF Core 迁移
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
            var context = services.GetRequiredService<AppDbContext>();
            context.Database.EnsureCreated(); 

            // [MANUAL MIGRATION] Create Brands Table
            context.Database.ExecuteSqlRaw(@"
                IF OBJECT_ID(N'[Brands]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Brands] (
                        [Id] int NOT NULL IDENTITY,
                        [Name] nvarchar(max) NOT NULL,
                        CONSTRAINT [PK_Brands] PRIMARY KEY ([Id])
                    );
                END
            ");

            // [MANUAL MIGRATION] Seed Brands
            if (!context.Brands.Any())
            {
                var defaultBrands = new[] { "Sterilite", "Nike", "TJX", "Landmark-Splash", "Landmark-BBS", "Landmark-MAX", "Nilron", "Walmart", "H&M", "TTI", "TATA", "Inditex", "DCL", "Padini", "KMART" };
                foreach (var b in defaultBrands) context.Brands.Add(new DataManagementApi.Models.Brand { Name = b });
                context.SaveChanges();
            }

            // [MANUAL MIGRATION] Create Brand Tables
            var recordTables = new[] { 
                "SteriliteRecords", "NikeRecords", "TJXRecords", "LandmarkSplashRecords", "LandmarkBBSRecords", 
                "LandmarkMAXRecords", "NilronRecords", "WalmartRecords", "HMRecords", "TTIRecords", 
                "TATARecords", "InditexRecords", "DCLRecords", "PadiniRecords", "KMARTRecords" 
            };

            foreach (var table in recordTables)
            {
                string createTableSql = $@"
                IF OBJECT_ID(N'[{table}]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [{table}] (
                        [Id] int NOT NULL IDENTITY,
                        [Location] nvarchar(max) NULL,
                        [Item] nvarchar(max) NULL,
                        [Year] int NOT NULL,
                        [Jan_ac] float NULL, [Jan_fc] float NULL, [Jan_diff] float NULL,
                        [Feb_ac] float NULL, [Feb_fc] float NULL, [Feb_diff] float NULL,
                        [Mar_ac] float NULL, [Mar_fc] float NULL, [Mar_diff] float NULL,
                        [Apr_ac] float NULL, [Apr_fc] float NULL, [Apr_diff] float NULL,
                        [May_ac] float NULL, [May_fc] float NULL, [May_diff] float NULL,
                        [Jun_ac] float NULL, [Jun_fc] float NULL, [Jun_diff] float NULL,
                        [Jul_ac] float NULL, [Jul_fc] float NULL, [Jul_diff] float NULL,
                        [Aug_ac] float NULL, [Aug_fc] float NULL, [Aug_diff] float NULL,
                        [Sep_ac] float NULL, [Sep_fc] float NULL, [Sep_diff] float NULL,
                        [Oct_ac] float NULL, [Oct_fc] float NULL, [Oct_diff] float NULL,
                        [Nov_ac] float NULL, [Nov_fc] float NULL, [Nov_diff] float NULL,
                        [Dec_ac] float NULL, [Dec_fc] float NULL, [Dec_diff] float NULL,
                        [Q1_ac] float NULL, [Q1_fc] float NULL, [Q1_diff] float NULL,
                        [Q2_ac] float NULL, [Q2_fc] float NULL, [Q2_diff] float NULL,
                        [Q3_ac] float NULL, [Q3_fc] float NULL, [Q3_diff] float NULL,
                        [Q4_ac] float NULL, [Q4_fc] float NULL, [Q4_diff] float NULL,
                        [UpdatedBy] nvarchar(max) NULL,
                        [UpdatedAt] datetime2 NULL,
                        CONSTRAINT [PK_{table}] PRIMARY KEY ([Id])
                    );
                END";
                context.Database.ExecuteSqlRaw(createTableSql);
            }

            // 2. Kmart Daily Records Table
             var createKmartTableSql = @"
                IF OBJECT_ID(N'[KmartDailyRecords]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [KmartDailyRecords] (
                        [Id] int NOT NULL IDENTITY,
                        [Location] nvarchar(max) NOT NULL,
                        [Category] nvarchar(max) NOT NULL,
                        [SubCategory] nvarchar(max) NOT NULL,
                        [Date] datetime2 NOT NULL,
                        [Quantity] int NOT NULL,
                        [ModifiedBy] nvarchar(max) NULL,
                        [UpdatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_KmartDailyRecords] PRIMARY KEY ([Id])
                    );
                END
                ELSE
                BEGIN
                    IF COL_LENGTH('[KmartDailyRecords]', 'ModifiedBy') IS NULL
                    BEGIN
                        ALTER TABLE [KmartDailyRecords] ADD [ModifiedBy] nvarchar(max) NULL;
                    END
                END
            ";
            context.Database.ExecuteSqlRaw(createKmartTableSql);

            // Seed Default Admin if No Users
            if (!context.Users.Any())
            {
                context.Users.Add(new DataManagementApi.Models.User
                {
                    Username = "admin",
                    Password = "123", // Default Password
                    Role = "admin",
                    Department = "IT",
                    CreatedAt = DateTime.Now,
                    Avatar = "https://ui-avatars.com/api/?name=Admin&background=0D8ABC&color=fff"
                });
                context.SaveChanges();
            }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseAuthentication(); // 启用身份认证
app.UseAuthorization();  // 启用权限授权

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
