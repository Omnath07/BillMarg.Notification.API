using BillMarg.Notification.API.Data;
using BillMarg.Notification.API.Interface;
using BillMarg.Notification.API.Model;
using BillMarg.Notification.API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// CONTROLLERS
// =====================================================

builder.Services.AddControllers();


// =====================================================
// SWAGGER
// =====================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// =====================================================
// DATABASE
// =====================================================

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"));
});


// =====================================================
// EMAIL SETTINGS
// Reads EmailSettings from appsettings.json
// =====================================================

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));


// =====================================================
// EMAIL SERVICE
// =====================================================

builder.Services.AddScoped<
    IEmailService,
    EmailService>();


// =====================================================
// DAILY BUSINESS REPORT SERVICE
// =====================================================

builder.Services.AddScoped<
    IDailyBusinessReportService,
    DailyBusinessReportService>();


// =====================================================
// BACKGROUND SERVICE
// Runs daily report automatically
// =====================================================

builder.Services.AddHostedService<
    DailyReportBackgroundService>();

builder.Services.AddScoped<IDailyStockReportService, DailyStockReportService>();

// =====================================================
// BUILD APPLICATION
// =====================================================

var app = builder.Build();


// =====================================================
// DEVELOPMENT ENVIRONMENT
// =====================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BillMarg Notification API v1");
    c.RoutePrefix = "swagger";
});


// =====================================================
// HTTPS
// =====================================================

app.UseHttpsRedirection();


// =====================================================
// AUTHORIZATION
// =====================================================

app.UseAuthorization();


// =====================================================
// CONTROLLERS
// =====================================================

app.MapControllers();


// =====================================================
// RUN
// =====================================================

app.Run();