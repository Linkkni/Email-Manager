using EmailWebApi.Api.Data;
using EmailWebApi.Api.Services;
using EmailWebApi.Api.Data;
using EmailWebApi.Api.Services; // Đảm bảo using đúng namespace của service
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.

// Cấu hình DbContext cho email cục bộ (nếu vẫn dùng)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<EmailDBContact>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)))
);

// Đăng ký EmailService (cho email cục bộ)
builder.Services.AddScoped<IEmailService, EmailService>();

// Đăng ký GmailApiService cho Dependency Injection
builder.Services.AddScoped<EmailWebApi.Api.Services.IGmailApiService, GmailApiService>(); // THÊM DÒNG NÀY

builder.Services.AddControllers();

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policy =>
        {
            // THAY THẾ "http://localhost:3001" BẰNG PORT FRONTEND REACT CỦA BẠN
            policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Email Web API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Email Web API v1"));

    // Tự động migrate database cục bộ (nếu dùng)
    using (var scope = app.Services.CreateScope())
    {
        var localDbContext = scope.ServiceProvider.GetRequiredService<EmailDBContact>();
        localDbContext.Database.Migrate();
    }
}

// app.UseHttpsRedirection(); // Tạm thời có thể bỏ comment nếu đang gặp vấn đề với HTTPS local

app.UseCors("AllowSpecificOrigin");

app.UseRouting(); // Đảm bảo UseRouting() được gọi trước UseAuthorization() và UseEndpoints()

app.UseAuthorization(); // Nếu bạn có xác thực/ủy quyền cho API của mình

app.MapControllers();

app.Run();
